# AGENTS.md — FixPortal.FixAtdl

Repo-specific conventions for agent work in this repository. Global FixPortal
rules apply on top of these; this file only carries what is true here and
nowhere else.

## What this repo is

A modernised fork of [atdl4net/atdl4net](https://github.com/atdl4net/atdl4net).
The repo publishes two NuGet packages, released in lockstep from one tag:
`FixPortal.FixAtdl` (headless FIXatdl v1.1 parser / model / validator / FIX-tag
emitter) and `FixPortal.FixAtdl.Contracts` (the JSON contract consumed by
`@fix-portal/fixatdl-react`). `net10.0` only; no UI layer (upstream's WPF
controls were removed on purpose — do not reintroduce one). The Contracts
goldens under `tests/FixPortal.FixAtdl.Contracts.Tests/Golden/` are never
regenerated. `contracts/state-rule-cases.json` is the canonical corpus that
`@fix-portal/fixatdl-react` copies.

## Fork discipline

- Files modified from upstream carry a `// FP Enhancement: <date> — <reason>`
  banner. Keep the banner when editing such a file, and add one when you are
  the first to diverge a file from upstream.
- The licence is **MIT, inherited from upstream** — a fork cannot unilaterally
  relicense, so do not "upgrade" `LICENSE` to Apache-2.0. `NOTICE` preserves
  upstream attribution; keep it intact.

## Published-package surface

- The public API is a compatibility surface. Breaking signature or behaviour
  changes are a release decision, not a drive-by.
- Both packed READMEs must stay NuGet-gallery compatible: the root
  `README.md` (`PackageReadmeFile` in
  `src/FixPortal.FixAtdl/FixPortal.FixAtdl.csproj`) and
  `src/FixPortal.FixAtdl.Contracts/README.md` (packed by the Contracts
  project). No YAML frontmatter and no GFM alert (`> [!NOTE]`) blocks — the
  NuGet renderer does not support them. A link that points at a file in this
  repository must be an absolute
  `https://github.com/FixPortal/fixportal-fixatdl/blob/main/...` URL: a
  relative path resolves on GitHub but breaks on the NuGet gallery. Links to
  other sites, including nuget.org and the React repository, stay ordinary
  absolute URLs.
- Every consumer-visible change (public API, behaviour, packaging) gets a
  `CHANGELOG.md` entry under `## [Unreleased]` in the same PR. CI, test, and
  internal-refactor commits do not.
- Parser edge cases (malformed, unusual, or hostile strategy XML) are
  security-relevant. Add tests for any parser change.

## Build, format, test

- CSharpier is pinned in `.config/dotnet-tools.json`; restore with
  `dotnet tool restore`, check with `dotnet csharpier check .` (CI runs the
  read-only check; format locally).
- Tests are xUnit v3 + AwesomeAssertions + NSubstitute in
  `tests/FixPortal.FixAtdl.Tests` and `tests/FixPortal.FixAtdl.Contracts.Tests`,
  running on Microsoft.Testing.Platform.
- Assert with `.Should()`, never xUnit `Assert.*`.
- CI collects coverage with `dotnet-coverage` and enforces a 70% line floor on
  `FixPortal.FixAtdl` and on `FixPortal.FixAtdl.Contracts` via
  `scripts/assert-coverage-floor.ps1`. The floor is
  defined in `ci.yml`, not in `docs/coverage-baseline.md` — that document is the
  historical 2026-05-30 starting baseline (32% line) and is not current state.

## Restore

Every package, including `FixPortal.CodeStyle`, restores from nuget.org with no
credentials. Do not reintroduce a private feed: it locks outside contributors
out of the build.

## Review workflow

PRs merge rebase-only. Risk tiers come from the committed
`.claude/review-policy.json`. Do not copy its path lists into this file: a
copied list drifts from the policy the guard actually enforces. Root
markdown, including the README shipped in the package, is not in the low
globs. `docs/ai-findings.md` is gitignored and is not a tracked ledger in
this repository; the policy still leaves that path off the low globs, so a
force-add stays NORMAL.
