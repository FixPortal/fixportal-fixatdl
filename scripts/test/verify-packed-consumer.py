#!/usr/bin/env python3
"""Restore and execute both freshly packed packages through their public APIs."""

import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import xml.etree.ElementTree as ET

VERSION = "9.8.7-audit.1"
IDS = ("FixPortal.FixAtdl", "FixPortal.FixAtdl.Contracts")


def run(root, env, dependency_source="https://api.nuget.org/v3/index.json"):
    artifacts = root / "artifacts"
    source = root / "scripts/test/PackedConsumer/Program.cs"
    stage = artifacts / "packed-consumer"
    if artifacts.is_symlink() or not artifacts.is_dir() or source.is_symlink() or not source.is_file():
        raise ValueError("Consumer requires ordinary packed artifacts and maintained source")
    # A fresh cache is essential: source mapping does not apply to already-cached packages.
    stage.mkdir()
    cache = stage / "packages"
    project = stage / "consumer.csproj"
    project.write_text("""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
    <IsPackable>false</IsPackable>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="Program.cs" />
    <PackageReference Include="FixPortal.FixAtdl" Version="9.8.7-audit.1" />
    <PackageReference Include="FixPortal.FixAtdl.Contracts" Version="9.8.7-audit.1" />
  </ItemGroup>
</Project>
""", encoding="utf-8")
    for name in ("Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props"):
        (stage / name).write_text("<Project />\n", encoding="utf-8")
    shutil.copyfile(source, stage / "Program.cs")
    config = ET.Element("configuration")
    sources = ET.SubElement(config, "packageSources")
    ET.SubElement(sources, "clear")
    ET.SubElement(sources, "add", key="packed", value=str(artifacts.resolve()))
    ET.SubElement(sources, "add", key="dependencies", value=dependency_source)
    mapping = ET.SubElement(config, "packageSourceMapping")
    ET.SubElement(mapping, "clear")
    packed = ET.SubElement(mapping, "packageSource", key="packed")
    for package_id in IDS:
        ET.SubElement(packed, "package", pattern=package_id)
    dependencies = ET.SubElement(mapping, "packageSource", key="dependencies")
    ET.SubElement(dependencies, "package", pattern="*")
    config_path = stage / "NuGet.Config"
    ET.ElementTree(config).write(config_path, encoding="utf-8", xml_declaration=True)
    command = ["dotnet", "restore", str(project), "--configfile", str(config_path), "--packages", str(cache)]
    subprocess.run(command, cwd=stage, env=env, check=True, timeout=120)
    assets_path = stage / "obj/project.assets.json"
    assets = json.loads(assets_path.read_text(encoding="utf-8"))
    if {str(Path(path).resolve()) for path in assets["packageFolders"]} != {str(cache.resolve())}:
        raise ValueError("Consumer restore escaped its fresh private package cache")
    actual_sources = set(assets["project"]["restore"]["sources"])
    if actual_sources != {str(artifacts.resolve()), dependency_source}:
        raise ValueError("Consumer restore used unexpected package sources")
    for package_id in IDS:
        key = f"{package_id}/{VERSION}"
        if key not in assets["libraries"]:
            raise ValueError(f"Consumer did not resolve fresh {key}")
        original = artifacts / f"{package_id}.{VERSION}.nupkg"
        cached = cache / package_id.lower() / VERSION / f"{package_id.lower()}.{VERSION}.nupkg"
        if original.is_symlink() or not original.is_file() or cached.is_symlink() or not cached.is_file():
            raise ValueError(f"Consumer requires ordinary archive files for {package_id}")
        expected = hashlib.sha256(original.read_bytes()).hexdigest()
        if hashlib.sha256(cached.read_bytes()).hexdigest() != expected:
            raise ValueError(f"Consumer did not restore actual packed bytes for {package_id}")
        print(f"Consumer actual {package_id} {VERSION} SHA256 {expected}", flush=True)
    qualified_inputs = {"obj/project.assets.json": hashlib.sha256(assets_path.read_bytes()).hexdigest()}
    for name in ("consumer.csproj.nuget.g.props", "consumer.csproj.nuget.g.targets"):
        path = stage / "obj" / name
        if path.is_symlink() or not path.is_file():
            raise ValueError(f"Missing ordinary consumer generated import {name}")
        raw = path.read_bytes()
        ET.fromstring(raw)
        qualified_inputs["obj/" + name] = hashlib.sha256(raw).hexdigest()
    (stage / "qualified-build-inputs.json").write_text(
        json.dumps(qualified_inputs, indent=2) + "\n", encoding="utf-8"
    )
    subprocess.run(["dotnet", "build", str(project), "-c", "Release", "--no-restore"], cwd=stage, env=env, check=True, timeout=120)
    subprocess.run(["dotnet", "run", "--project", str(project), "-c", "Release", "--no-build", "--no-restore"], cwd=stage, env=env, check=True, timeout=30)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, required=True)
    args = parser.parse_args()
    run(args.root.resolve(), os.environ.copy())


if __name__ == "__main__":
    main()
