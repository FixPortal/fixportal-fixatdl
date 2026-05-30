# Coverage Baseline

> Measured 2026-05-30 at commit `0006205`. Input artefact for the Phase 1 test-hardening plan.

## Overall
- Line coverage: 32%
- Branch coverage: 19%

Raw counts: 1,627 covered lines / 5,072 coverable lines; 294 covered branches / 1,546 total branches.
Method coverage: 30.2% (278 / 919 methods).

## Core namespaces (Phase 1 priority — lowest first)

Aggregated from the per-class JsonSummary report. Line % = covered lines ÷ coverable lines; Branch % = covered branches ÷ total branches (null branch counts are treated as 0/0 and excluded from the denominator).

| Namespace | Coverable lines | Covered lines | Line % | Covered branches | Total branches | Branch % |
|-----------|----------------:|--------------:|-------:|-----------------:|---------------:|---------:|
| `FixPortal.FixAtdl.Model.Reference.*` | 263 | 0 | 0% | 0 | 6 | 0% |
| `FixPortal.FixAtdl.Model.Controls.*` | 713 | 43 | 6% | 12 | 240 | 5% |
| `FixPortal.FixAtdl.Model.Types.*` | 1,049 | 138 | 13% | 55 | 530 | 10% |
| `FixPortal.FixAtdl.Validation.*` | 188 | 37 | 20% | 9 | 122 | 7% |
| `FixPortal.FixAtdl.Fix.*` | 198 | 51 | 26% | 2 | 56 | 4% |
| `FixPortal.FixAtdl.Model.Collections.*` | 496 | 131 | 26% | 33 | 169 | 20% |
| `FixPortal.FixAtdl.Model.Elements.*` | 539 | 209 | 39% | 41 | 165 | 25% |
| `FixPortal.FixAtdl.Xml.Serialization.*` | 558 | 392 | 70% | 117 | 184 | 64% |

**Notable outliers within buckets:**
- `Model.Reference`: `Regions` is a 263-line static lookup table (0% — never exercised).
- `Model.Controls`: `EnumState` (209 coverable, 0%) and `BinaryControlBase` (119 coverable, 0%) dominate.
- `Model.Types`: almost all concrete types are 0%; `UTCDateTimeTypeBase` (89%) and `Percentage_t` (45%) are the outliers on the high side.
- `Xml.Serialization`: the best-covered core namespace thanks to the XML round-trip tests — `ElementFactory` (71%) and `StrategiesReader` (72%) carry it.

## Mutation testing
Stryker `mutate` scope is currently `**/ParameterCollection.cs` only. Phase 1 broadens this to the core namespaces.

## Finalized coverage bar (D2)

The provisional ≥80% line / ≥70% branch target is **not achievable** against the current baseline (32% line / 19% branch overall; worst core namespaces are at 0–6% line). Setting that bar as a Phase 1 exit gate would require the entire codebase to be written green from scratch.

The realistic Phase 1 targets, calibrated against what a focused test-hardening pass can move:

> **Target for Phase 1 exit: ≥ 60% line / ≥ 45% branch on the core namespaces** (excluding trivial marker/DTO types such as `Country_t`, `Market_t`, `Region_t`, `SecurityType_t`, `StrategyEdit_t`, and the pure exception subclasses).

Rationale: the two best-covered namespaces (`Xml.Serialization` at 70%/64% and `Model.Collections.ParameterCollection` at 100%/94%) demonstrate that focused test coverage can reach 60–70%+ line coverage on complex production code. Extending that effort to `Model.Controls`, `Model.Types`, `Validation`, and `Fix` — which together represent the majority of the uncovered lines — makes 60%/45% a stretch-but-achievable bar that meaningfully improves confidence without requiring exhaustive coverage of every DTO property accessor.

The ≥80%/≥70% bar should be re-evaluated as a Phase 2 target once Phase 1 numbers are in.
