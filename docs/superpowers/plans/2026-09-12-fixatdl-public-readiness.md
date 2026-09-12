# FIXatdl Public Readiness Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prepare the published FIXatdl packages and evidenced integrations for a future public announcement.

**Architecture:** Package repositories own their adoption documentation and unlinked launch drafts that are publicly visible in source but not approved for publication. Simulator and EMS evidence remains owned by the applications that integrate them; only code-proven integration paths are described. GitHub repository metadata mirrors the final README positioning.

**Tech Stack:** Markdown, NuGet.org, npm, GitHub repository metadata, .NET 10, React 19.

**Spec:** `docs/superpowers/specs/2026-09-12-fixatdl-public-readiness-design.md`

## Global Constraints

- Do not alter public APIs, package versions, licences or attribution.
- Do not add dependencies or a marketing site.
- Claims must be traceable to implementation, tests or an existing release.
- Announcements remain drafts pending Chris's explicit publication decision.
- Use isolated worktrees; push once per repository only after its normal gate passes.

---

### Task 1: Core package public surface

**Files:**
- Modify: `README.md`
- Create: `docs/launch/pre-announcement-draft.md`
- Create: `docs/superpowers/specs/2026-09-12-fixatdl-public-readiness-design.md`
- Create: `docs/superpowers/plans/2026-09-12-fixatdl-public-readiness.md`

**Interfaces:**
- Consumes: the published `FixPortal.FixAtdl` 1.1.2 package, `NOTICE`, and `docs/conformance.md`.
- Produces: a NuGet-gallery-compatible README with verified package and ecosystem links.

- [ ] Inspect package metadata, `NOTICE`, conformance record and current public links.
- [ ] Replace stale `1.0.1` status language with the current release status; retain the upstream-fork statement and MIT attribution.
- [ ] Add concise links to the WPF and React adapters, NuGet.org, the source repository and conformance record.
- [ ] Create an unlinked launch draft, publicly visible in source but not approved for publication, containing release copy, a LinkedIn draft and an article outline.
- [ ] Run `dotnet csharpier check .`, restore/build/test/pack commands from `README.md`, and verify every Markdown URL.
- [ ] Commit with `docs: prepare core package for public discovery`.

### Task 2: WPF and React package public surfaces

**Files:**
- Modify: `D:\fix-portal\fixportal-fixatdl-wpf\README.md`
- Create: `D:\fix-portal\fixportal-fixatdl-wpf\docs\launch\pre-announcement-draft.md`
- Modify: `D:\fix-portal\fixportal-fixatdl-react\README.md`
- Create: `D:\fix-portal\fixportal-fixatdl-react\docs\launch\pre-announcement-draft.md`

**Interfaces:**
- Consumes: published `FixPortal.FixAtdl.Wpf` 1.0.2, `FixPortal.FixAtdl.Wpf.Core` 1.0.2 and `@fix-portal/fixatdl-react` 0.2.0.
- Produces: installation instructions that use public registries and correctly link the headless core and sibling adapter.

- [ ] Inspect both package manifests and published registry metadata before editing install and release text.
- [ ] Update WPF installation wording from GitHub Packages to NuGet.org and link its core package and headless dependency.
- [ ] Add reciprocal core/WPF/React links, clear host-boundary wording and preserved attribution to both READMEs.
- [ ] Add unlinked launch drafts, publicly visible in source but not approved for publication, with accurate package-specific copy.
- [ ] Run the WPF Release build/test gate and React `typecheck`, `lint`, `test:coverage`, `build` and `npm pack --dry-run`.
- [ ] Commit each repository independently with `docs: prepare package for public discovery`.

### Task 3: Simulator integration evidence

**Files:**
- Modify only if evidence warrants it: `D:\fix-portal\fixportal-simulator-backend\README.md`
- Modify only if evidence warrants it: `D:\fix-portal\fixportal-simulator-frontend\README.md`

**Interfaces:**
- Consumes: simulator backend FIXatdl parser-to-DTO mapping and frontend published-package consumption.
- Produces: reproducible, source-backed integration documentation; no unverified product claims.

- [ ] Trace each simulator integration from package reference through its visible entry point and existing tests.
- [ ] Record only a developer-facing run or verification path supported by that trace; make no edit if the existing README already supplies it or no safe public path exists.
- [ ] Run each touched repository's existing documentation/build gate.
- [ ] Commit documentation-only changes separately in the owning repository.

### Task 4: GitHub discovery metadata and review

**Files:**
- External metadata: `FixPortal/fixportal-fixatdl`, `FixPortal/fixportal-fixatdl-wpf`, `FixPortal/fixportal-fixatdl-react`

**Interfaces:**
- Consumes: merged README language and verified public package URLs.
- Produces: concise descriptions, homepage links and evidence-based topics.

- [ ] Read current repository metadata through GitHub API and compare it to the merged READMEs.
- [ ] Set descriptions, homepage URLs and topics only after the relevant README PR is green and approved.
- [ ] Open one PR per changed repository, request Gitar according to each policy, wait for required CI, and rebase-merge only when green.
- [ ] Pull each primary checkout forward with `git pull --ff-only` after merge.
- [ ] Recheck public package pages, repository metadata and README rendering before reporting readiness.

## Self-review

Spec coverage: Tasks 1 and 2 cover package positioning and drafts; Task 3 covers integration evidence; Task 4 covers discoverability, review and synchronization.

Placeholder scan: no deferred implementation placeholders are present; conditional documentation edits are intentionally constrained by source evidence.

Type consistency: this plan changes no runtime interfaces.
