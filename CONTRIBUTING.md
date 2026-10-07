# Contributing

Issues and pull requests are welcome. This is a maintained fork of
[atdl4net/atdl4net](https://github.com/atdl4net/atdl4net) — changes that
realign behaviour with upstream, fix parser/validator bugs, or improve the
modernised .NET 10 surface are all in scope. Reintroducing a UI layer is not.

## Getting set up

Every dependency restores from nuget.org; no credentials are needed. The
standard loop:

```powershell
dotnet tool restore
```

```powershell
dotnet csharpier format .
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

```powershell
dotnet dotnet-coverage collect -f cobertura -o coverage.cobertura.xml "dotnet test --solution FixPortal.FixAtdl.slnx --configuration Release --no-build"
```

```powershell
./scripts/assert-coverage-floor.ps1 -ReportPath coverage.cobertura.xml -Package FixPortal.FixAtdl -MinimumLineRate 70
```

```powershell
./scripts/assert-coverage-floor.ps1 -ReportPath coverage.cobertura.xml -Package FixPortal.FixAtdl.Contracts -MinimumLineRate 82
```

CI runs `dotnet csharpier check .`, which validates formatting without
rewriting files — run `dotnet csharpier format .` before pushing. It also
collects coverage and enforces a 70% line floor on `FixPortal.FixAtdl` and an
82% line floor on `FixPortal.FixAtdl.Contracts`. The floor script needs
PowerShell 7.

## Conventions

- **Fork banner.** Files modified from upstream carry a
  `// FP Enhancement: <date> — <reason>` banner; keep it when editing such a
  file and add one when you first diverge a file from upstream.
- **Public API.** The package's public surface is a compatibility contract;
  call out any breaking signature or behaviour change explicitly in the PR.
- **Tests.** xUnit v3, AwesomeAssertions (`.Should()`), NSubstitute. Parser
  changes need tests covering the edge case they address.
- **Contracts goldens.** `tests/FixPortal.FixAtdl.Contracts.Tests/Golden/` pins
  the JSON `@fix-portal/fixatdl-react` consumes. If a change makes a golden
  test fail, the change alters that contract: fix the code, or raise it as a
  breaking change in the PR. Do not regenerate the golden to make it pass.
- **Licence.** MIT, inherited from upstream. `NOTICE` preserves attribution —
  do not remove or relicense.

## Pull requests

- Branch from `main`, open a PR, and let CI go green before asking for
  review. The required checks are `CI Gate` (build, test, coverage floor,
  CSharpier, actionlint) and `Review policy intact`.
- Some files under `.github/` are shared CI assets synced from FixPortal's
  internal tooling (listed in `.github/canonical-assets.json`); CI rejects
  local edits to them. If one needs changing, say so in an issue or PR
  description and a maintainer will make the change upstream.
- PRs are merged rebase-only; keep commits clean and individually meaningful.
