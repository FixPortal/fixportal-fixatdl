#!/usr/bin/env bash
# Fails unless a tagged commit is (1) reachable from main and (2) associated by GitHub
# with a merged pull request into main. A ref gate on refs/tags/v*.*.* only proves the
# tag's SHAPE, never that the commit was reviewed -- git push origin v1.2.3 can put the
# tag on any commit, including one that never opened a PR. Rulesets target branches, not
# tags, so this asserts reachability itself and fails closed.
#
# Exercised standalone by scripts/test/verify-release-eligibility.sh (both the pass case
# and each rejection case) and invoked for real by .github/workflows/release.yml.
set -euo pipefail

repo_dir="${1:?usage: assert-release-eligibility.sh <repo-dir> <tagged-ref> <main-ref>}"
tagged_ref="${2:?usage: assert-release-eligibility.sh <repo-dir> <tagged-ref> <main-ref>}"
main_ref="${3:?usage: assert-release-eligibility.sh <repo-dir> <tagged-ref> <main-ref>}"
repository="${GITHUB_REPOSITORY:?GITHUB_REPOSITORY env var required}"

cd "$repo_dir"

# PEEL the ref first. For an ANNOTATED or signed tag, the ref names the tag OBJECT, not
# a commit, and merge-base then fails with a confusing message rather than the intended
# one. `^{commit}` peels a tag object and is the identity for a plain commit sha, so this
# is correct either way.
tagged_commit="$(git rev-parse "${tagged_ref}^{commit}")"

if ! git merge-base --is-ancestor "$tagged_commit" "$main_ref"; then
  echo "::error::Commit ${tagged_commit} is not reachable from ${main_ref}. Only reviewed, merged commits may be released."
  exit 1
fi

# Reachability alone still permits a tag to select an arbitrary older main commit.
# Require GitHub to associate the commit with a merged PR into main as a second,
# API-backed review boundary.
merged_pr="$(gh api "repos/${repository}/commits/${tagged_commit}/pulls" --jq 'any(.[]; .merged_at != null and .base.ref == "main")')"
if [ "$merged_pr" != "true" ]; then
  echo "::error::Commit ${tagged_commit} names a main commit with no merged pull request into main."
  exit 1
fi

echo "Commit ${tagged_commit} is eligible for release: reachable from ${main_ref} with a merged PR into main."
