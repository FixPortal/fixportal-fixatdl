# FIXatdl cross-library correctness

## Objective

Goal 3 of the approved programme: establish and correct FIXatdl behaviour across
the headless core, WPF adapter and React library before migrating EMS. The user
approved comparing shared examples against expected results and fixing discrepancies.

Use FIXatdl 1.1 with the December 2010 errata as the baseline. The specification,
not agreement between implementations, determines the expected behaviour. Keep
the current host boundaries: core parses and validates, WPF consumes its models,
React consumes the simulator's backend-owned DTO. Extend that DTO where it loses
information required for correct rendering or validation. Do not add a second
XML parser to React or change EMS in this pass.

## Evidence and implementation order

1. Expression evaluation: typed values, missing/null/empty values, field-to-field
   comparisons, boolean and enum controls, compound operators and invalid trees.
2. State transitions: initial false conditions, inverse enabled/visible effects,
   ordinary value assignment, `{NULL}` clear/restore, cascades and cyclic rules.
3. Parameter/control mapping: bounds, initialization, binary enum mappings,
   radio groups, hidden controls, numeric increments and panel ordering.
4. Validation and read-back: required values, types/ranges, strategy edits,
   multiple controls for one parameter, enums and uninitialized field omission.
5. Time and amendments: explicit host time context, timezone conversion and
   preserving loaded values/mutability. Distinguish preview from authoritative FIX
   encoding; do not present approximate output as conformance evidence.

For each domain, use small synthetic inputs with named expected results, test the
real core/model path and WPF/React paths, reproduce deviations before fixing them,
and record coverage and remaining limitations. Existing real-world-derived XML
fixtures supplement the small cases. Passing a test count alone is not completion.

## Known observations to reproduce

- React derives enabled/visible state only from currently true rules, losing
  inverse effects. WPF skips initial false rules entirely.
- React's value-rule pass does not settle cascades or restore `{NULL}` values.
- The simulator AST builder does not carry `Edit.field2` into the wire contract.
- The simulator mapper emits null for bounds, precision and defaults, and omits
  strategy edits and binary/radio metadata.
- WPF reads FIX values by control, so controls sharing one parameter can produce
  duplicate tags. Confirm with a radio-group case before changing it.
- The existing JSON expression corpus compares React with the simulator evaluator,
  not the headless core, and therefore cannot by itself establish three-way parity.

## Delivery and safeguards

Work in `D:/fix-portal/fixatdl-conformance/{core,wpf,react}` on separate
`fix/fixatdl-conformance` branches based on each origin/main. Add simulator
worktrees only when its mapping/contract needs editing. Preserve primary-checkout
changes and earlier WPF review work. Keep .NET public API additions compatible;
release and integrate dependent packages in order. Never force dependency
installation past a peer constraint.

Run each repository's required local checks before its final push. Use .NET's
existing xUnit v3/AwesomeAssertions conventions and React's existing Vitest setup.
No new test framework, deployment, EMS migration or unrelated cleanup is required.
