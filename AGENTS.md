# AGENTS.md — FixPortal.FixAtdl

Repo-specific conventions for agent work in this repository. Global FixPortal
rules apply on top of these; this file only carries what is true here and
nowhere else.

## What this repo is

A modernised fork of [atdl4net/atdl4net](https://github.com/atdl4net/atdl4net),
published as the `FixPortal.FixAtdl` NuGet package. Headless FIXatdl v1.1
parser / model / validator / FIX-tag emitter; `net10.0` only; no UI layer
(upstream's WPF controls were removed on purpose — do not reintroduce one).

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
- `README.md` is packed into the nupkg (`PackageReadmeFile` in
  `src/FixPortal.FixAtdl/FixPortal.FixAtdl.csproj`). Keep it NuGet-gallery
  compatible: no YAML frontmatter, no GFM alert (`> [!NOTE]`) blocks — the
  NuGet renderer does not support them. Links out of `README.md` must be
  absolute `https://github.com/FixPortal/fixportal-fixatdl/blob/main/...` URLs:
  a relative path resolves on GitHub but breaks on the NuGet gallery.
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
  `tests/FixPortal.FixAtdl.Tests`, running on Microsoft.Testing.Platform.
- Assert with `.Should()`, never xUnit `Assert.*`.
- CI collects coverage with `dotnet-coverage` and enforces a 70% line floor on
  `FixPortal.FixAtdl` via `scripts/assert-coverage-floor.ps1`. The floor is
  defined in `ci.yml`, not in `docs/coverage-baseline.md` — that document is the
  historical 2026-05-30 starting baseline (32% line) and is not current state.

## Private feed restore

`FixPortal.CodeStyle` comes from the private `github-fixportal` feed
(`nuget.config`). A restore without `GITHUB_PACKAGES_TOKEN` set fails with
NU1301/401 — see the README Troubleshooting section before concluding the
build is broken.

## Review workflow

PRs merge rebase-only. `.claude/review-policy.json` tiers risk:
`nuget.config`, `Directory.Build.props`, and the review control plane itself
(`ci.yml`, `review-policy-guard.yml`, its two assertion scripts under
`.github/scripts/`, the two coverage-floor scripts under `scripts/`,
`review-policy.json`, `.coderabbit.yaml`) are HIGH; other workflows are NORMAL
— workflow hygiene is asserted mechanically by `review-policy-guard.yml`
instead. LOW documentation is limited to `docs/architecture/**`,
`docs/superpowers/**`, `docs/batch-3-findings-disposition.md`,
`docs/batch-5-conformance-review.md`, and `docs/coverage-baseline.md`; unmatched
docs, including `docs/ai-findings.md` and new root-level files, are NORMAL.
Root markdown includes the README shipped in the package.
