#!/usr/bin/env python3
"""Fail closed if either real pack loses its released API baseline or integrity gate."""
import copy
import json
from pathlib import Path
import xml.etree.ElementTree as ET

PACKAGES = {
    "FixPortal.FixAtdl": "3DDA14D38CA75C7BD7C35EEA63B79EE4E8088DDE953E834E2F2343B925A980B1",
    "FixPortal.FixAtdl.Contracts": "C5FB054FF39FF5789DB246A66956BCD0F5CB780C5A64FC61E028BAA998CB1814",
}
CONDITION = "'$(PackageId)' == 'FixPortal.FixAtdl' or '$(PackageId)' == 'FixPortal.FixAtdl.Contracts'"
VALIDATION = "'$(EnablePackageValidation)' != 'true' or '$(RunApiCompat)' != 'true' or '$(DisablePackageBaselineValidation)' == 'true' or '$(PackageValidationBaselineVersion)' != '$(_ReleasedBaselineVersion)'"
BASELINE_PATH = "$([MSBuild]::NormalizePath('$(NuGetPackageRoot)', '$(PackageId.ToLower())', '$(_ReleasedBaselineVersion)', '$(PackageId.ToLower()).$(_ReleasedBaselineVersion).nupkg'))"


def require(value, message):
    if not value:
        raise ValueError(message)


def documents(root):
    return ({name: ET.parse(root / "src" / name / (name + ".csproj")).getroot() for name in PACKAGES},
            ET.parse(root / "Directory.Build.targets").getroot(),
            json.loads((root / ".claude/review-policy.json").read_text())["high"])


def contract(projects, targets, high):
    for name, project in projects.items():
        require(all(not group.attrib for group in project.findall("PropertyGroup")), "Package validation properties must be unconditional")
        for key, value in {"PackageId": name, "EnablePackageValidation": "true", "PackageValidationBaselineVersion": "1.4.0", "RunApiCompat": "true"}.items():
            nodes = project.findall("PropertyGroup/" + key)
            require(len(nodes) == 1 and nodes[0].text == value and not nodes[0].attrib, f"{name}: unconditional {key}={value} required")
        require(not project.findall(".//DisablePackageBaselineValidation") and not project.findall(".//ApiCompatSuppressionFile"), "Baseline validation must not be bypassed")
    found = targets.findall("Target")
    require(len(found) == 1, "Only the released baseline integrity target is supported")
    target = found[0]
    require(target.attrib == {"Name": "ValidateReleasedBaselineIntegrity", "BeforeTargets": "RunPackageValidation", "Condition": CONDITION}, "Integrity target must run before both actual SDK package validations")
    require([x.tag for x in target] == ["PropertyGroup", "Error", "Error", "GetFileHash", "Error", "Message"], "Integrity command shape/order changed")
    group = target[0]
    require(not group.attrib, "Integrity properties must be unconditional")
    require(all(set(target[i].attrib) == {"Condition", "Text"} and len(target[i]) == 0 for i in (1, 2, 4)), "Integrity errors must propagate without overrides")
    values = [(x.tag, x.text, x.attrib) for x in group]
    expected = [("_ReleasedBaselineVersion", "1.4.0", {}), ("_ReleasedBaselinePath", BASELINE_PATH, {})]
    expected += [("_ReleasedBaselineSha256", digest, {"Condition": f"'$(PackageId)' == '{name}'"}) for name, digest in PACKAGES.items()]
    expected += [("PackageValidationBaselinePath", "$(_ReleasedBaselinePath)", {})]
    require(values == expected, "Actual baseline path/version/release digests must be pinned for both packages")
    require(target[1].get("Condition") == VALIDATION and bool(target[1].get("Text")), "Validation must fail closed")
    require(target[2].get("Condition") == "!Exists('$(_ReleasedBaselinePath)')" and bool(target[2].get("Text")), "Missing baseline must fail")
    require(target[3].attrib == {"Files": "$(_ReleasedBaselinePath)", "Algorithm": "SHA256"}, "Hash actual baseline archive with SHA256")
    require(len(target[3]) == 1 and target[3][0].tag == "Output" and target[3][0].attrib == {"TaskParameter": "Hash", "PropertyName": "_ActualReleasedBaselineSha256"}, "Consume actual GetFileHash result")
    require(target[4].get("Condition") == "'$(_ActualReleasedBaselineSha256)' != '$(_ReleasedBaselineSha256)'" and bool(target[4].get("Text")), "Digest mismatch must fail")
    for path in ("Directory.Build.targets", "scripts/test/verify-api-baseline.py", "scripts/test/verify-release-package.py"):
        require(path in high, f"Executable API baseline gate must remain HIGH: {path}")


def verify(root):
    contract(*documents(root))


def add_suppression(project):
    group = ET.SubElement(project, "ItemGroup")
    ET.SubElement(group, "ApiCompatSuppressionFile", {"Include": "CompatibilitySuppressions.xml"})


def self_test(root):
    original = documents(root)
    mutations = []
    for name in PACKAGES:
        mutations += [(f"{name} disabled", lambda p, t, h, n=name: setattr(p[n].find("PropertyGroup/EnablePackageValidation"), "text", "false")),
                      (f"{name} baseline changed", lambda p, t, h, n=name: setattr(p[n].find("PropertyGroup/PackageValidationBaselineVersion"), "text", "1.3.0")),
                      (f"{name} package identity changed", lambda p, t, h, n=name: setattr(p[n].find("PropertyGroup/PackageId"), "text", "Unexpected.Package")),
                      (f"{name} suppression added", lambda p, t, h, n=name: add_suppression(p[n])),
                      (f"{name} baseline bypass added", lambda p, t, h, n=name: setattr(ET.SubElement(p[n].find("PropertyGroup"), "DisablePackageBaselineValidation"), "text", "true"))]
    mutations += [
        ("removed executable hook", lambda p, t, h: t[0].set("BeforeTargets", "Build")),
        ("conditional integrity", lambda p, t, h: t[0].set("Condition", "false")),
        ("wrong release digest", lambda p, t, h: setattr(t[0][0][2], "text", "0" * 64)),
        ("removed mismatch failure", lambda p, t, h: t[0].remove(t[0][4])),
        ("weakened digest condition", lambda p, t, h: t[0][4].set("Condition", "false")),
        ("hash another file", lambda p, t, h: t[0][3].set("Files", "README.md")),
        ("target no longer HIGH", lambda p, t, h: h.remove("Directory.Build.targets")),
    ]
    contract(*original)
    for name, mutate in mutations:
        current = copy.deepcopy(original)
        mutate(*current)
        try:
            contract(*current)
        except ValueError:
            continue
        raise ValueError("API baseline negative fixture survived: " + name)
    print(f"Released API baseline wiring: valid control + {len(mutations)} negative fixtures", flush=True)


if __name__ == "__main__":
    root = Path(__file__).resolve().parents[2]
    verify(root)
    self_test(root)
