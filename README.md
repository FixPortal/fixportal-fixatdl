# FixPortal.FixAtdl

![Release](https://img.shields.io/github/v/release/FixPortal/fixportal-fixatdl)
![CI](https://github.com/FixPortal/fixportal-fixatdl/actions/workflows/ci.yml/badge.svg)
![License](https://img.shields.io/github/license/FixPortal/fixportal-fixatdl)

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

## Read these first

- [Usage guide](https://github.com/FixPortal/fixportal-fixatdl/blob/main/docs/usage.md) — loading, setting values, validation, FIX output, exceptions.
- [Architecture overview](https://github.com/FixPortal/fixportal-fixatdl/blob/main/docs/architecture/README.md) — the pipeline, layers, and load-bearing components.
- [Conformance record](https://github.com/FixPortal/fixportal-fixatdl/blob/main/docs/conformance.md) — the assessed FIXatdl 1.1 surface and its limits.
- [Changelog](https://github.com/FixPortal/fixportal-fixatdl/blob/main/CHANGELOG.md) — what changed in each release.

## Install

```
dotnet add package FixPortal.FixAtdl
```

## Ecosystem

- [NuGet package](https://www.nuget.org/packages/FixPortal.FixAtdl/) and [source repository](https://github.com/FixPortal/fixportal-fixatdl)
- [WPF adapter](https://github.com/FixPortal/fixportal-fixatdl-wpf) for editable desktop strategy forms
- [React adapter](https://github.com/FixPortal/fixportal-fixatdl-react) for browser-side strategy forms
- [Conformance record](https://github.com/FixPortal/fixportal-fixatdl/blob/main/docs/conformance.md) for assessed scope and limits

## Quick start

```csharp
using FixPortal.FixAtdl.Xml;

var reader = new StrategiesReader();
using var stream = File.OpenRead("twap.xml");
var strategies = reader.Load(stream);

var twap = strategies.Strategies[0];
twap.Parameters["StartTime"].WireValue = "20260101-09:30:00";

foreach (var tag in twap.Parameters.GetOutputValues())
    Console.WriteLine($"{tag.Key}={tag.Value}");
```

## Differences from upstream Atdl4net

- Target framework: `net10.0` only (upstream: `net3.5`, `net4.0`).
- Namespace: `FixPortal.FixAtdl.*` (upstream: `Atdl4net.*`).
- WPF UI controls removed; library is now UI-agnostic.
- `Common.Logging` → `Microsoft.Extensions.Logging.Abstractions`.
- `System.Configuration` glue replaced with `FixAtdlOptions` POCO.
- Nullable reference types enabled throughout.
- New xUnit v3 test suite (AwesomeAssertions + NSubstitute).

## Development

Open `FixPortal.FixAtdl.slnx` in Visual Studio, then build the solution and run
the tests from Test Explorer. The command-line checks below are the ones CI
runs, in CI's order — run them all before pushing.

```powershell
dotnet tool restore
```

```powershell
dotnet csharpier check .
```

```powershell
dotnet restore FixPortal.FixAtdl.slnx
```

```powershell
dotnet build FixPortal.FixAtdl.slnx --configuration Release --no-restore
```

```powershell
dotnet test --solution FixPortal.FixAtdl.slnx --configuration Release --no-build
```

CSharpier is a pinned local tool. CI runs the read-only `check`; fix formatting
locally with `dotnet csharpier format .` and commit the result — never as the
gate itself, which would turn a failing check into a silent pass.

To produce a package locally:

```powershell
dotnet pack FixPortal.FixAtdl.slnx --configuration Release --no-build --output ./_pkgout
```

Files modified from upstream carry a `// FP Enhancement: <date> — <reason>` banner.

## Benchmarks

The repository-owned BenchmarkDotNet workloads cover representative FIXatdl XML parsing and FIX message parse/emit paths. Run them manually in Release mode; CI compiles the project but does not use workstation timings as a performance gate.

```powershell
dotnet run --project benchmarks/FixPortal.FixAtdl.Benchmarks/FixPortal.FixAtdl.Benchmarks.csproj --configuration Release -- --filter '*'
```

## Mutation testing

This repo uses Stryker.NET for mutation testing. `stryker-config.json` sets
`mutate: ["**/*.cs"]`, so the whole library is in scope.

Run it locally from the test project directory — the working directory is
load-bearing, because `stryker-config.json` names no test project:

```powershell
dotnet tool restore
cd tests/FixPortal.FixAtdl.Tests
dotnet stryker --config-file ../../stryker-config.json
```

Output lands under `tests/FixPortal.FixAtdl.Tests/StrykerOutput/<timestamp>/reports/`.
CI summarizes the run with `scripts/summarize-stryker.ps1` and uploads both the
full Stryker output and compact summaries:

- `mutation-summary.json`
- `mutation-summary.md`

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `dotnet restore` fails with NU1301 ("Unable to find package FixPortal.CodeStyle") or 401 Unauthorized on the `github-fixportal` feed | The build consumes `FixPortal.CodeStyle` from the private FixPortal GitHub Packages feed, and `nuget.config` reads its credentials from the `GITHUB_PACKAGES_TOKEN` environment variable. Without it, the feed rejects the restore anonymously. | For a local restore, set `$env:GITHUB_PACKAGES_TOKEN = "<token>"` (Windows PowerShell) or `export GITHUB_PACKAGES_TOKEN=...` (bash), then restore again. Locally the token must be a **personal access token (classic)** with the `read:packages` scope on the FixPortal org — the GitHub Packages NuGet registry does not accept fine-grained tokens, which fail with the same 401. In GitHub Actions no PAT is needed: the workflows supply `GITHUB_PACKAGES_TOKEN` from `secrets.GITHUB_TOKEN`. |

## Licence

MIT, inherited from upstream. See `LICENSE`. Attribution preserved in `NOTICE`.

## Status

The latest NuGet.org release is 1.1.2. The public surface is governed by
[semantic versioning](https://semver.org/spec/v2.0.0.html) and recorded in the
[changelog](CHANGELOG.md); there is no analyzer enforcing it at build time
(`PublicApiAnalyzers` was removed before 1.0.5).
[Issues and PRs](https://github.com/FixPortal/fixportal-fixatdl) are welcome.
