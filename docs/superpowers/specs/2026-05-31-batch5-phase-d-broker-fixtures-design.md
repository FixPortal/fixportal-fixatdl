# Batch 5 Phase D — Obfuscated Broker Conformance Fixtures: Design

**Date:** 2026-05-31
**Branch:** `reviewer-findings-batch8`
**Status:** Approved design — pending spec review, then implementation plan.

## Goal

Add two real-world-derived FIXatdl 1.1 documents to the test project as **conformance
fixtures**, plus tests that (a) prove the parser/model survive real-world-shaped input and
(b) pin the semantic findings already fixed in Phases A–C against that realistic input.

The source documents are **real client data** and this repository is a **public** fork.
Therefore the central constraint of this phase is data protection: only fully-obfuscated
content may be committed, and the obfuscation mapping must never enter the repository.

## Background

The Phase A–C findings (C1, C2, H1–H4, M1–M4) were derived by auditing the library against
two distinct, live production broker ATDL documents held in a local scratch directory outside
the repository. For this design they are referred to neutrally:

- **The timezone spec** — exercises `Clock_t` with `localMktTz` (an IANA market zone) feeding
  `UTCTimestamp_t`, a time-only `maxValue`, and `SecurityTypes`. This is the document that
  drove the critical timezone findings (C1/C2).
- **The multi-region spec** — exercises `Regions`/`Region@inclusion`, rich `Char_t` enumerations,
  deeply nested `StrategyPanel`s, `mutableOnCxlRpl`, `EnumPair@index`, `constValue`, and the
  `{NULL}` sentinel wire value.

A third source document is byte-identical to the multi-region spec and is therefore not added
separately.

## Constraints

1. **Public repository.** Every committed artifact (fixtures, tests, this spec, the plan, and
   the existing findings doc) is world-readable and must contain nothing that identifies the
   client firm or its products.
2. **No mapping in the repo.** The original→replacement rename map and the leak-scan denylist
   exist only in an uncommitted scratch script. Committing them would re-introduce the very
   identifiers being removed.
3. **No new library behavior.** All findings are already resolved in Phases A–C; this phase adds
   fixtures and tests only.

## Obfuscation rules

A single deterministic transform (a PowerShell script in the scratch directory, **not
committed**) reads the two source documents, applies an explicit hand-built
denylist→replacement map, and writes the scrubbed XML directly into the repository's fixtures
directory.

| Token class | Action |
|---|---|
| `providerID` | Replaced with a single neutral value (`DEMO`). |
| Strategy `name`/`uiRep`/`wireValue` | **Industry-standard generic algo-type names retained as-is** (e.g. VWAP, TWAP, POV, IS, SOR, PEG, CLOSE, TARGET_OPEN/TARGET_CLOSE). **Broker-specific brand/product names replaced** with generic functional descriptors (e.g. a dark-liquidity brand → `DARK`). |
| Brand/product enum `wireValue`s and parameter names that are marketing terms | Replaced with neutral generic equivalents. |
| All XML comments | Stripped (release notes carry dates, regions, and version history; banner art may encode identity). |
| `fixTag` / `strategyIdentifierTag` values | **Kept as-is** — tag numbers alone are not identifying once names and comments are removed, and keeping them maximises real-world fidelity. |
| Namespaces, `xsi:type`s, all other attributes, panel nesting, `localMktTz` (IANA zone), `SecurityType`/`Region` names, structural sentinels (`""`, `{NULL}`, numerics, single chars `Y`/`N`/`B`) | **Kept** — these carry the conformance value and are not identifying. |

The replacement map is built by scanning both documents for every firm-, product-, and
identity-bearing token. When a token's status is ambiguous, it is replaced (err toward
over-scrubbing). Generic FIX/trading vocabulary is retained.

## Committed artifacts

Two scrubbed fixtures under a new subfolder (the existing `Fixtures\**\*.xml` MSBuild glob
already copies subfolders to the test output):

- `tests/FixPortal.FixAtdl.Tests/Fixtures/RealWorld/tz-clock.xml` — from the timezone spec.
- `tests/FixPortal.FixAtdl.Tests/Fixtures/RealWorld/regions-enums.xml` — from the multi-region spec.

Each fixture begins with a neutral provenance comment (naming no firm, product, or id):

```xml
<!-- Synthetic FIXatdl 1.1 conformance fixture. Structurally derived from a real-world
     production broker spec, with all firm-, product-, and identity-bearing content removed
     (provider, brand/product names, comments). Retained solely to exercise the parser and
     model against real-world construct combinations. -->
```

## Conformance tests

New test class `Tests/Conformance/RealWorldSpecConformanceTests.cs` (xUnit v3 +
AwesomeAssertions, matching the existing test conventions). The library is **load-only** (no
XML serializer), so "round-trip" means parse-survives plus FIX-wire round-trip, not XML
re-serialization.

**Parse-survives / structural invariants (both fixtures):**
- Loads without throwing.
- Expected strategy count and per-strategy parameter counts.
- Presence of the high-value constructs: `Regions`, nested `StrategyPanel` depth, `Char_t`
  enumerations, captured `EnumPair@index` values, and the `{NULL}` sentinel wire value
  (multi-region fixture); `SecurityTypes` and the `Clock_t`/`localMktTz` control feeding a
  `UTCTimestamp_t` parameter (timezone fixture).

**Targeted findings on real-shaped input (the regression value):**
- **C1** — the `Clock_t` control bearing `localMktTz` (an IANA zone) feeding a `UTCTimestamp_t`
  parameter emits a correctly zone-shifted UTC value on the FIX wire (assertion style mirrors
  the existing `ClockTimeZoneTests`).
- **C2** — a time-only `maxValue` on a `UTCTimestamp_t` parameter compares on the time-of-day
  component only (no injected-date contamination).
- **H3** — `EnumPair@index` is captured as `int? Index` on the `Char_t` enumerations.
- **H1 / M2 / M3** — presence/parse checks for the relevant shapes where the fixtures contain
  them (e.g. list controls, two-field edits), pinning the resolved behavior on realistic input.

Exact API entry points and expected values are determined during planning by reading the
relevant model/control/parameter members and the existing analogous tests.

## Leak guard

The scrub script ends with a **denylist scan** over its own committed output: it greps the
generated fixtures for every original identifier (firm name, brand/product names, original
`providerID` values) and **fails the run if any survives**. The denylist lives only in the
scratch script. Before any commit, the controller verifies a zero-leak result; the spec-review
subagent independently inspects the committed fixtures for any residual identifying content.

## Companion cleanup (in-scope)

`docs/batch-5-conformance-review.md` currently contains an identity-bearing reference to the
source firm and its products. Because the repository is public, this reference is scrubbed as
part of this phase (replaced with a neutral structural description), so the fixture obfuscation
is not undermined by the existing committed doc. A scan confirms no other committed file
carries identifying content.

## Workflow

- Runs in the ephemeral review worktree at the canonical path on `reviewer-findings-batch8`
  (branched from `origin/main`).
- The obfuscation transform, the leak-scan, and committing the scrubbed fixtures are performed
  by the controller — the real source files are never read by a subagent, and the mapping
  script is never committed.
- Tests are written via subagent-driven TDD against the already-committed, already-safe
  fixtures; subagents never touch the real source files.
- Finalize with a holistic review, then a rebase-merge PR
  (`--repo FixPortal/fixportal-fixatdl --base main`), then worktree + branch teardown.

## Out of scope

- No new library behavior (all findings resolved in Phases A–C).
- No XML serializer.
- No third source document (it is byte-identical to the multi-region spec).
- The deferred adversarial-review (Phase 3) sweep remains parked.
