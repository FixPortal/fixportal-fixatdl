# Contributing

Issues and pull requests are welcome. This is a maintained fork of
[atdl4net/atdl4net](https://github.com/atdl4net/atdl4net) — changes that
realign behaviour with upstream, fix parser/validator bugs, or improve the
modernised .NET 10 surface are all in scope. Reintroducing a UI layer is not.

## Getting set up

The build consumes `FixPortal.CodeStyle` from the private FixPortal GitHub
Packages feed, so restore needs a token with `read:packages` on the `FixPortal`
org exported first:

```powershell
$env:GITHUB_PACKAGES_TOKEN = "<token with read:packages>"
```

Then the standard loop:

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

CI runs `dotnet csharpier check .`, which validates formatting without
rewriting files — run `dotnet csharpier format .` before pushing.

## Conventions

- **Fork banner.** Files modified from upstream carry a
  `// FP Enhancement: <date> — <reason>` banner; keep it when editing such a
  file and add one when you first diverge a file from upstream.
- **Public API.** The package's public surface is a compatibility contract;
  call out any breaking signature or behaviour change explicitly in the PR.
- **Tests.** xUnit v3, AwesomeAssertions (`.Should()`), NSubstitute. Parser
  changes need tests covering the edge case they address.
- **Licence.** MIT, inherited from upstream. `NOTICE` preserves attribution —
  do not remove or relicense.

## Pull requests

- Branch from `main`, open a PR, and let CI (build, test, CSharpier,
  actionlint) go green before asking for review.
- PRs are merged rebase-only; keep commits clean and individually meaningful.
