#!/usr/bin/env python3
"""Execute the release workflow's bounded build/test/pack steps, without publishing.

The parser deliberately accepts the current, simple workflow shape and fails closed
on unsupported YAML/shell changes. Version derivation and commands come from the
workflow itself; this verifier never implements tag stripping or its own pack path.
"""

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess


def job(text, name):
    match = re.search(rf"(?ms)^  {re.escape(name)}:\s*\n(.*?)(?=^  [\w-]+:|\Z)", text)
    if not match:
        raise ValueError(f"Missing workflow job {name}")
    return match.group(1)


def step(block, name):
    matches = list(re.finditer(rf"(?m)^      - name: {re.escape(name)}\s*$", block))
    if len(matches) != 1:
        raise ValueError(f"Expected exactly one named step {name}")
    following = block[matches[0].end():]
    following = re.split(r"(?m)^      - ", following, maxsplit=1)[0]
    if re.search(r"(?m)^        (?:if|continue-on-error|working-directory|shell):", following):
        raise ValueError(f"{name}: conditional, ignored failure, or execution override is unsupported")
    match = re.search(r"(?m)^        run: (.+)$", following)
    if not match:
        raise ValueError(f"{name}: missing run command")
    raw = match.group(1).strip()
    if raw == "|":
        lines = following[match.end():].splitlines()
        body = []
        for line in lines:
            if not line.strip():
                continue
            if not line.startswith("          "):
                break
            body.append(line[10:])
        return "\n".join(body), following
    if raw.startswith(("|", ">", "'", '"')):
        raise ValueError(f"{name}: unsupported YAML run scalar")
    return raw, following


def check_wiring(root):
    ci = (root / ".github/workflows/ci.yml").read_text()
    for name in ("build-and-test", "gate-coverage"):
        if re.search(r"(?m)^    (?:if|continue-on-error|defaults|env):", job(ci, name)):
            raise ValueError(f"{name}: package verification job must be unconditional without execution overrides")
    command, _ = step(job(ci, "build-and-test"), "Verify tag-stamped release packages")
    if command != "python3 scripts/test/verify-release-package.py":
        raise ValueError("Required build-and-test job must execute the real package verifier")
    command, _ = step(job(ci, "gate-coverage"), "Verify release-package gate wiring")
    if command != "python3 scripts/test/verify-release-package.py --check-wiring":
        raise ValueError("Independent gate-coverage job must preserve package-verifier wiring")
    high = json.loads((root / ".claude/review-policy.json").read_text())["high"]
    for name in ("scripts/test/verify-release-package.py", "scripts/test/ReleasePackageProbe/Program.cs"):
        if name not in high:
            raise ValueError(f"Package verification must remain HIGH: {name}")


def release_blocks(root):
    release = (root / ".github/workflows/release.yml").read_text()
    block = job(release, "pack-and-release")
    if not re.search(r"(?m)^    runs-on: ubuntu-latest\s*$", block):
        raise ValueError("Only the current Linux release execution context is supported")
    if re.search(r"(?m)^    defaults:", block):
        raise ValueError("Release job execution defaults are unsupported")
    names = ("Restore", "Build (Release)", "Test (Release)", "Pack")
    positions = [re.search(rf"(?m)^      - name: {re.escape(name)}\s*$", block).start() for name in names]
    if positions != sorted(positions):
        raise ValueError("Release Restore/Build/Test/Pack order changed")
    result = []
    for name in ("Restore", "Build (Release)", "Test (Release)", "Pack"):
        body, metadata = step(block, name)
        lines = body.splitlines()
        if name in ("Build (Release)", "Pack"):
            if not re.search(r"(?m)^          REF_NAME: \$\{\{ github.ref_name \}\}\s*$", metadata):
                raise ValueError(f"{name}: REF_NAME must come from the actual release tag")
            if not re.fullmatch(r"VERSION=(?:\$\{REF_NAME(?:[#%]{1,2}[A-Za-z0-9._-]+)?\}|[A-Za-z0-9._-]+)", lines[0]):
                raise ValueError(f"{name}: unsupported version derivation command")
            commands = lines[1:]
        else:
            commands = lines
        allowed = {
            "Restore": [r"dotnet restore FixPortal\.FixAtdl\.slnx"],
            "Build (Release)": [r'dotnet build FixPortal\.FixAtdl\.slnx -c Release --no-restore(?: /p:Version="\$\{VERSION\}")?'],
            "Test (Release)": [r"dotnet test --solution FixPortal\.FixAtdl\.slnx -c Release --no-build"],
            "Pack": [rf'dotnet pack src/{re.escape(project)}/{re.escape(project)}\.csproj -c Release --no-build -o artifacts(?: /p:Version="\$\{{VERSION\}}")?' for project in ("FixPortal.FixAtdl", "FixPortal.FixAtdl.Contracts")],
        }[name]
        if len(commands) != len(allowed) or any(not re.fullmatch(pattern, command) for pattern, command in zip(allowed, commands)):
            raise ValueError(f"{name}: unsupported release command; publishing and arbitrary shell are forbidden")
        result.append((name, body))
    print(f"Actual release workflow SHA256 {hashlib.sha256(release.encode()).hexdigest()}", flush=True)
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--check-wiring", action="store_true")
    args = parser.parse_args()
    root = args.root.resolve()
    check_wiring(root)
    if args.check_wiring:
        print("Release-package verifier is wired into required CI and protected HIGH")
        return
    blocks = release_blocks(root)
    if (root / "artifacts").exists():
        raise ValueError("Release proof requires a fresh snapshot: artifacts already exists; nothing will be deleted")
    env = {name: value for name, value in os.environ.items() if name in {"PATH", "HOME", "LANG", "LC_ALL", "DOTNET_ROOT", "DOTNET_CLI_HOME", "NUGET_PACKAGES"}}
    env["REF_NAME"] = "v9.8.7-audit.1"
    env["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
    for name, body in blocks:
        print(f"Executing actual release step {name}:\n{body}", flush=True)
        subprocess.run(["bash", "-euo", "pipefail", "-c", body], cwd=root, env=env, check=True)
    subprocess.run(["dotnet", "run", "--project", str(root / "scripts/test/ReleasePackageProbe/ReleasePackageProbe.csproj"), "-c", "Release", "--", str(root)], cwd=root, env=env, check=True)


if __name__ == "__main__":
    main()
