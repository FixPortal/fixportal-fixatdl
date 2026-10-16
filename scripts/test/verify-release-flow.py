#!/usr/bin/env python3
"""Run real workflow release shell with only the external gh boundary substituted."""
import copy
import json
from pathlib import Path
import re
import shlex
import subprocess
import sys
import tempfile
import unittest
import yaml

ROOT = Path(__file__).resolve().parents[2]
COMMAND = "python3 scripts/test/verify-release-flow.py"

class UniqueLoader(yaml.BaseLoader):
    pass

def mapping(loader, node):
    result = {}
    for key, value in node.value:
        name = loader.construct_object(key)
        if name in result:
            raise ValueError(f"Duplicate YAML key: {name}")
        result[name] = loader.construct_object(value)
    return result

UniqueLoader.add_constructor(yaml.resolver.BaseResolver.DEFAULT_MAPPING_TAG, mapping)

def load(text):
    return yaml.load(text, Loader=UniqueLoader)

def require(condition, message):
    if not condition:
        raise ValueError(message)

def active(value):
    require(not any(key in value for key in ("if", "continue-on-error", "defaults", "working-directory", "needs")), "Execution must be unconditional and fail closed")
    require(value.get("shell", "bash") == "bash", "Unsupported shell")

def step(job, name):
    found = [s for s in job["steps"] if s.get("name") == name]
    require(len(found) == 1, f"Missing unique actual step: {name}")
    active(found[0])
    return found[0]

def tokens(body):
    return [shlex.split(line, comments=True) for line in body.replace("\\\n", " ").splitlines() if shlex.split(line, comments=True)]

def release_body(release):
    job = release["jobs"]["pack-and-release"]
    active(job)
    require(not any(k in release for k in ("defaults", "env")) and "env" not in job, "Unsupported inherited execution context")
    action = step(job, "Create GitHub Release")
    require(action.get("env") == {"GITHUB_TOKEN": "${{ github.token }}", "RELEASE_TAG": "${{ github.ref_name }}"}, "Actual release step must receive token and tag")
    body = action["run"]
    lines = [line.strip() for line in body.splitlines() if line.strip() and not line.lstrip().startswith("#")]
    require(len(lines) == 5 and lines[0] == 'if gh release view "$RELEASE_TAG" > /dev/null 2>&1; then' and lines[2] == "else" and lines[4] == "fi", "Only bounded gh release branch shell may execute")
    for line in (lines[1], lines[3]):
        require(re.fullmatch(r'gh release (?:upload|create) "\$RELEASE_TAG" artifacts/\*\.nupkg(?: --(?:clobber|generate-notes))?', line), "Only bounded gh upload/create may execute")
    return body

def contract(release, ci, high):
    require(release.get("on") == {"push": {"tags": ["v*.*.*"]}}, "Release must remain tag only")
    body = release_body(release)
    job = release["jobs"]["pack-and-release"]
    guard = step(job, "Assert the tagged commit is reviewed and reachable from main")
    require(guard.get("env") == {"GH_TOKEN": "${{ github.token }}"}, "Eligibility token scope changed")
    require(tokens(guard["run"]) == [["set", "-euo", "pipefail"], ["git", "fetch", "--no-tags", "origin", "main"], ["./scripts/assert-release-eligibility.sh", "$GITHUB_WORKSPACE", "$GITHUB_SHA", "FETCH_HEAD"]], "Eligibility requires exact fail-closed full-fetch/helper commands")
    positions = [job["steps"].index(step(job, name)) for name in ("Assert the tagged commit is reviewed and reachable from main", "Restore", "Build (Release)", "Test (Release)", "Pack", "Push to GitHub Packages", "Push to NuGet.org", "Create GitHub Release")]
    require(positions == sorted(positions), "Eligibility/build/pack/publication/release order changed")
    require(all(not s.get("run") for s in job["steps"][:positions[0]]), "Eligibility must precede other executable commands")
    require(step(job, "Pack").get("run", "").count("dotnet pack ") == 2, "Both packages must precede release")
    jobs = ci["jobs"]
    gate = jobs["gate-coverage"]
    active(gate)
    require("env" not in gate and not any(k in ci for k in ("env", "defaults")), "Unsupported CI execution inheritance")
    require(ci.get("on", {}).get("pull_request") == {"branches": ["main"]}, "PR checks must reach main")
    require("gate-coverage" in jobs["ci-gate"]["needs"] and jobs["ci-gate"].get("if") == "always()", "Release contracts must gate merge")
    check = step(gate, "Test actual release idempotency and workflow coupling")
    require(check.get("run") == COMMAND and not check.get("env"), "Required job must execute actual release contract")
    build = jobs["build-and-test"]
    active(build)
    existing = step(build, "Test the release-eligibility script")
    require(existing.get("run") == "./scripts/test/verify-release-eligibility.sh" and not existing.get("env"), "Real Git eligibility outcomes must run before merge")
    require("scripts/**" in high or "scripts/test/verify-release-flow.py" in high, "Release contract must remain HIGH")
    return body

class ReleaseFlowTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.release = load((ROOT / ".github/workflows/release.yml").read_text())
        cls.ci = load((ROOT / ".github/workflows/ci.yml").read_text())
        cls.high = json.loads((ROOT / ".claude/review-policy.json").read_text())["high"]

    def test_actual_workflow_coupling(self):
        contract(self.release, self.ci, self.high)

    def test_actual_release_shell_existing_missing_and_action_failure(self):
        body = release_body(self.release)
        for view, action, failure in [(0, "upload", 0), (1, "create", 0), (0, "upload", 17), (1, "create", 19)]:
            with self.subTest(view=view, action=action, exit=failure), tempfile.TemporaryDirectory(prefix="fixatdl-release-flow-") as current:
                root = Path(current)
                print(f"Current-test-owned temporary cleanup: {root.resolve()}", flush=True)
                owned = root / "artifacts"
                owned.mkdir()
                for name in ("Core.9.8.7.nupkg", "Contracts.9.8.7.nupkg"):
                    (owned / name).write_text("current test package witness")
                binary = root / "bin"
                binary.mkdir()
                gh = binary / "gh"
                gh.write_text("#!" + sys.executable + "\nimport json, os, sys\nwith open(os.environ['CALLS'], 'a') as log: log.write(json.dumps(sys.argv[1:]) + '\\n')\nsys.exit(int(os.environ['VIEW_EXIT'] if sys.argv[2] == 'view' else os.environ['ACTION_EXIT']))\n")
                gh.chmod(0o755)
                calls = root / "calls.jsonl"
                env = {"PATH": str(binary) + ":/usr/bin:/bin", "RELEASE_TAG": "v9.8.7", "CALLS": str(calls), "VIEW_EXIT": str(view), "ACTION_EXIT": str(failure)}
                result = subprocess.run(["bash", "--noprofile", "--norc", "-e", "-o", "pipefail", "-c", body], cwd=root, env=env, capture_output=True, text=True, timeout=10)
                self.assertEqual(result.returncode, failure, result.stderr)
                observed = [json.loads(line) for line in calls.read_text().splitlines()]
                expected = [["release", "view", "v9.8.7"], ["release", action, "v9.8.7", "artifacts/Contracts.9.8.7.nupkg", "artifacts/Core.9.8.7.nupkg", "--clobber" if action == "upload" else "--generate-notes"]]
                self.assertEqual(observed, expected)

    def test_negative_workflow_fixtures(self):
        def guard(d):
            return step(d["jobs"]["pack-and-release"], "Assert the tagged commit is reviewed and reachable from main")
        def action(d):
            return step(d["jobs"]["pack-and-release"], "Create GitHub Release")
        cases = {
            "comment-only helper": lambda r, c: guard(r).update(run="# " + guard(r)["run"].replace("\n", "\n# ")),
            "remove helper step": lambda r, c: r["jobs"]["pack-and-release"]["steps"].remove(guard(r)),
            "shallow fetch": lambda r, c: guard(r).update(run=guard(r)["run"].replace("git fetch --no-tags", "git fetch --depth=1 --no-tags")),
            "wrong mainline": lambda r, c: guard(r).update(run=guard(r)["run"].replace("FETCH_HEAD", "HEAD")),
            "early successful exit": lambda r, c: guard(r).update(run=guard(r)["run"].replace("git fetch --no-tags origin main", "git fetch --no-tags origin main\nexit 0")),
            "ignored guard": lambda r, c: guard(r).update({"continue-on-error": "true"}),
            "conditional guard": lambda r, c: guard(r).update({"if": "false"}),
            "after restore": lambda r, c: r["jobs"]["pack-and-release"]["steps"].append(r["jobs"]["pack-and-release"]["steps"].pop(r["jobs"]["pack-and-release"]["steps"].index(guard(r)))),
            "wrong release tag env": lambda r, c: action(r)["env"].update(RELEASE_TAG="${{ github.sha }}"),
            "wrong token env": lambda r, c: action(r)["env"].update(GITHUB_TOKEN="${{ secrets.UNRELATED }}"),
            "comment-only release": lambda r, c: action(r).update(run="# gh release upload tag artifacts/*.nupkg --clobber"),
            "tag-dependent contract job": lambda r, c: c["jobs"]["gate-coverage"].update(needs=["release"]),
            "conditional contract job": lambda r, c: c["jobs"]["gate-coverage"].update({"if": "false"}),
            "remove required contract": lambda r, c: step(c["jobs"]["gate-coverage"], "Test actual release idempotency and workflow coupling").update(run="echo disabled"),
            "remove PR helper selftest": lambda r, c: step(c["jobs"]["build-and-test"], "Test the release-eligibility script").update(run="echo disabled"),
            "remove required needs": lambda r, c: c["jobs"]["ci-gate"].update(needs=["build-and-test"]),
        }
        for name, mutate in cases.items():
            with self.subTest(name=name):
                release, ci = copy.deepcopy(self.release), copy.deepcopy(self.ci)
                mutate(release, ci)
                with self.assertRaises(ValueError):
                    contract(release, ci, self.high)

    def test_duplicate_keys_and_high_removal_fail_closed(self):
        with self.assertRaisesRegex(ValueError, "Duplicate YAML key"):
            load("jobs:\n  pack: {}\n  pack: {}")
        with self.assertRaisesRegex(ValueError, "HIGH"):
            contract(self.release, self.ci, [])

if __name__ == "__main__":
    unittest.main(verbosity=2)
