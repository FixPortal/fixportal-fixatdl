# FixPortal.FixAtdl v0.1.0 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship `FixPortal.FixAtdl` v0.1.0 — a modernised .NET 10 fork of the unmaintained Atdl4net library, packaged as a NuGet, with a real xUnit v3 test suite, GitHub Actions CI/CD, Dependabot, and a branch-protected `main`. Consumed by QFSIM's Phase 1 ATDL Examiner as a `.nupkg` drop into `LocalPackages/`.

**Architecture:** Single SDK-style class library targeting `net10.0`. Kept modules from upstream: `Diagnostics/`, `Fix/`, `Model/`, `Configuration/`, `Resources/`, `Utility/`, `Validation/`, `Xml/`. Removed: WPF UI (`Wpf/`, `Notification/`, `Providers/`, `Atdl4net.ExampleApplication/`). Public surface for QFSIM: `StrategiesParser`, `Model.Elements.*`, `Validation.*`, `FixTagValuesProvider`. Modernisation is sequenced — *first* make it build on net10 with the old shape (so test infrastructure can stand up), *then* characterisation tests as a regression net, *then* nullable + language features + namespace rename. Reversing that order on an 8–12k LOC port would be reckless.

**Tech Stack:**
- .NET 10 (`net10.0`), C# latest
- xUnit v3 + AwesomeAssertions + NSubstitute for tests
- `Microsoft.Extensions.Logging.Abstractions` (replaces upstream `Common.Logging`)
- Centralised Package Management (`Directory.Packages.props`)
- Standard `Directory.Build.props` for common compiler settings
- `.editorconfig` from `E:\Documents\Training\Resources\.editorconfig` enforces style (file-scoped namespaces, expression-bodied members, etc.)
- GitHub Actions for CI (`build-and-test.yml`, `release.yml`)
- Dependabot for NuGet + GitHub Actions weekly updates
- GitHub branch protection on `main` (gated by CI + 1 review)

**Reference spec:** `D:\Centerprise\work\QFSIM\docs\superpowers\specs\2026-05-23-atdl-examiner-design.md` §4 (Phase 0). This plan implements that section in full.

---

## File structure (final shape at v0.1.0)

```
FixPortal.FixAtdl/                              repo root
├── .editorconfig                                copied verbatim from reference
├── .gitignore                                   updated for SDK-style + tooling
├── .github/
│   ├── workflows/
│   │   ├── build-and-test.yml                   CI on push/PR
│   │   └── release.yml                          pack + upload artifact on tag
│   └── dependabot.yml                           weekly nuget + actions updates
├── Directory.Build.props                        net10, nullable, warnings-as-errors, common props
├── Directory.Packages.props                     CPM — all package versions pinned here
├── FixPortal.FixAtdl.sln                        new SDK-style solution
├── README.md                                    rewritten — fork rationale, install, usage
├── LICENSE                                      MIT (preserved from upstream)
├── NOTICE                                       attribution to Steve Wilkinson / original Atdl4net
├── src/
│   └── FixPortal.FixAtdl/
│       ├── FixPortal.FixAtdl.csproj             SDK-style, packs to .nupkg
│       ├── Diagnostics/                         (moved & modernised)
│       ├── Fix/                                 (moved & modernised)
│       ├── Model/                               (moved & modernised)
│       ├── Configuration/FixAtdlOptions.cs      replaces ConfigurationSectionHandler glue
│       ├── Resources/                           .resx kept; .Designer.cs regenerated
│       ├── Utility/                             (moved & modernised)
│       ├── Validation/                          (moved & modernised)
│       └── Xml/                                 (moved & modernised)
└── tests/
    └── FixPortal.FixAtdl.Tests/
        ├── FixPortal.FixAtdl.Tests.csproj       xUnit v3 test project
        ├── Fixtures/                            sample ATDL XML files for tests
        │   ├── twap.xml
        │   ├── vwap.xml
        │   ├── pov.xml
        │   └── invalid-schema.xml
        ├── Parsing/                             tests for Xml.StrategiesParser
        ├── Model/                               tests for typed model
        ├── Validation/                          tests for schema + ref resolution
        ├── Fix/                                 tests for FixTagValuesProvider
        └── StateRules/                          tests for Edit evaluation
```

**Deleted from upstream:** `Atdl4net/Wpf/`, `Atdl4net/Notification/`, `Atdl4net/Providers/`, `Atdl4net/Properties/AssemblyInfo.cs`, `Atdl4net/Configuration/ConfigurationSectionHandler.cs`, `Atdl4net/Configuration/Atdl4netConfiguration.cs` (replaced), `Atdl4net.ExampleApplication/`, `Atdl4net/Atdl4net.csproj` (old), `Atdl4net/Atdl4net.VS2008-3.5.csproj`, `VS2010-4.0/` directory.

---

## Phase A — Build scaffolding & first compile

TDD doesn't apply yet — the "test" for these tasks is a clean `dotnet build`. We have to get net10 building before we can stand up xUnit. Working branch for this whole plan: `main` directly, with frequent commits (the repo is fresh; no protected branch yet — branch protection lands in Phase F).

### Task A1: Copy reference .editorconfig to repo root

**Files:**
- Create: `D:\Centerprise\work\FixPortal\FixAtdl\.editorconfig`

- [ ] **Step 1: Copy the reference file**

```powershell
Copy-Item E:\Documents\Training\Resources\.editorconfig D:\Centerprise\work\FixPortal\FixAtdl\.editorconfig
```

- [ ] **Step 2: Verify file is present**

```powershell
Test-Path D:\Centerprise\work\FixPortal\FixAtdl\.editorconfig
```
Expected: `True`

- [ ] **Step 3: Commit**

```powershell
git add .editorconfig
```
```powershell
git commit -m "chore: add .editorconfig (file-scoped namespaces, formatting rules)"
```

### Task A2: Add Directory.Build.props

**Files:**
- Create: `D:\Centerprise\work\FixPortal\FixAtdl\Directory.Build.props`

- [ ] **Step 1: Write the file**

```xml
<Project>
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsNotAsErrors></WarningsNotAsErrors>
    <NoWarn></NoWarn>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
    <SatelliteResourceLanguages>en</SatelliteResourceLanguages>
    <ProduceReferenceAssembly>true</ProduceReferenceAssembly>
    <DeterministicSourcePaths Condition="'$(GITHUB_ACTIONS)' == 'true'">true</DeterministicSourcePaths>
    <ContinuousIntegrationBuild Condition="'$(GITHUB_ACTIONS)' == 'true'">true</ContinuousIntegrationBuild>
  </PropertyGroup>
  <PropertyGroup Label="Package metadata">
    <Authors>FixPortal contributors; originally Steve Wilkinson (Atdl4net)</Authors>
    <Company>FixPortal</Company>
    <Copyright>Copyright (c) 2010-2011 Steve Wilkinson; modifications (c) 2026 FixPortal contributors. MIT.</Copyright>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageProjectUrl>https://github.com/FixPortal/FixAtdl</PackageProjectUrl>
    <RepositoryUrl>https://github.com/FixPortal/FixAtdl</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageTags>FIX;FIXatdl;Atdl4net;algorithmic-trading;FixPortal</PackageTags>
  </PropertyGroup>
</Project>
```

CS1591 (missing XML doc) is silenced globally for now; Phase E re-enables it for the main project specifically.

- [ ] **Step 2: Commit**

```powershell
git add Directory.Build.props
```
```powershell
git commit -m "chore: add Directory.Build.props (net10, nullable, warnings-as-errors)"
```

### Task A3: Add Directory.Packages.props with all package versions pinned

**Files:**
- Create: `D:\Centerprise\work\FixPortal\FixAtdl\Directory.Packages.props`

- [ ] **Step 1: Write the file**

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  <ItemGroup Label="Runtime">
    <PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />
  </ItemGroup>
  <ItemGroup Label="Test">
    <PackageVersion Include="xunit.v3" Version="3.0.0" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.1.0" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.13.0" />
    <PackageVersion Include="AwesomeAssertions" Version="9.4.0" />
    <PackageVersion Include="NSubstitute" Version="5.3.0" />
    <PackageVersion Include="coverlet.collector" Version="6.0.4" />
  </ItemGroup>
</Project>
```

Versions are best-guess current; the executing engineer should run `dotnet add package <id>` per project once to let NuGet pick the actual latest and update this file. The CPM mechanism enforces single-source-of-truth.

- [ ] **Step 2: Commit**

```powershell
git add Directory.Packages.props
```
```powershell
git commit -m "chore: add Directory.Packages.props for centralised package management"
```

### Task A4: Create new src/ layout and SDK-style csproj

**Files:**
- Create: `D:\Centerprise\work\FixPortal\FixAtdl\src\FixPortal.FixAtdl\FixPortal.FixAtdl.csproj`

- [ ] **Step 1: Create the directory**

```powershell
New-Item -ItemType Directory -Force -Path D:\Centerprise\work\FixPortal\FixAtdl\src\FixPortal.FixAtdl | Out-Null
```

- [ ] **Step 2: Write the csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>Atdl4net</RootNamespace>
    <AssemblyName>FixPortal.FixAtdl</AssemblyName>
    <PackageId>FixPortal.FixAtdl</PackageId>
    <Description>Modernised .NET 10 fork of Atdl4net — parser, model, validator and FIX-tag emitter for FIXatdl v1.1 strategy XML documents. Headless (no UI). Maintained by FixPortal.</Description>
    <Version>0.1.0</Version>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
  </ItemGroup>
</Project>
```

Note: `RootNamespace` stays `Atdl4net` for Phase A so files don't need editing yet. Renamed in Phase D.

- [ ] **Step 3: Commit**

```powershell
git add src/FixPortal.FixAtdl/FixPortal.FixAtdl.csproj
```
```powershell
git commit -m "chore: scaffold src/FixPortal.FixAtdl SDK-style project (RootNamespace still Atdl4net)"
```

### Task A5: Move kept source directories into src/FixPortal.FixAtdl/

**Files:**
- Move: `Atdl4net/Diagnostics/` → `src/FixPortal.FixAtdl/Diagnostics/`
- Move: `Atdl4net/Fix/` → `src/FixPortal.FixAtdl/Fix/`
- Move: `Atdl4net/Model/` → `src/FixPortal.FixAtdl/Model/`
- Move: `Atdl4net/Resources/` → `src/FixPortal.FixAtdl/Resources/`
- Move: `Atdl4net/Utility/` → `src/FixPortal.FixAtdl/Utility/`
- Move: `Atdl4net/Validation/` → `src/FixPortal.FixAtdl/Validation/`
- Move: `Atdl4net/Xml/` → `src/FixPortal.FixAtdl/Xml/`

- [ ] **Step 1: Use git mv to preserve history**

```powershell
git mv Atdl4net/Diagnostics src/FixPortal.FixAtdl/Diagnostics
```
```powershell
git mv Atdl4net/Fix src/FixPortal.FixAtdl/Fix
```
```powershell
git mv Atdl4net/Model src/FixPortal.FixAtdl/Model
```
```powershell
git mv Atdl4net/Resources src/FixPortal.FixAtdl/Resources
```
```powershell
git mv Atdl4net/Utility src/FixPortal.FixAtdl/Utility
```
```powershell
git mv Atdl4net/Validation src/FixPortal.FixAtdl/Validation
```
```powershell
git mv Atdl4net/Xml src/FixPortal.FixAtdl/Xml
```

- [ ] **Step 2: Commit**

```powershell
git commit -m "refactor: move kept source dirs into src/FixPortal.FixAtdl/"
```

### Task A6: Replace Configuration/ contents with FixAtdlOptions POCO

Upstream's `Configuration/Atdl4netConfiguration.cs` + `ConfigurationSectionHandler.cs` lean on `System.Configuration` (app.config XML sections), which is dead in modern .NET. Replacement is a plain POCO that consumers can construct and pass in.

**Files:**
- Delete: `Atdl4net/Configuration/Atdl4netConfiguration.cs`
- Delete: `Atdl4net/Configuration/ConfigurationSectionHandler.cs`
- Create: `src/FixPortal.FixAtdl/Configuration/FixAtdlOptions.cs`

- [ ] **Step 1: Look at the upstream configuration to capture intent**

```powershell
Get-Content Atdl4net/Configuration/Atdl4netConfiguration.cs
```
Expected: read the file to enumerate which knobs upstream exposed (likely strict-mode flag, schema-validation flag, missing-control behaviour). Capture them as POCO properties.

- [ ] **Step 2: Write the replacement**

```csharp
// FP Enhancement: 2026-05-23 — replaced System.Configuration-based loader with POCO; .NET 10 has no <configuration> section support.
namespace Atdl4net.Configuration;

public sealed class FixAtdlOptions
{
    public static FixAtdlOptions Default { get; } = new();

    /// <summary>When true, schema validation errors throw; when false they accumulate in the result.</summary>
    public bool StrictSchemaValidation { get; init; } = false;

    /// <summary>When true, unresolved EditRef / parameterRef references throw at parse time.</summary>
    public bool StrictReferenceResolution { get; init; } = true;

    /// <summary>When true, the parser rejects unknown control or parameter type names; when false it logs and skips.</summary>
    public bool RejectUnknownTypes { get; init; } = true;
}
```

The actual knobs may differ once you read the upstream file. Mirror what was there; do not invent new knobs.

- [ ] **Step 3: Delete the obsolete files**

```powershell
git rm Atdl4net/Configuration/Atdl4netConfiguration.cs
```
```powershell
git rm Atdl4net/Configuration/ConfigurationSectionHandler.cs
```

- [ ] **Step 4: Commit**

```powershell
git add src/FixPortal.FixAtdl/Configuration/FixAtdlOptions.cs
```
```powershell
git commit -m "refactor(config): replace System.Configuration glue with FixAtdlOptions POCO"
```

### Task A7: Replace Common.Logging usage with Microsoft.Extensions.Logging.Abstractions

Upstream uses `Common.Logging` 2.0 (last released ~2010). Modern equivalent is `Microsoft.Extensions.Logging.Abstractions` — the library takes an `ILogger<T>` or `ILoggerFactory` and consumers wire up Serilog / NLog / built-in / whatever.

**Files:**
- Modify: every file under `src/FixPortal.FixAtdl/` that has `using Common.Logging;`

- [ ] **Step 1: Find all Common.Logging usages**

```powershell
Select-String -Path src\FixPortal.FixAtdl\**\*.cs -Pattern "Common\.Logging" | Select-Object Path -Unique
```
Expected: a list of files using the old logging package.

- [ ] **Step 2: For each file, replace the using directive**

Pattern:
- `using Common.Logging;` → `using Microsoft.Extensions.Logging;`
- `private static readonly ILog Log = LogManager.GetLogger<T>();` → `private readonly ILogger<T> _log;` (injected via constructor)
- `Log.DebugFormat("...", args)` / `Log.Debug(...)` → `_log.LogDebug("...", args)`
- `Log.WarnFormat(...)` / `Log.Warn(...)` → `_log.LogWarning(...)`
- `Log.ErrorFormat(...)` / `Log.Error(...)` → `_log.LogError(...)`
- `Log.InfoFormat(...)` / `Log.Info(...)` → `_log.LogInformation(...)`

Where the upstream uses `LogManager.GetLogger<T>()` as a static field, refactor the class to take `ILogger<T>` via the constructor (or `NullLogger<T>.Instance` default). Static loggers are an anti-pattern in DI'd code.

For internal classes never exposed to consumers, an acceptable shortcut is `private readonly ILogger _log = NullLogger.Instance;` — but only as a stop-gap; flag it with `// FP Enhancement: TODO wire injected logger` and resolve in a follow-up.

- [ ] **Step 3: Verify no Common.Logging remains**

```powershell
Select-String -Path src\FixPortal.FixAtdl\**\*.cs -Pattern "Common\.Logging"
```
Expected: no output.

- [ ] **Step 4: Commit**

```powershell
git add src/FixPortal.FixAtdl/
```
```powershell
git commit -m "refactor(logging): swap Common.Logging for Microsoft.Extensions.Logging.Abstractions"
```

### Task A8: Delete the upstream remnants — Wpf, Notification, Providers, ExampleApp, old csprojs, VS2010-4.0 dir

**Files:**
- Delete: `Atdl4net/Wpf/`
- Delete: `Atdl4net/Notification/`
- Delete: `Atdl4net/Providers/`
- Delete: `Atdl4net/Properties/`
- Delete: `Atdl4net/Atdl4net.csproj`
- Delete: `Atdl4net/Atdl4net.VS2008-3.5.csproj`
- Delete: `Atdl4net/Configuration/` (now empty after Task A6)
- Delete: `Atdl4net/` (whole top-level dir — should be empty)
- Delete: `Atdl4net.ExampleApplication/`
- Delete: `VS2010-4.0/`

- [ ] **Step 1: Remove the directories and old project files**

```powershell
git rm -r Atdl4net/Wpf Atdl4net/Notification Atdl4net/Providers Atdl4net/Properties
```
```powershell
git rm Atdl4net/Atdl4net.csproj Atdl4net/Atdl4net.VS2008-3.5.csproj
```
```powershell
git rm -r Atdl4net/Configuration
```
```powershell
git rm -r Atdl4net.ExampleApplication
```
```powershell
git rm -r VS2010-4.0
```

- [ ] **Step 2: Confirm Atdl4net/ is gone**

```powershell
Test-Path Atdl4net
```
Expected: `False`. If anything remains, git rm it and rerun.

- [ ] **Step 3: Commit**

```powershell
git commit -m "refactor: strip WPF, Notification, Providers, example app, and legacy csproj scaffolding"
```

### Task A9: Add the new solution file and verify dotnet sln knows about the project

**Files:**
- Create: `D:\Centerprise\work\FixPortal\FixAtdl\FixPortal.FixAtdl.sln`

- [ ] **Step 1: Generate the solution and add the project**

```powershell
dotnet new sln -n FixPortal.FixAtdl
```
```powershell
dotnet sln FixPortal.FixAtdl.sln add src/FixPortal.FixAtdl/FixPortal.FixAtdl.csproj
```

- [ ] **Step 2: Verify**

```powershell
dotnet sln FixPortal.FixAtdl.sln list
```
Expected: lists `src\FixPortal.FixAtdl\FixPortal.FixAtdl.csproj`.

- [ ] **Step 3: Commit**

```powershell
git add FixPortal.FixAtdl.sln
```
```powershell
git commit -m "chore: add FixPortal.FixAtdl.sln"
```

### Task A10: First clean build — fix compile errors until green

This is the largest task in Phase A. Expect a multi-page error list on the first run. Fix in batches; commit per category.

**Files:**
- Modify: any file in `src/FixPortal.FixAtdl/` that fails to compile

- [ ] **Step 1: Run the build**

```powershell
dotnet build FixPortal.FixAtdl.sln -c Debug
```
Expected: errors. Categorise them.

- [ ] **Step 2: Fix .NET-framework-only API usages**

Common culprits and replacements:
- `System.Configuration.ConfigurationManager.AppSettings[...]` → not in net10 by default; if any survives Task A6, replace with `FixAtdlOptions` field.
- `[Serializable]` + `ISerializable` BinaryFormatter paths → drop the attribute and the implementation; BinaryFormatter is obsolete and refused in net10.
- `System.ComponentModel.Composition.*` (MEF) → should be gone with Providers; if any reference remains, delete the file or refactor away.
- `System.Drawing` non-public uses → remove unless genuinely needed.
- `Hashtable`, `ArrayList` → replace with `Dictionary<,>`, `List<>`.

For each fix, commit in a category-scoped batch:

```powershell
git add src/FixPortal.FixAtdl/
```
```powershell
git commit -m "fix(build): drop System.Configuration callsites (FixAtdlOptions covers them)"
```

- [ ] **Step 3: Fix Resources/.Designer.cs auto-generated files**

Upstream `Resources/*.Designer.cs` files are stale (VS2010 era). Delete them and let MSBuild regenerate from `.resx`:

```powershell
git rm src\FixPortal.FixAtdl\Resources\*.Designer.cs
```
```powershell
dotnet build FixPortal.FixAtdl.sln
```
If the regenerated files appear in `obj/`, no action. If MSBuild needs an `<EmbeddedResource>` item explicitly, add to csproj:

```xml
<ItemGroup>
  <EmbeddedResource Update="Resources\*.resx">
    <Generator>ResXFileCodeGenerator</Generator>
    <SubType>Designer</SubType>
  </EmbeddedResource>
</ItemGroup>
```

Commit:

```powershell
git add src/FixPortal.FixAtdl/
```
```powershell
git commit -m "fix(build): regenerate Resources designer files for SDK-style project"
```

- [ ] **Step 4: Fix nullable-reference warnings that block the build**

`<Nullable>enable</Nullable>` with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` means CS8600/CS8602/CS8603/CS8618 become errors. The .editorconfig suppresses CS8600/CS8603/CS8618 by default — good. Anything that still bleeds through (CS8602 etc.) needs *minimal* fixes in Phase A: just enough to build. Deep nullable analysis is Phase C work. For Phase A, sprinkle `!` (null-forgiving) where a value is provably non-null in context and add `// FP Enhancement: 2026-05-23 — nullable cleanup deferred to Phase C` next to it.

Commit:

```powershell
git add src/FixPortal.FixAtdl/
```
```powershell
git commit -m "fix(build): minimal nullable fixes to land first compile (deep cleanup in Phase C)"
```

- [ ] **Step 5: Verify clean build**

```powershell
dotnet build FixPortal.FixAtdl.sln -c Release
```
Expected: `Build succeeded. 0 Warning(s). 0 Error(s).`

- [ ] **Step 6: Commit a marker**

```powershell
git commit --allow-empty -m "milestone: Phase A complete — FixPortal.FixAtdl builds on net10"
```

---

## Phase B — Establish characterisation test base

Now there's something to test against. xUnit v3 + AwesomeAssertions + NSubstitute. Each test locks in observable behaviour we don't want to break during modernisation. Tests live in `tests/FixPortal.FixAtdl.Tests/`. Real ATDL XML fixtures from public sources (e.g. the FIX protocol website's sample TWAP/VWAP definitions).

### Task B1: Scaffold the test project

**Files:**
- Create: `tests/FixPortal.FixAtdl.Tests/FixPortal.FixAtdl.Tests.csproj`
- Create: `tests/FixPortal.FixAtdl.Tests/GlobalUsings.cs`

- [ ] **Step 1: Create directory and project**

```powershell
New-Item -ItemType Directory -Force -Path tests\FixPortal.FixAtdl.Tests | Out-Null
```
```powershell
dotnet new xunit3 -n FixPortal.FixAtdl.Tests -o tests\FixPortal.FixAtdl.Tests --force
```

The `--force` is OK because the directory is new and contains nothing yet.

- [ ] **Step 2: Edit the generated csproj to add references**

Replace contents of `tests/FixPortal.FixAtdl.Tests/FixPortal.FixAtdl.Tests.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="AwesomeAssertions" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="coverlet.collector" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\FixPortal.FixAtdl\FixPortal.FixAtdl.csproj" />
  </ItemGroup>
  <ItemGroup>
    <None Update="Fixtures\**\*.xml">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Write GlobalUsings**

```csharp
global using AwesomeAssertions;
global using NSubstitute;
global using Xunit;
```

- [ ] **Step 4: Add to solution**

```powershell
dotnet sln FixPortal.FixAtdl.sln add tests/FixPortal.FixAtdl.Tests/FixPortal.FixAtdl.Tests.csproj
```

- [ ] **Step 5: Verify it builds**

```powershell
dotnet build FixPortal.FixAtdl.sln
```
Expected: 0 errors.

- [ ] **Step 6: Commit**

```powershell
git add tests/ FixPortal.FixAtdl.sln
```
```powershell
git commit -m "test: scaffold FixPortal.FixAtdl.Tests (xUnit v3 + AwesomeAssertions + NSubstitute)"
```

### Task B2: Add ATDL XML fixtures

**Files:**
- Create: `tests/FixPortal.FixAtdl.Tests/Fixtures/twap.xml`
- Create: `tests/FixPortal.FixAtdl.Tests/Fixtures/vwap.xml`
- Create: `tests/FixPortal.FixAtdl.Tests/Fixtures/pov.xml`
- Create: `tests/FixPortal.FixAtdl.Tests/Fixtures/invalid-schema.xml`
- Create: `tests/FixPortal.FixAtdl.Tests/Fixtures/malformed.xml`

- [ ] **Step 1: Source TWAP fixture**

Take the canonical TWAP example from the FIXatdl v1.1 spec annex (or the FIX Protocol Limited sample documents). Save as `Fixtures/twap.xml`. This should contain at least one `<Strategy>` with multiple `<Parameter>` declarations, a `<StrategyLayout>`, and at least one `<StateRule>`.

If a known good fixture cannot be located quickly, hand-write a minimal one:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Strategies xmlns="http://www.fixprotocol.org/FIXatdl-1-1/Core"
            xmlns:val="http://www.fixprotocol.org/FIXatdl-1-1/Validation"
            xmlns:lay="http://www.fixprotocol.org/FIXatdl-1-1/Layout"
            xmlns:flow="http://www.fixprotocol.org/FIXatdl-1-1/Flow"
            xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Strategy name="TWAP" wireValue="TWAP" uiRep="TWAP" providerID="DEMO">
    <Parameter name="StartTime" xsi:type="UTCTimestamp_t" fixTag="168" use="required"/>
    <Parameter name="EndTime" xsi:type="UTCTimestamp_t" fixTag="126" use="required"/>
    <Parameter name="Participation" xsi:type="Percentage_t" fixTag="7700" use="optional"/>
    <StrategyLayout>
      <lay:StrategyPanel title="TWAP Parameters" orientation="VERTICAL" collapsible="false" border="LINE">
        <lay:Control ID="c_StartTime" xsi:type="lay:Clock_t" parameterRef="StartTime" label="Start"/>
        <lay:Control ID="c_EndTime"   xsi:type="lay:Clock_t" parameterRef="EndTime"   label="End"/>
        <lay:Control ID="c_Part"      xsi:type="lay:TextField_t" parameterRef="Participation" label="Participation %"/>
      </lay:StrategyPanel>
    </StrategyLayout>
  </Strategy>
</Strategies>
```

- [ ] **Step 2: Write the rest of the fixtures**

`vwap.xml` and `pov.xml`: variants of the above with different parameter sets — VWAP adds `BenchmarkPrice` (`Price_t`); POV adds `TargetPercentage` (`Percentage_t`) and a `StateRule` that disables `TargetPercentage` when an `Aggression` dropdown is set to `PASSIVE`.

`invalid-schema.xml`: well-formed XML but violates the schema — e.g. a `<Strategy>` with no `name` attribute.

`malformed.xml`: garbage that breaks XML parsing, e.g. unclosed tag.

- [ ] **Step 3: Commit**

```powershell
git add tests/FixPortal.FixAtdl.Tests/Fixtures/
```
```powershell
git commit -m "test: add ATDL XML fixtures (TWAP, VWAP, POV, invalid-schema, malformed)"
```

### Task B3: Write `StrategiesParserTests.cs` — happy-path parse

**Files:**
- Create: `tests/FixPortal.FixAtdl.Tests/Parsing/StrategiesParserTests.cs`

- [ ] **Step 1: Write the failing test for the happy path**

```csharp
namespace FixPortal.FixAtdl.Tests.Parsing;

using global::Atdl4net.Xml;

public class StrategiesParserTests
{
    [Fact]
    public async Task Parse_twap_fixture_yields_one_strategy_named_TWAP()
    {
        var xml = await File.ReadAllTextAsync("Fixtures/twap.xml");

        var parser = new StrategiesReader();
        var strategies = parser.Load(new StringReader(xml));

        strategies.Should().NotBeNull();
        strategies.Should().HaveCount(1);
        strategies[0].Name.Should().Be("TWAP");
    }
}
```

The actual entry-point type/method may differ (`StrategiesReader.Load(...)` vs `StrategiesParser.Parse(...)`). Look at `src/FixPortal.FixAtdl/Xml/StrategiesReader.cs` to confirm and adjust.

- [ ] **Step 2: Run the test**

```powershell
dotnet test --filter "FullyQualifiedName~StrategiesParserTests.Parse_twap_fixture_yields_one_strategy_named_TWAP"
```
Expected: PASS if the API surface matches; otherwise FAIL with a clear "no such method" — fix the call to match the real public method on `StrategiesReader`.

- [ ] **Step 3: Commit**

```powershell
git add tests/FixPortal.FixAtdl.Tests/Parsing/StrategiesParserTests.cs
```
```powershell
git commit -m "test(parsing): happy-path TWAP parse yields strategy named TWAP"
```

### Task B4: Extend `StrategiesParserTests` — parameter & control extraction

**Files:**
- Modify: `tests/FixPortal.FixAtdl.Tests/Parsing/StrategiesParserTests.cs`

- [ ] **Step 1: Add tests covering parameter, control, and state-rule extraction**

```csharp
[Fact]
public async Task Parse_twap_extracts_three_parameters_with_correct_fix_tags()
{
    var xml = await File.ReadAllTextAsync("Fixtures/twap.xml");
    var strategies = new StrategiesReader().Load(new StringReader(xml));

    var twap = strategies[0];
    twap.Parameters.Should().HaveCount(3);
    twap.Parameters.Select(p => p.Name).Should().BeEquivalentTo("StartTime", "EndTime", "Participation");
    twap.Parameters.Single(p => p.Name == "StartTime").FixTag.Should().Be(168);
    twap.Parameters.Single(p => p.Name == "EndTime").FixTag.Should().Be(126);
    twap.Parameters.Single(p => p.Name == "Participation").FixTag.Should().Be(7700);
}

[Fact]
public async Task Parse_twap_extracts_strategy_layout_with_three_controls()
{
    var xml = await File.ReadAllTextAsync("Fixtures/twap.xml");
    var strategies = new StrategiesReader().Load(new StringReader(xml));

    var layout = strategies[0].StrategyLayout;
    layout.Should().NotBeNull();
    var panel = layout!.RootPanel;
    panel.Controls.Should().HaveCount(3);
    panel.Controls.Select(c => c.ID).Should().BeEquivalentTo("c_StartTime", "c_EndTime", "c_Part");
}

[Fact]
public async Task Parse_pov_extracts_state_rule_on_target_percentage()
{
    var xml = await File.ReadAllTextAsync("Fixtures/pov.xml");
    var strategies = new StrategiesReader().Load(new StringReader(xml));

    var pov = strategies[0];
    var targetCtrl = pov.StrategyLayout!.RootPanel.Controls.Single(c => c.ParameterRef == "TargetPercentage");
    targetCtrl.StateRules.Should().HaveCount(1);
}
```

- [ ] **Step 2: Run and adjust to match actual API surface**

```powershell
dotnet test --filter "FullyQualifiedName~StrategiesParserTests"
```
Expected: all 4 PASS (or initially fail compilation if a property name is wrong — fix to match the real model).

- [ ] **Step 3: Commit**

```powershell
git add tests/FixPortal.FixAtdl.Tests/Parsing/StrategiesParserTests.cs
```
```powershell
git commit -m "test(parsing): cover parameter & control extraction, state-rule presence"
```

### Task B5: Write parser rejection tests for malformed & schema-invalid

**Files:**
- Create: `tests/FixPortal.FixAtdl.Tests/Parsing/StrategiesParserRejectionTests.cs`

- [ ] **Step 1: Write the tests**

```csharp
namespace FixPortal.FixAtdl.Tests.Parsing;

using global::Atdl4net.Xml;

public class StrategiesParserRejectionTests
{
    [Fact]
    public async Task Parse_malformed_xml_throws_xml_exception()
    {
        var xml = await File.ReadAllTextAsync("Fixtures/malformed.xml");

        var act = () => new StrategiesReader().Load(new StringReader(xml));

        act.Should().Throw<System.Xml.XmlException>();
    }

    [Fact]
    public async Task Parse_schema_invalid_xml_throws_or_records_validation_error()
    {
        var xml = await File.ReadAllTextAsync("Fixtures/invalid-schema.xml");

        var act = () => new StrategiesReader().Load(new StringReader(xml));

        // Confirm the actual behaviour: either it throws ValidationException,
        // or returns with an error collection. Whichever the current behaviour is,
        // assert it explicitly — that's what we're locking in.
        act.Should().Throw<Exception>();
    }
}
```

After the first run, replace the generic `Throw<Exception>()` with the actual exception type that bubbles up. The point of a characterisation test is to capture *whatever happens today* so modernisation doesn't silently change it.

- [ ] **Step 2: Run and adjust**

```powershell
dotnet test --filter "FullyQualifiedName~StrategiesParserRejectionTests"
```

- [ ] **Step 3: Commit**

```powershell
git add tests/FixPortal.FixAtdl.Tests/Parsing/StrategiesParserRejectionTests.cs
```
```powershell
git commit -m "test(parsing): characterisation tests for malformed and schema-invalid input"
```

### Task B6: Write `FixTagValuesProviderTests.cs` — FIX-tag emission

**Files:**
- Create: `tests/FixPortal.FixAtdl.Tests/Fix/FixTagValuesProviderTests.cs`

- [ ] **Step 1: Read the upstream entry point**

```powershell
Get-Content src\FixPortal.FixAtdl\Fix\FixFieldValueProvider.cs
```
Confirm the public surface for emitting StrategyParametersGrp (957/958/959/960). Adjust the test below to match.

- [ ] **Step 2: Write the test**

```csharp
namespace FixPortal.FixAtdl.Tests.Fix;

using global::Atdl4net.Xml;
using global::Atdl4net.Fix;

public class FixTagValuesProviderTests
{
    [Fact]
    public async Task Filled_twap_strategy_emits_expected_fix_tags()
    {
        var xml = await File.ReadAllTextAsync("Fixtures/twap.xml");
        var strategies = new StrategiesReader().Load(new StringReader(xml));
        var twap = strategies[0];

        twap.Parameters["StartTime"].SetValueFromString("20260101-09:30:00");
        twap.Parameters["EndTime"].SetValueFromString("20260101-16:00:00");
        twap.Parameters["Participation"].SetValueFromString("10");

        var fixValues = twap.GetFixTagValues();

        fixValues.Should().NotBeNull();
        fixValues.Should().Contain(t => t.Tag == 168 && t.Value == "20260101-09:30:00");
        fixValues.Should().Contain(t => t.Tag == 126 && t.Value == "20260101-16:00:00");
        fixValues.Should().Contain(t => t.Tag == 7700 && t.Value == "10");
    }
}
```

- [ ] **Step 3: Run, adjust API calls to match actual surface, then make pass**

```powershell
dotnet test --filter "FullyQualifiedName~FixTagValuesProviderTests"
```

- [ ] **Step 4: Commit**

```powershell
git add tests/FixPortal.FixAtdl.Tests/Fix/FixTagValuesProviderTests.cs
```
```powershell
git commit -m "test(fix): characterisation test for FIX tag emission from filled TWAP"
```

### Task B7: Write `EditEvaluatorTests.cs` — StateRule expression evaluation

**Files:**
- Create: `tests/FixPortal.FixAtdl.Tests/Validation/EditEvaluatorTests.cs`

- [ ] **Step 1: Read the upstream evaluator**

```powershell
Get-Content src\FixPortal.FixAtdl\Validation\EditEvaluator.cs
```
Identify the public evaluation API and the operator set it implements.

- [ ] **Step 2: Write a parameterised test**

```csharp
namespace FixPortal.FixAtdl.Tests.Validation;

using global::Atdl4net.Model.Elements;
using global::Atdl4net.Validation;

public class EditEvaluatorTests
{
    [Theory]
    [InlineData("EQ",  "100", "100",  true)]
    [InlineData("EQ",  "100", "101",  false)]
    [InlineData("NE",  "100", "101",  true)]
    [InlineData("GT",  "101", "100",  true)]
    [InlineData("LT",  "99",  "100",  true)]
    [InlineData("GE",  "100", "100",  true)]
    [InlineData("LE",  "100", "100",  true)]
    public void Single_edit_with_comparison_evaluates_as_expected(
        string op, string left, string right, bool expected)
    {
        var edit = new Edit_t
        {
            Operator = Enum.Parse<Operator_t>(op, ignoreCase: true),
            Field = "X",
            Value = right
        };
        var values = new Dictionary<string, object> { ["X"] = left };

        var result = edit.Evaluate(values);

        result.Should().Be(expected);
    }
}
```

The actual `Edit_t` constructor and `Evaluate` signature may differ. Adapt to the real model.

- [ ] **Step 3: Run and fix**

```powershell
dotnet test --filter "FullyQualifiedName~EditEvaluatorTests"
```

- [ ] **Step 4: Commit**

```powershell
git add tests/FixPortal.FixAtdl.Tests/Validation/EditEvaluatorTests.cs
```
```powershell
git commit -m "test(validation): characterisation tests for Edit comparison operators"
```

### Task B8: Write `SchemaValidationTests.cs`

**Files:**
- Create: `tests/FixPortal.FixAtdl.Tests/Validation/SchemaValidationTests.cs`

- [ ] **Step 1: Write the tests**

```csharp
namespace FixPortal.FixAtdl.Tests.Validation;

using global::Atdl4net.Xml;

public class SchemaValidationTests
{
    [Theory]
    [InlineData("Fixtures/twap.xml")]
    [InlineData("Fixtures/vwap.xml")]
    [InlineData("Fixtures/pov.xml")]
    public async Task Canonical_fixture_validates_against_fixatdl_v11_schema(string path)
    {
        var xml = await File.ReadAllTextAsync(path);

        var act = () => new StrategiesReader().Load(new StringReader(xml));

        act.Should().NotThrow();
    }

    [Fact]
    public async Task Schema_invalid_fixture_reports_validation_error()
    {
        var xml = await File.ReadAllTextAsync("Fixtures/invalid-schema.xml");

        // Capture today's actual behaviour — fill in once observed.
        var act = () => new StrategiesReader().Load(new StringReader(xml));

        act.Should().Throw<Exception>();
    }
}
```

- [ ] **Step 2: Run and adjust to actual exception type**

```powershell
dotnet test --filter "FullyQualifiedName~SchemaValidationTests"
```

- [ ] **Step 3: Commit**

```powershell
git add tests/FixPortal.FixAtdl.Tests/Validation/SchemaValidationTests.cs
```
```powershell
git commit -m "test(validation): schema validation pass/fail characterisation"
```

### Task B9: Full test run + Phase B milestone commit

- [ ] **Step 1: Run all tests**

```powershell
dotnet test FixPortal.FixAtdl.sln
```
Expected: all green. Output should show roughly 15–20 passing tests across Parsing, Fix, and Validation.

- [ ] **Step 2: Marker commit**

```powershell
git commit --allow-empty -m "milestone: Phase B complete — characterisation tests green (regression net in place)"
```

---

## Phase C — Code modernisation

Tests are now the safety net. Each modernisation pass: change → `dotnet test` → green → commit. Any red goes back immediately.

### Task C1: File-scoped namespaces across all source files

`.editorconfig` already mandates `csharp_style_namespace_declarations = file_scoped:error`. The build won't fail today because existing files are block-scoped (the analyzer flags but doesn't break). This task converts them in one mechanical sweep.

**Files:**
- Modify: every `.cs` file under `src/FixPortal.FixAtdl/`

- [ ] **Step 1: Run dotnet format to apply the rule**

```powershell
dotnet format style --severity info src/FixPortal.FixAtdl/FixPortal.FixAtdl.csproj
```

Expected: dotnet format rewrites every block-scoped namespace declaration as file-scoped, removes the extra indentation level, and saves the file. It may also touch other style points (using directive order, etc.) — that's fine.

- [ ] **Step 2: Build + test**

```powershell
dotnet build FixPortal.FixAtdl.sln -c Debug
```
Expected: 0 errors.

```powershell
dotnet test FixPortal.FixAtdl.sln
```
Expected: all green.

- [ ] **Step 3: Commit**

```powershell
git add src/FixPortal.FixAtdl/
```
```powershell
git commit -m "refactor: convert all source files to file-scoped namespaces"
```

### Task C2: Deep nullable annotation pass — Model/Types/

Begin nullable cleanup with `Model/Types/` (smallest, most self-contained). Each type file gets a careful pass: remove `!`-forgiving operators added in Phase A, properly annotate properties and method signatures.

**Files:**
- Modify: every file under `src/FixPortal.FixAtdl/Model/Types/`

- [ ] **Step 1: Walk each file; annotate properly**

Pattern: `public string Name { get; set; }` becomes one of:
- `public required string Name { get; init; }` — for properties that must be set by construction
- `public string? Name { get; init; }` — for genuinely optional properties
- `public string Name { get; init; } = "";` — for properties with a sensible default

Method signatures: parameters and return types similarly annotated. Mutation methods that take a value that may be null get `string?`; methods that return a value that's always present get a non-nullable return.

Remove `// FP Enhancement: 2026-05-23 — nullable cleanup deferred to Phase C` comments as you complete each file.

- [ ] **Step 2: Build + test**

```powershell
dotnet build FixPortal.FixAtdl.sln
```
```powershell
dotnet test FixPortal.FixAtdl.sln
```
Both green.

- [ ] **Step 3: Commit**

```powershell
git add src/FixPortal.FixAtdl/Model/Types/
```
```powershell
git commit -m "refactor(nullable): proper annotations on Model/Types/"
```

### Task C3: Deep nullable annotation pass — Model/Elements/

Same pattern as C2 but for `Model/Elements/`.

**Files:**
- Modify: every file under `src/FixPortal.FixAtdl/Model/Elements/`

- [ ] **Step 1: Apply C2's pattern**

- [ ] **Step 2: Build + test green**

```powershell
dotnet test FixPortal.FixAtdl.sln
```

- [ ] **Step 3: Commit**

```powershell
git add src/FixPortal.FixAtdl/Model/Elements/
```
```powershell
git commit -m "refactor(nullable): proper annotations on Model/Elements/"
```

### Task C4: Deep nullable annotation pass — Model/Controls/ and Model/Collections/

**Files:**
- Modify: every file under `src/FixPortal.FixAtdl/Model/Controls/` and `Model/Collections/`

- [ ] **Step 1: Apply same pattern**

- [ ] **Step 2: Build + test green**

```powershell
dotnet test FixPortal.FixAtdl.sln
```

- [ ] **Step 3: Commit**

```powershell
git add src/FixPortal.FixAtdl/Model/Controls/ src/FixPortal.FixAtdl/Model/Collections/
```
```powershell
git commit -m "refactor(nullable): proper annotations on Model/Controls/ and Model/Collections/"
```

### Task C5: Deep nullable annotation pass — Xml/

**Files:**
- Modify: every file under `src/FixPortal.FixAtdl/Xml/`

- [ ] **Step 1: Apply same pattern**

Pay particular attention to `StrategiesReader` and the `Serialization/` subtree — these touch reflection and `XElement` queries that often return null.

- [ ] **Step 2: Build + test green**

```powershell
dotnet test FixPortal.FixAtdl.sln
```

- [ ] **Step 3: Commit**

```powershell
git add src/FixPortal.FixAtdl/Xml/
```
```powershell
git commit -m "refactor(nullable): proper annotations on Xml/"
```

### Task C6: Deep nullable annotation pass — Validation/, Fix/, Diagnostics/, Utility/, Configuration/

**Files:**
- Modify: every remaining file in `src/FixPortal.FixAtdl/Validation/`, `Fix/`, `Diagnostics/`, `Utility/`, `Configuration/`

- [ ] **Step 1: Apply same pattern**

- [ ] **Step 2: Build + test green**

```powershell
dotnet test FixPortal.FixAtdl.sln
```

- [ ] **Step 3: Commit**

```powershell
git add src/FixPortal.FixAtdl/
```
```powershell
git commit -m "refactor(nullable): proper annotations across Validation/, Fix/, Diagnostics/, Utility/, Configuration/"
```

### Task C7: Confirm no nullable suppressions remain

- [ ] **Step 1: Search for residual null-forgiving operators tagged as deferred**

```powershell
Select-String -Path src\FixPortal.FixAtdl\**\*.cs -Pattern "nullable cleanup deferred"
```
Expected: no output. If any, fix and re-test.

- [ ] **Step 2: Search for `#pragma warning disable` left over from Phase A**

```powershell
Select-String -Path src\FixPortal.FixAtdl\**\*.cs -Pattern "pragma warning disable"
```
Expected: no output. The spec section 4.2 explicitly forbids these.

- [ ] **Step 3: Re-enable strict nullable warnings in Directory.Build.props**

Remove the `CS8600`, `CS8603`, `CS8618` suppressions from `.editorconfig` so they become errors:

Edit `.editorconfig` and change:
```
dotnet_diagnostic.CS8600.severity = none
dotnet_diagnostic.CS8603.severity = none
dotnet_diagnostic.CS8618.severity = none
```
to:
```
dotnet_diagnostic.CS8600.severity = error
dotnet_diagnostic.CS8603.severity = error
dotnet_diagnostic.CS8618.severity = error
```

- [ ] **Step 4: Build with strict nullable — expect green**

```powershell
dotnet build FixPortal.FixAtdl.sln -c Release
```
Expected: 0 errors. If any survive, fix the remaining sites.

```powershell
dotnet test FixPortal.FixAtdl.sln
```
Expected: green.

- [ ] **Step 5: Commit**

```powershell
git add .editorconfig src/FixPortal.FixAtdl/
```
```powershell
git commit -m "refactor(nullable): strict CS8600/8603/8618 enforcement re-enabled"
```

### Task C8: Adopt modern C# language features where they simplify

Sweep for opportunities. Don't force features that don't fit.

**Files:**
- Modify: as needed across `src/FixPortal.FixAtdl/`

- [ ] **Step 1: Run dotnet format style with all default analyzers**

```powershell
dotnet format src/FixPortal.FixAtdl/FixPortal.FixAtdl.csproj
```

Applies: target-typed `new()`, collection expressions where applicable, pattern matching simplifications, using-directive ordering, redundant qualifications removed, etc. — guided by the `.editorconfig`.

- [ ] **Step 2: Review and revert anything dotnet format made worse**

```powershell
git diff src/FixPortal.FixAtdl/
```
If a particular auto-rewrite hurts readability, `git checkout -- <file>` to revert and add a per-file suppression rather than the whole change.

- [ ] **Step 3: Build + test green**

```powershell
dotnet test FixPortal.FixAtdl.sln
```

- [ ] **Step 4: Commit**

```powershell
git add src/FixPortal.FixAtdl/
```
```powershell
git commit -m "refactor: apply modern C# language features (target-typed new, collection exprs, pattern matching)"
```

### Task C9: Manual primary-constructor pass on hot types

`.editorconfig` has primary-constructor preference as `none` severity — they don't auto-apply. Manually convert where they cleanly reduce ceremony.

**Files:**
- Modify: types that have a single non-trivial constructor that just assigns to readonly fields. Candidates often live under `Xml/Serialization/`, `Validation/`, and any internal helper classes.

- [ ] **Step 1: Convert candidate classes one at a time**

Example before:
```csharp
internal sealed class ElementFactory
{
    private readonly IServiceProvider _services;

    public ElementFactory(IServiceProvider services)
    {
        _services = services;
    }

    public T Create<T>() => (T)_services.GetService(typeof(T));
}
```

After:
```csharp
internal sealed class ElementFactory(IServiceProvider services)
{
    public T Create<T>() => (T)services.GetService(typeof(T));
}
```

Skip any type with multiple constructors, base-class initialisation that needs explicit `: base(...)`, or properties that depend on constructor logic beyond assignment.

- [ ] **Step 2: Build + test green per file or per small batch**

```powershell
dotnet test FixPortal.FixAtdl.sln
```

- [ ] **Step 3: Commit**

```powershell
git add src/FixPortal.FixAtdl/
```
```powershell
git commit -m "refactor: adopt primary constructors on suitable types"
```

### Task C10: Phase C milestone marker

- [ ] **Step 1: Confirm clean build + green tests**

```powershell
dotnet build FixPortal.FixAtdl.sln -c Release
```
```powershell
dotnet test FixPortal.FixAtdl.sln
```

- [ ] **Step 2: Marker commit**

```powershell
git commit --allow-empty -m "milestone: Phase C complete — nullable + file-scoped + modern C# features applied"
```

---

## Phase D — Namespace rename

Mechanical rename `Atdl4net.*` → `FixPortal.FixAtdl.*`. Tests reference `global::Atdl4net.*` today; they get updated in the same pass. The test suite is the safety net.

### Task D1: Rename namespace declarations and using directives

**Files:**
- Modify: every `.cs` file in `src/FixPortal.FixAtdl/` and `tests/FixPortal.FixAtdl.Tests/`
- Modify: `src/FixPortal.FixAtdl/FixPortal.FixAtdl.csproj` (RootNamespace property)

- [ ] **Step 1: Update RootNamespace in csproj**

```xml
<RootNamespace>FixPortal.FixAtdl</RootNamespace>
```

- [ ] **Step 2: Find-and-replace `Atdl4net` → `FixPortal.FixAtdl` in all .cs files**

```powershell
$files = Get-ChildItem -Path src,tests -Recurse -Filter *.cs
```
```powershell
foreach ($f in $files) { (Get-Content -LiteralPath $f.FullName -Raw) -replace 'namespace Atdl4net', 'namespace FixPortal.FixAtdl' -replace 'using Atdl4net', 'using FixPortal.FixAtdl' -replace 'global::Atdl4net', 'global::FixPortal.FixAtdl' | Set-Content -LiteralPath $f.FullName -Encoding utf8 }
```

Make these three replacements ONLY — anything else is dangerous (e.g. blanket replacing `Atdl4net` could trash a comment or a resource string that legitimately mentions the upstream name).

- [ ] **Step 3: Check for stragglers**

```powershell
Select-String -Path src\**\*.cs,tests\**\*.cs -Pattern "Atdl4net\." | Where-Object { $_.Line -notmatch "//|Originally" }
```
Expected: zero unmissable hits. Anything that comes up is either a legitimate reference (e.g. `// originally Atdl4net.X — see NOTICE`) or a missed rename.

- [ ] **Step 4: Build + test**

```powershell
dotnet build FixPortal.FixAtdl.sln
```
```powershell
dotnet test FixPortal.FixAtdl.sln
```
Both green.

- [ ] **Step 5: Commit**

```powershell
git add src/ tests/
```
```powershell
git commit -m "refactor(rename): Atdl4net.* -> FixPortal.FixAtdl.* (namespaces and usings)"
```

### Task D2: Rename anything tied to the old assembly name

- [ ] **Step 1: Search for residual literal `Atdl4net` outside comments**

```powershell
Select-String -Path src\**\*.cs -Pattern "\bAtdl4net\b" | Where-Object { $_.Line -notmatch "^\s*//" }
```
Expected: hits in places like exception type names (`Atdl4netException`), error message resource keys, etc. Decision: rename the C# type `Atdl4netException` → `FixAtdlException`; keep the upstream string identifiers in `.resx` files unchanged (they're loaded by resource key — renaming would break the bundled error catalogue without value).

- [ ] **Step 2: Rename `Atdl4netException.cs` → `FixAtdlException.cs`**

```powershell
git mv src/FixPortal.FixAtdl/Diagnostics/Exceptions/Atdl4netException.cs src/FixPortal.FixAtdl/Diagnostics/Exceptions/FixAtdlException.cs
```

Edit the file: rename the type, update all references.

```powershell
$files = Get-ChildItem -Path src,tests -Recurse -Filter *.cs
```
```powershell
foreach ($f in $files) { (Get-Content -LiteralPath $f.FullName -Raw) -replace '\bAtdl4netException\b', 'FixAtdlException' | Set-Content -LiteralPath $f.FullName -Encoding utf8 }
```

- [ ] **Step 3: Build + test green**

```powershell
dotnet test FixPortal.FixAtdl.sln
```

- [ ] **Step 4: Commit**

```powershell
git add src/ tests/
```
```powershell
git commit -m "refactor(rename): Atdl4netException -> FixAtdlException"
```

### Task D3: Phase D milestone marker

- [ ] **Step 1: Confirm all green**

```powershell
dotnet build FixPortal.FixAtdl.sln -c Release
```
```powershell
dotnet test FixPortal.FixAtdl.sln
```

- [ ] **Step 2: Marker commit**

```powershell
git commit --allow-empty -m "milestone: Phase D complete — namespace and assembly identity is FixPortal.FixAtdl"
```

---

## Phase E — XML doc comments + FP Enhancement provenance markers

### Task E1: Add XML doc comments to public API surface

`<GenerateDocumentationFile>true</GenerateDocumentationFile>` is on; CS1591 is currently suppressed globally. This task adds one-line docs to every public type and member, then re-enables CS1591 as an error.

**Files:**
- Modify: every `.cs` file in `src/FixPortal.FixAtdl/` with `public` types

- [ ] **Step 1: Enumerate public surface**

```powershell
Select-String -Path src\FixPortal.FixAtdl\**\*.cs -Pattern "^\s*public " | Group-Object Path | Sort-Object Count -Descending
```

- [ ] **Step 2: Add one-line `<summary>` to each public type and public member**

Per user `CLAUDE.md`: "one short line each". Example:

```csharp
/// <summary>Parses FIXatdl v1.1 strategy XML into a typed model tree.</summary>
public sealed class StrategiesReader { ... }
```

Don't write multi-line paragraphs. Don't restate the obvious from the name — only add real information.

- [ ] **Step 3: Remove CS1591 suppression from Directory.Build.props**

Edit `Directory.Build.props` and change:
```xml
<NoWarn>$(NoWarn);CS1591</NoWarn>
```
to:
```xml
<NoWarn></NoWarn>
```

- [ ] **Step 4: Build — must be clean**

```powershell
dotnet build FixPortal.FixAtdl.sln -c Release
```
Expected: 0 errors, 0 warnings. Any CS1591 means a public member is still undocumented — add the doc.

```powershell
dotnet test FixPortal.FixAtdl.sln
```
Still green.

- [ ] **Step 5: Commit**

```powershell
git add src/FixPortal.FixAtdl/ Directory.Build.props
```
```powershell
git commit -m "docs: one-line XML summaries on all public types and members; CS1591 enforced"
```

### Task E2: Add FP Enhancement provenance markers

Per spec section 4.2: every file with non-trivial changes from upstream gets a `// FP Enhancement: <date> — <one-line reason>` comment at the top.

**Files:**
- Modify: every file in `src/FixPortal.FixAtdl/` that has been modified meaningfully (almost all of them after Phases A–E)

- [ ] **Step 1: Add a single banner comment to the top of each non-trivially-modified file**

Format:
```csharp
// FP Enhancement: 2026-05-23 — nullable enabled, file-scoped namespace, MELA logging.
namespace FixPortal.FixAtdl.Xml;
...
```

The reason field is one short clause summarising what changed about *this file*. Files that only had a namespace rename can use: `// FP Enhancement: 2026-05-23 — namespace rename and nullable annotations.`

Trivially-changed files (e.g. an exception class where only the namespace declaration changed) don't strictly need the banner. Use judgment.

- [ ] **Step 2: Verify no duplicate banners**

```powershell
Select-String -Path src\FixPortal.FixAtdl\**\*.cs -Pattern "^// FP Enhancement" | Group-Object Path | Where-Object Count -gt 1
```
Expected: no output.

- [ ] **Step 3: Build + test still green**

```powershell
dotnet test FixPortal.FixAtdl.sln
```

- [ ] **Step 4: Commit**

```powershell
git add src/FixPortal.FixAtdl/
```
```powershell
git commit -m "docs: add FP Enhancement provenance banners on modified files"
```

### Task E3: Rewrite README.md and add NOTICE

**Files:**
- Modify: `D:\Centerprise\work\FixPortal\FixAtdl\README.md`
- Create: `D:\Centerprise\work\FixPortal\FixAtdl\NOTICE`

- [ ] **Step 1: Rewrite README**

Replace the upstream README with a fork-focused one:

```markdown
# FixPortal.FixAtdl

> Modernised .NET 10 fork of [Atdl4net](https://github.com/atdl4net/atdl4net) — the open-source reference implementation of FIXatdl v1.1. Maintained by [FixPortal](https://www.fixportal.org).

## What this is

A headless library for parsing, validating, and emitting FIX-tag values from
FIXatdl v1.1 strategy XML documents. It targets .NET 10 and is consumed as a
NuGet package (`FixPortal.FixAtdl`).

## What it is *not*

- **Not a FIX engine.** It produces FIX tag values; sending them over the wire
  is the host application's responsibility. Pair with QuickFIX/n or similar.
- **Not a UI library.** The upstream Atdl4net's WPF rendering layer has been
  removed. Consumers wire their own UI (React, Blazor, WPF, anything) on top
  of the parsed model.

## Install

```
dotnet add package FixPortal.FixAtdl
```

## Quick start

```csharp
using FixPortal.FixAtdl.Xml;

var reader = new StrategiesReader();
var strategies = reader.Load(new StringReader(xml));

var twap = strategies["TWAP"];
twap.Parameters["StartTime"].SetValueFromString("20260101-09:30:00");

var fixTags = twap.GetFixTagValues();
foreach (var tag in fixTags)
    Console.WriteLine($"{tag.Tag}={tag.Value}");
```

## Differences from upstream Atdl4net

- Target framework: `net10.0` only (upstream: `net3.5`, `net4.0`).
- Namespace: `FixPortal.FixAtdl.*` (upstream: `Atdl4net.*`).
- WPF UI controls removed; library is now UI-agnostic.
- `Common.Logging` → `Microsoft.Extensions.Logging.Abstractions`.
- `System.Configuration` glue replaced with `FixAtdlOptions` POCO.
- Nullable reference types enabled throughout.
- New xUnit v3 test suite.

Files modified from upstream carry a `// FP Enhancement: <date> — <reason>`
banner.

## Licence

MIT, inherited from upstream. See `LICENSE`. Attribution preserved in `NOTICE`.

## Status

Pre-1.0; the public surface may evolve as the QFSIM ATDL Examiner exercises it.
Issues and PRs welcome at https://github.com/FixPortal/FixAtdl.
```

- [ ] **Step 2: Add NOTICE file**

```
FixPortal.FixAtdl

This product is a modernised fork of Atdl4net, originally created by
Steve Wilkinson (2010-2011) as the .NET reference implementation of the
FIXatdl v1.1 standard.

Upstream:        https://github.com/atdl4net/atdl4net
Upstream author: Steve Wilkinson
Upstream licence: MIT

Modifications and ongoing maintenance: FixPortal contributors (2026-).
Modifications licensed under the same MIT licence.

FIX Protocol and FIXatdl are trademarks or service marks of FIX Protocol Limited.
```

- [ ] **Step 3: Commit**

```powershell
git add README.md NOTICE
```
```powershell
git commit -m "docs: rewrite README for the fork; add NOTICE preserving upstream attribution"
```

---

## Phase F — CI/CD + Dependabot

### Task F1: Add GitHub Actions build-and-test workflow

**Files:**
- Create: `.github/workflows/build-and-test.yml`

- [ ] **Step 1: Create the directory and file**

```powershell
New-Item -ItemType Directory -Force -Path .github\workflows | Out-Null
```

- [ ] **Step 2: Write the workflow**

```yaml
name: build-and-test

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: ${{ github.ref != 'refs/heads/main' }}

permissions:
  contents: read

jobs:
  build-and-test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4

      - name: Set up .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore FixPortal.FixAtdl.sln

      - name: Build
        run: dotnet build FixPortal.FixAtdl.sln -c Release --no-restore

      - name: Test
        run: dotnet test FixPortal.FixAtdl.sln -c Release --no-build --logger "trx;LogFileName=test-results.trx" --collect:"XPlat Code Coverage"

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: test-results
          path: '**/*.trx'
```

- [ ] **Step 3: Commit**

```powershell
git add .github/workflows/build-and-test.yml
```
```powershell
git commit -m "ci: add GitHub Actions build-and-test workflow"
```

### Task F2: Add GitHub Actions release workflow

**Files:**
- Create: `.github/workflows/release.yml`

- [ ] **Step 1: Write the workflow**

```yaml
name: release

on:
  push:
    tags:
      - 'v*.*.*'

permissions:
  contents: write

jobs:
  pack-and-release:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4

      - name: Set up .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore FixPortal.FixAtdl.sln

      - name: Build (Release)
        run: dotnet build FixPortal.FixAtdl.sln -c Release --no-restore

      - name: Test (Release)
        run: dotnet test FixPortal.FixAtdl.sln -c Release --no-build

      - name: Pack
        run: dotnet pack src/FixPortal.FixAtdl/FixPortal.FixAtdl.csproj -c Release --no-build -o artifacts

      - name: Upload nupkg artefact
        uses: actions/upload-artifact@v4
        with:
          name: nupkg
          path: artifacts/*.nupkg

      - name: Create GitHub Release
        uses: softprops/action-gh-release@v2
        with:
          files: artifacts/*.nupkg
          generate_release_notes: true
```

- [ ] **Step 2: Commit**

```powershell
git add .github/workflows/release.yml
```
```powershell
git commit -m "ci: add release workflow (pack + GitHub Release on v*.*.* tag)"
```

### Task F3: Add Dependabot configuration

**Files:**
- Create: `.github/dependabot.yml`

- [ ] **Step 1: Write the config**

```yaml
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/"
    schedule:
      interval: "weekly"
      day: "monday"
      time: "09:00"
      timezone: "Europe/London"
    open-pull-requests-limit: 5
    commit-message:
      prefix: "chore(deps)"
      include: "scope"
    groups:
      microsoft-extensions:
        patterns:
          - "Microsoft.Extensions.*"
      xunit:
        patterns:
          - "xunit*"

  - package-ecosystem: "github-actions"
    directory: "/"
    schedule:
      interval: "weekly"
      day: "monday"
      time: "09:00"
      timezone: "Europe/London"
    open-pull-requests-limit: 3
    commit-message:
      prefix: "chore(actions)"
```

Notes:
- NuGet updates grouped to reduce PR noise (Microsoft.Extensions.* moves together, xunit.* moves together).
- GitHub Actions updates kept separate so action SHAs stay tight.
- 5 + 3 cap prevents Dependabot from swarming the inbox.

- [ ] **Step 2: Commit**

```powershell
git add .github/dependabot.yml
```
```powershell
git commit -m "ci: configure Dependabot for weekly nuget + github-actions updates"
```

### Task F4: Update .gitignore for SDK-style + Rider/VS artefacts

**Files:**
- Modify: `D:\Centerprise\work\FixPortal\FixAtdl\.gitignore`

- [ ] **Step 1: Inspect current contents**

```powershell
Get-Content .gitignore
```

- [ ] **Step 2: Ensure it covers SDK-style outputs**

If the upstream `.gitignore` is short or missing standard entries, replace with the canonical Visual Studio + .NET SDK gitignore. Easiest path:

```powershell
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/github/gitignore/main/VisualStudio.gitignore" -OutFile .gitignore
```

If invoking external URLs is unwanted, hand-edit to add at minimum:
```
bin/
obj/
*.user
.vs/
.idea/
artifacts/
TestResults/
*.trx
*.coverage
```

- [ ] **Step 3: Commit**

```powershell
git add .gitignore
```
```powershell
git commit -m "chore: update .gitignore for SDK-style and tooling artefacts"
```

### Task F5: Push to GitHub and verify first CI green

- [ ] **Step 1: Confirm a remote exists pointing at github.com/FixPortal/FixAtdl**

```powershell
git remote -v
```
Expected: `origin  https://github.com/FixPortal/FixAtdl ...`. If not, the human needs to create the GitHub repo under the `FixPortal` org and add it as origin (this step is a human prerequisite — the agent cannot create org-level GitHub repos).

- [ ] **Step 2: Push current main branch**

```powershell
git push -u origin main
```

- [ ] **Step 3: Watch the build**

```powershell
gh run watch
```
Expected: build-and-test workflow runs and goes green.

- [ ] **Step 4: Marker commit**

```powershell
git commit --allow-empty -m "milestone: Phase F complete — CI green on main, Dependabot armed"
```
```powershell
git push
```

### Task F6: Configure branch protection on main

This step uses the `gh` CLI to apply branch protection. The agent can execute it once the repo exists on GitHub.

- [ ] **Step 1: Apply protection ruleset**

```powershell
gh api -X PUT repos/FixPortal/FixAtdl/branches/main/protection -F required_status_checks.strict=true -F required_status_checks.contexts[]="build-and-test" -F enforce_admins=false -F required_pull_request_reviews.required_approving_review_count=1 -F required_pull_request_reviews.dismiss_stale_reviews=true -F restrictions= -F required_linear_history=true -F allow_force_pushes=false -F allow_deletions=false
```

If `gh api` syntax for multi-field PUTs is fiddly, the simpler equivalent is to author a JSON file `branch-protection.json` and `gh api -X PUT --input branch-protection.json repos/FixPortal/FixAtdl/branches/main/protection`. Either works.

- [ ] **Step 2: Verify**

```powershell
gh api repos/FixPortal/FixAtdl/branches/main/protection
```
Expected: JSON showing required status check `build-and-test`, 1 review required, linear history, no force-push, no deletion.

- [ ] **Step 3: No commit (config is server-side); proceed to Phase G**

---

## Phase G — Pack and release v0.1.0

### Task G1: Local pack dry-run

**Files:**
- (none modified)

- [ ] **Step 1: Build Release then pack**

```powershell
dotnet build FixPortal.FixAtdl.sln -c Release
```
```powershell
dotnet pack src/FixPortal.FixAtdl/FixPortal.FixAtdl.csproj -c Release --no-build -o artifacts
```

- [ ] **Step 2: Inspect the nupkg**

```powershell
Get-ChildItem artifacts/*.nupkg
```
Expected: `FixPortal.FixAtdl.0.1.0.nupkg` (and `.snupkg` if symbols enabled).

- [ ] **Step 3: Sanity-check contents**

```powershell
Expand-Archive -LiteralPath artifacts/FixPortal.FixAtdl.0.1.0.nupkg -DestinationPath artifacts/inspect -Force
```
```powershell
Get-ChildItem artifacts/inspect -Recurse | Select-Object FullName
```
Expected: `lib/net10.0/FixPortal.FixAtdl.dll`, `lib/net10.0/FixPortal.FixAtdl.xml`, MIT licence in metadata, README in metadata, NOTICE not necessarily (NuGet doesn't standardise it, but acceptable).

- [ ] **Step 4: Clean up the inspection directory**

```powershell
Remove-Item -Recurse -Force artifacts/inspect
```

### Task G2: Tag v0.1.0 and push

- [ ] **Step 1: Annotated tag**

```powershell
git tag -a v0.1.0 -m "v0.1.0 — first usable FixPortal.FixAtdl release (modernised Atdl4net fork)"
```

- [ ] **Step 2: Push the tag**

```powershell
git push origin v0.1.0
```

- [ ] **Step 3: Confirm release workflow ran**

```powershell
gh run watch
```
Expected: release workflow goes green; a GitHub Release for `v0.1.0` is created with the `.nupkg` attached.

```powershell
gh release view v0.1.0
```
Expected: release exists, has the nupkg artefact.

### Task G3: Drop the .nupkg into QFSIM's LocalPackages/

This is the bridge to Phase 1.

- [ ] **Step 1: Download the released nupkg**

```powershell
gh release download v0.1.0 -R FixPortal/FixAtdl -p "*.nupkg" -D D:\Centerprise\work\QFSIM\LocalPackages\
```

- [ ] **Step 2: Update QFSIM's nuget.config packageSourceMapping**

In `D:\Centerprise\work\QFSIM\nuget.config`, ensure `FixPortal.FixAtdl.*` is mapped to the `Local` feed (mirrors how `Centerprise.QuickFIXn` is wired). The exact mechanics vary with QFSIM's existing config — this is a one-line addition under the appropriate `<packageSource key="Local">`.

This task is recorded here for completeness, but the actual edit is the first task of the Phase 1 plan (`docs/superpowers/plans/2026-05-23-atdl-examiner-phase1.md` over in the QFSIM repo). Leave it for Phase 1's executor.

- [ ] **Step 3: Marker commit (no file change, just a milestone)**

```powershell
git commit --allow-empty -m "milestone: Phase G complete — v0.1.0 released and available for QFSIM Phase 1"
```
```powershell
git push
```

---

## Acceptance criteria

When this plan is done:

- [ ] `FixPortal.FixAtdl.sln` builds clean on net10.0 with warnings-as-errors and strict nullable.
- [ ] `dotnet test FixPortal.FixAtdl.sln` is green; tests cover parser, model, validation, FIX-tag emission, and Edit/StateRule evaluation.
- [ ] No `Common.Logging`, `System.Configuration`, `System.ComponentModel.Composition`, WPF, or Notification references remain.
- [ ] Top-level namespace is `FixPortal.FixAtdl.*`; no `Atdl4net.*` identifiers outside `.resx` resource keys and licence/notice text.
- [ ] Every modified file carries a `// FP Enhancement: <date> — <reason>` banner (per spec §4.2).
- [ ] GitHub Actions `build-and-test` workflow runs green on push/PR; `release` workflow runs green on tag.
- [ ] Dependabot is configured for weekly NuGet + actions updates.
- [ ] `main` branch is protected (PR-only, 1 review, CI must pass, linear history).
- [ ] `v0.1.0` GitHub Release exists with `FixPortal.FixAtdl.0.1.0.nupkg` attached.
- [ ] README, NOTICE, LICENCE all present and accurate.

---

## Open items punted to follow-up issues (not in v0.1.0 scope)

- **GitHub Packages publishing** — spec §4.3 explicitly defers this. Out-of-scope; revisit at v0.2 if cross-product adoption materialises.
- **Symbols package (.snupkg) publishing to NuGet.org or a SymServer** — same deferral; the `.nupkg` carries embedded PDBs via `<DebugType>embedded</DebugType>` if needed later.
- **FIXatdl v1.2 schema support** — explicit non-goal per spec §2.
- **Multi-tier StrategyProvider replacement** — Providers/ deleted; if a consumer ever needs a strategy-registry mechanism, build it on top of `FixPortal.FixAtdl` rather than inside it.
- **InternalsVisibleTo for the test project** — currently the tests exercise the public surface only, which is the right discipline. If any internal needs targeted testing later, add the attribute then.
