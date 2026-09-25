#!/usr/bin/env bash
# Exercises assert-release-eligibility.sh against a real, disposable git history for
# every eligibility outcome: it must observe the gate's actual pass/fail exit code and
# stderr, never assert on release.yml's YAML text. A test that only checked for command
# text could stay green if the gate were made non-blocking or its condition weakened;
# this drives the real script so weakening either condition (ancestry or merged-PR) is
# caught here.
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

# Fake `gh` reads a per-commit canned answer from GH_RESPONSE_MAP so each case controls
# whether the merged-PR check reports true or false, without a network call.
cat > "$fake_gh_dir/gh" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
# args: api repos/<repo>/commits/<sha>/pulls --jq '...'
sha="$(echo "$2" | sed -E 's#.*/commits/([^/]+)/pulls#\1#')"
answer="$(grep "^${sha} " "$GH_RESPONSE_MAP" | tail -n1 | cut -d' ' -f2 || true)"
if [ -z "$answer" ]; then
  echo "fake gh: no canned answer for $sha" >&2
  exit 1
fi
echo "$answer"
EOF
chmod +x "$fake_gh_dir/gh"
export PATH="$fake_gh_dir:$PATH"

fail() { echo "FAIL: $1" >&2; exit 1; }

echo "$main_eligible_commit true" >> "$GH_RESPONSE_MAP"
echo "$non_main_commit false" >> "$GH_RESPONSE_MAP"

# Case 1: eligible - on main, and GitHub reports a merged PR into main.
if ! output="$("$gate" "$repo" "$main_eligible_commit" main_ref 2>&1)"; then
  fail "eligible commit was rejected:\n$output"
fi
echo "$output" | grep -q "is eligible for release" || fail "eligible-case output missing success line:\n$output"

# Case 2: non-main commit - ancestry check must reject it before the merged-PR check
# is even consulted (its canned answer above is false, so a weakened gate that skipped
# ancestry would still be caught by case 3, but this proves ancestry itself gates).
if output="$("$gate" "$repo" "$non_main_commit" main_ref 2>&1)"; then
  fail "non-main commit was accepted:\n$output"
fi
echo "$output" | grep -q "not reachable from" || fail "non-main rejection had wrong message:\n$output"

# Case 3: on main, but GitHub reports no merged PR into main.
echo "$main_eligible_commit false" >> "$GH_RESPONSE_MAP"
if output="$("$gate" "$repo" "$main_eligible_commit" main_ref 2>&1)"; then
  fail "commit with no merged PR was accepted:\n$output"
fi
echo "$output" | grep -q "no merged pull request" || fail "no-merged-PR rejection had wrong message:\n$output"

echo "verify-release-eligibility.sh OK - eligible/non-main/no-merged-PR outcomes all observed from the real gate script"
