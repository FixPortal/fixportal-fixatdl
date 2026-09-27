#!/usr/bin/env bash
# Exercises assert-release-eligibility.sh against a real, disposable git history for
# every eligibility outcome. The fake GitHub CLI serves canned paged responses and the
# gate applies its own jq filter to them, so weakening the merged-PR condition is caught
# here.
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
gate="$script_dir/../assert-release-eligibility.sh"

root="$(mktemp -d)"
trap 'rm -rf "$root"' EXIT

repo="$root/repo"
git init --quiet "$repo"
git -C "$repo" config user.email "test@example.com"
git -C "$repo" config user.name "test"

git -C "$repo" commit --quiet --allow-empty -m "main commit 1"
main_eligible_commit="$(git -C "$repo" rev-parse HEAD)"
git -C "$repo" branch main_ref "$main_eligible_commit"

git -C "$repo" checkout --quiet -b side
git -C "$repo" commit --quiet --allow-empty -m "unmerged side commit"
non_main_commit="$(git -C "$repo" rev-parse HEAD)"
git -C "$repo" checkout --quiet main_ref

fake_gh_dir="$root/bin"
mkdir -p "$fake_gh_dir"
export GITHUB_REPOSITORY="FixPortal/fixportal-fixatdl"
export GH_RESPONSE_MAP="$root/gh-response-map"
: > "$GH_RESPONSE_MAP"

# The final fixture for a SHA wins, allowing each case to drive the same eligible commit.
cat > "$fake_gh_dir/gh" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
# The real gh rejects --slurp combined with --jq; the gate fetches here and pipes to jq itself.
if [ "$1" != "api" ] || [ "$3" != "--paginate" ] || [ "$4" != "--slurp" ] || [ $# -ne 4 ]; then
  echo "fake gh: unexpected arguments: $*" >&2
  exit 1
fi
sha="$(echo "$2" | sed -E 's#.*/commits/([^/]+)/pulls#\1#')"
fixture="$(awk -v sha="$sha" '$1 == sha { $1 = ""; sub(/^ /, ""); answer = $0 } END { print answer }' "$GH_RESPONSE_MAP")"
if [ -z "$fixture" ]; then
  echo "fake gh: no canned answer for $sha" >&2
  exit 1
fi
printf '%s\n' "$fixture"
EOF
chmod +x "$fake_gh_dir/gh"
export PATH="$fake_gh_dir:$PATH"

fail() { echo "FAIL: $1" >&2; exit 1; }

echo "$main_eligible_commit [[{\"merged_at\":\"2026-09-01T12:00:00Z\",\"base\":{\"ref\":\"main\"}}]]" >> "$GH_RESPONSE_MAP"
echo "$non_main_commit []" >> "$GH_RESPONSE_MAP"

# Case 1: eligible - on main, and GitHub reports a merged PR into main.
if ! output="$("$gate" "$repo" "$main_eligible_commit" main_ref 2>&1)"; then
  fail "eligible commit was rejected:\n$output"
fi
echo "$output" | grep -q "is eligible for release" || fail "eligible-case output missing success line:\n$output"

# Case 1b: the gate peels an annotated tag object before checking ancestry and GitHub.
git -C "$repo" tag -a v1.1.6 -m "release tag" "$main_eligible_commit"
tag_object="$(git -C "$repo" rev-parse refs/tags/v1.1.6)"
if ! output="$("$gate" "$repo" "$tag_object" main_ref 2>&1)"; then
  fail "annotated tag was rejected:\n$output"
fi
echo "$output" | grep -q "is eligible for release" || fail "annotated-tag output missing success line:\n$output"

# Case 2: non-main commit - ancestry must reject it before the PR query.
if output="$("$gate" "$repo" "$non_main_commit" main_ref 2>&1)"; then
  fail "non-main commit was accepted:\n$output"
fi
echo "$output" | grep -q "not reachable from" || fail "non-main rejection had wrong message:\n$output"

# Case 3: a merged PR into another branch must not qualify.
echo "$main_eligible_commit [[{\"merged_at\":\"2026-09-01T12:00:00Z\",\"base\":{\"ref\":\"develop\"}}]]" >> "$GH_RESPONSE_MAP"
if output="$("$gate" "$repo" "$main_eligible_commit" main_ref 2>&1)"; then
  fail "commit merged into another branch was accepted:\n$output"
fi
echo "$output" | grep -q "no merged pull request" || fail "other-branch rejection had wrong message:\n$output"

# Case 4: an unmerged PR must not qualify.
echo "$main_eligible_commit [[{\"merged_at\":null,\"base\":{\"ref\":\"main\"}}]]" >> "$GH_RESPONSE_MAP"
if output="$("$gate" "$repo" "$main_eligible_commit" main_ref 2>&1)"; then
  fail "unmerged pull request was accepted:\n$output"
fi
echo "$output" | grep -q "no merged pull request" || fail "unmerged rejection had wrong message:\n$output"

# Case 5: no associated PRs must not qualify.
echo "$main_eligible_commit [[]]" >> "$GH_RESPONSE_MAP"
if output="$("$gate" "$repo" "$main_eligible_commit" main_ref 2>&1)"; then
  fail "commit with no merged PR was accepted:\n$output"
fi
echo "$output" | grep -q "no merged pull request" || fail "empty-response rejection had wrong message:\n$output"

# Case 6: a qualifying PR on a later page still qualifies.
echo "$main_eligible_commit [[{\"merged_at\":null,\"base\":{\"ref\":\"develop\"}}],[{\"merged_at\":\"2026-09-01T12:00:00Z\",\"base\":{\"ref\":\"main\"}}]]" >> "$GH_RESPONSE_MAP"
if ! output="$("$gate" "$repo" "$main_eligible_commit" main_ref 2>&1)"; then
  fail "eligible commit with a later-page merged PR was rejected:\n$output"
fi
echo "$output" | grep -q "is eligible for release" || fail "later-page eligible output missing success line:\n$output"

echo "verify-release-eligibility.sh OK - annotated tag, later-page main merge, other-base merge, unmerged, empty, and non-main outcomes all observed"
