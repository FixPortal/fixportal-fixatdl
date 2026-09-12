# FIXatdl conformance work, September 2026

This work checks the implemented FIXatdl 1.1 model and its WPF and React consumers
against the December 2010 errata. It is regression evidence for the supported
surface, not certification of every FIXatdl schema feature or every FIX type.

The specification source is [FIX Trading Community's official repository](https://github.com/FIXTradingCommunity/fixatdl-specification/tree/9bf2b793814a29020760f45e42fa395392fa982b/v1-1-STANDARD),
including `publication/FIXatdl-1 1-Specification_with_Errata_20101221.pdf`.

## Tested boundaries

| Boundary | Result and evidence |
|---|---|
| Typed edits | Native parameter strings preserve leading zeroes; custom Boolean literals, `field2`, missing values and exactly-one XOR have model regressions in `ExpressionAndParameterConformanceTests`. |
| XML edit references | Correctly namespaced `val:EditRef` loads through the real parser. The reusable `editref-staterule.xml` fixture exposed and now guards the constructor mapping. |
| State-rule effects | Core evaluates expressions; the adapters apply effects. The identical `Fixtures/Conformance/state-transitions.json` scenarios run through WPF `EditViewModel` and React `useAtdlFormState`: initial inverse enabled/visible effects, NULL restoration, repeated cycles, cascading assignments and a non-converging rule. |
| Enumerations | Selection equality ignores declaration order. Inverted lists distinguish uninitialized/explicitly NULL values from a deliberately empty selection, decode multi-token FIX initialization, and restore snapshots. |
| Binary controls | StateRule literals use enum IDs for enum-backed checkboxes/radios. Shared radio parameters read from the selected member. Boolean `field2` retains native Boolean semantics; literal NULL comparison respects a suppressed wire mapping. |
| Numeric controls | Numeric and discrete sliders are both supported. Numeric default lower bounds follow the specification and allow explicit overrides. WPF tests exercise actual keyboard editing, including one-sided bounds. |
| Time | Daily timestamp bounds use the timestamp's market date and timezone. WPF and React preserve an untouched loaded instant during a DST overlap; edited wall times use the core's lenient timezone policy. |
| Validation/read-back | Required values, inclusive bounds, enum mappings, and strategy edits are tested at model and adapter boundaries. Invalid values or non-converging state rules block adapter validation. |
| Amendments | Adapters accept explicit amendment context, retain loaded values, and prevent changes to immutable parameters and conflicting radio-group selections. |
| Simulator contract | The simulator mapper reads constraints from parameter value types and carries strategy edits, both edit operands, initialization policy, enum/radio mappings, increments, and clock metadata to React. The backend owns the copied expression corpus and DTO snapshots. |

## Core verification

The starting core revision was `bd0e35b` with 731 passing tests. The completed
core source has **822 passing tests**, no failures or skips. CSharpier checks
219 files. The coverage command below reports **76.1% library line coverage**,
above the existing 70% floor. WPF's corresponding local integration run has
89 passing tests against the updated core package.

```sh
dotnet test --solution FixPortal.FixAtdl.slnx -c Release
dotnet csharpier check .
dotnet dotnet-coverage collect -f cobertura -o coverage.cobertura.xml "dotnet test --solution FixPortal.FixAtdl.slnx -c Release --report-xunit-trx --timeout 5m"
```

Run `scripts/assert-coverage-floor.ps1` with `-ReportPath coverage.cobertura.xml
-Package FixPortal.FixAtdl -MinimumLineRate 70` to check the library floor.
The source changes are intended for core package **1.0.5**; local intermediate
packages 1.0.2–1.0.4 were integration candidates, not releases.

## Findings that were refuted

Core XOR already required exactly one true operand. Core existence checks already
handled empty values, false and zero correctly. Core numeric bounds were already
inclusive. Boolean `field2` comparisons intentionally compare native Boolean
values even when parameters declare different wire spellings. None of these
was replaced merely to agree with a browser implementation.

## Explicit limits

- The core model does not schedule UI effects. WPF and React own state transitions,
  convergence errors, and radio selection exclusion.
- The model does not build a complete FIX order or implement StrategyParametersGrp
  output. The React group emitter remains a preview; the host validates and builds
  the final order.
- `DateTime` cannot represent year 0000 or leap-second 60. These are rejected.
  A Clock `initValue` carrying its own explicit UTC offset (the base XML Schema
  `time` type's `hh:mm[:ss]{+,-}hh:mm` form) now resolves directly from that
  offset, taking precedence over `localMktTz`-based zone resolution; `localMktTz`
  remains a required attribute regardless, per the spec's attribute table.
- Core DateTime wire formatting and the WPF clock's minute-level edit UI retain
  their existing precision policies. Preserving an untouched loaded value is
  tested separately from editing it.
- Standard `FIX_*` operands still need a host value provider. Comparisons are now
  type-directed via `FixFieldTypes` (generated from the FIX 5.0 SP2 field
  dictionary): only fields whose actual FIX type is numeric are decimal-parsed for
  comparison, so a numeric-looking String/Char field (e.g. a zero-padded ClOrdID)
  compares as text rather than being silently converted to a number. A FIX field
  outside that dictionary (custom/extension) falls back to the previous
  parse-and-guess behaviour.
- Browser validation does not replace XML schema validation, current ISO code
  lists, or host-side validation before order submission. Unsupported binary-data
  comparisons are reported as unsupported rather than treated as satisfied.
- Full schema semantics, arbitrary custom parameter implementations, conflicting
  timezone annotations on bounds, and every possible interaction among rules have
  not been exhaustively assessed.

Consumer release order is core, WPF/React, then simulator dependencies. EMS
integration is the separate fifth programme goal.
