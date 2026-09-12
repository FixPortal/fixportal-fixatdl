# FIXatdl Conformance Implementation Plan

> Execute inline with superpowers:executing-plans. Keep the checklist current.

**Goal:** Correct and demonstrate the shared FIXatdl behaviour across core, WPF and React.
**Architecture:** Specification-backed examples exercised through the real model,
adapter and DTO paths. Reuse existing evaluators, controls and test infrastructure.
**Tech Stack:** .NET 10, WPF, React 19, TypeScript 6, xUnit v3, Vitest.
**Spec:** ../specs/2026-09-12-fixatdl-conformance-design.md

## Global constraints

- FIXatdl 1.1 with December 2010 errata; no assumptions based solely on parity.
- Backend owns DTO changes; regenerate contract snapshots before consumer integration.
- Preserve primary checkout changes. No EMS changes or production deployment.
- One finished push per repository; rebase merge and update local checkouts safely.

## Work

- [x] Baseline: run current core, WPF and React suites; record source versions and
  specification provenance alongside the conformance matrix.
- [x] Expression corpus: extend `tests/FixPortal.FixAtdl.Tests/Conformance/` with
  synthetic model cases and expected booleans; exercise corresponding React ASTs
  in `src/StateRuleEvaluator.test.ts`. Correct the simulator AST/evaluator seam
  wherever it loses field references or value semantics.
- [x] State transitions: add a common JSON scenario corpus, run it through WPF
  `EditViewModel` and React `useAtdlFormState`, then fix initial inverse effects,
  null restoration and convergence with submission-blocking cycle errors.
- [x] Mapping and validation: test XML-to-DTO bounds, initialization and control
  metadata in the simulator; correct loss there and consume the metadata in React.
  Add real WPF read-back regressions for shared parameters and invalid values.
- [x] Time/amendment and output: verify existing core timezone/loaded-order cases
  through adapters, fix deviations and record the supported boundary explicitly.
- [ ] Integration: refresh backend/frontend contract snapshots and package
  dependencies in order; verify the actual simulator uses the corrected library.
- [ ] Finish: run required suites, update conformance evidence and limitations,
  commit, PR, rebase-merge and clean up owned worktrees when complete.

Implementation evidence: [conformance matrix](../../conformance.md). Local consumer integration is verified; package publication and simulator rollout complete in dependency order after the core PR.
