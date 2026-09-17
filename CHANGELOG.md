# Changelog

Notable changes to `FixPortal.FixAtdl`. Format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project uses
[semantic versioning](https://semver.org/spec/v2.0.0.html).

Dates are the release-tag date, in UTC. Entries are consumer-facing: build,
CI, and test-infrastructure commits are omitted unless they change what a
consumer sees. No `1.0.2`–`1.0.4` release was tagged.

## [Unreleased]

### Added

- Diagrams. `docs/architecture/README.md` gains a parse-to-emit pipeline, both
  type hierarchies as trees, a six-layer stack and the three-pack architecture;
  `docs/usage.md` gains an end-to-end host sequence; the README leads with the
  three-pack. All four ASCII block diagrams and the one hand-written Mermaid
  graph are gone. Sources are self-contained HTML in `docs/diagrams/`, exported
  to PNG in `docs/images/` so they render on GitHub *and* the NuGet gallery.
- `docs/usage.md` — consumer guide covering loading, value setting, validation,
  FIX output, and the exception surface.
- `docs/api.md` — consumer-facing list of the types a host actually calls.
- README now leads with the WPF and React adapters, so readers wanting a rendered
  strategy form find the finished implementations instead of being told to build
  their own.
- Architecture overview notes that graphify / understand-anything artefacts
  are generated locally (gitignored) and includes the three-pack data flow.
- README hero banner and social preview under `docs/images/`.
- This changelog.
- `IsoCurrencyCode` gains the four active ISO 4217 assignments missing from the
  enum — TMT, VED, XCG, ZWG. Withdrawn codes ZWL and CUC are deliberately
  absent; the type remarks document the retained historic (TMM, ZWD, ANG) and
  de-facto (SPL, GGP, IMP, JEP, TVD) codes.
- Host-registered custom parameter types validate eagerly: a value-type
  `ClrType` is rejected at registration (attribute writes would be lost to
  boxing), and each attribute property path is resolved up front under the rule
  the deserializer enforces, so a mismatched mapping fails at registration
  rather than at first document use.

### Changed

- `InternalErrorException` now derives from `FixAtdlException`, so catching the
  library's documented base exception no longer misses it.
- Enum wire parsing rejects a comma-bearing value for a non-`[Flags]` enum
  instead of silently OR-ing the members into a different defined value.
- Package metadata URLs (`PackageProjectUrl`/`RepositoryUrl`) now name the real
  `FixPortal/fixportal-fixatdl` repository. The csproj `<Version>` is 1.1.3
  (unreleased); the latest tagged NuGet.org release remains 1.1.2.

### Fixed

- FIX field values spelled with an exponent (`1E2`) were decimal-parsed as the
  expanded number (100) when read through `FixFieldValueProvider` and compared
  in an `Edit_t` FIX-field condition. The FIX numeric alphabet has no exponent,
  so both sites now use the model's own `Atdl.FixDecimalStyles` and the
  spelling fails, as it already did in `Float_t`.
- The NuGet gallery listed a second author literally named
  `originally Steve Wilkinson (Atdl4net)`, because MSBuild splits `Authors` on
  `;` and the prose separator was read as one. Two clean author names; the
  relationship between them stays in `Copyright` and `NOTICE`.
- `docs/usage.md` stated `InternalErrorException` derives from `Exception`
  directly, so a blanket `catch (FixAtdlException)` would miss it. It derives
  from `FixAtdlException` like every other library exception; the guide now
  says so. The same section then claimed `catch (FixAtdlException)` covers the
  whole surface - it does not. The BCL types the library also raises
  (`InvalidCastException` on a control/parameter type mismatch,
  `InvalidOperationException` from the emitter's SOH guards) are now tabulated.
- `docs/usage.md` stated `RunAllStateRules()` mutates control enabled/visible
  state. Core only evaluates each rule into `CurrentState`; adapters apply
  effects. The custom-parameter-type sample now imports
  `FixPortal.FixAtdl.Xml.Serialization`.
- `docs/conformance.md` mixed two passes (822 vs 1261 tests) and still aimed
  the work at package 1.0.5. The figures now match the September 2026 record.
  It also denied StrategyParametersGrp output, which
  `StrategyParametersGrpEmitter` has provided since 1.0.6.
- `docs/architecture/README.md` layer table counted `Configuration` as 2 files
  (it is 1) and named an `ExceptionInfo` type that does not exist in the source.
- README Status used a relative changelog link (broken on the NuGet gallery)
  and did not distinguish the published 1.1.2 from in-tree 1.1.3.

- The NuGet package now carries `LICENSE` and `NOTICE` alongside the README, so
  the upstream Atdl4net attribution travels with the redistributed binary.
- The package `<Authors>` no longer overrides the solution-wide value, so the
  gallery listing credits Steve Wilkinson (Atdl4net) as well as FixPortal.
- Decimal parsing across the model rejects a thousands separator: `NumberStyles.Number`
  read `"1,5"` as `15`, silently and by a factor of ten, for a wire value the FIX
  protocol cannot produce. `Control_t.TryConvertToDecimal`, `NumericControlBase`'s
  FIX-load and `SetValue` paths, `Slider_t`'s `initValue` pre-seed, `Edit_t`'s
  numeric-string normalisation and `EditValueConverter`'s decimal arm now share one
  `Atdl.FixDecimalStyles` constant. The WPF adapter already rejected these values.
- `StrategyParametersGrpEmitter` rejects a parameter name or wire value containing
  the FIX field delimiter (SOH). Names come from broker-supplied ATDL XML and were
  emitted into tag 958 unchecked, which would split one field into two and inject
  arbitrary FIX fields once a host joined the emitted tuples onto the wire.
- `DropDownList_t` carried a stale upstream header claiming GNU LGPLv3 and
  referencing a `Licenses/Commercial.txt` that does not exist in this repository.
  It now matches the MIT header every other file carries; the licence is unchanged.
- README no longer claims the public surface is locked by `PublicAPI.Shipped.txt`;
  that analyzer was removed before `1.0.5` (see below).
- Time-only UTC `Clock_t` parameter values re-anchor to today's UTC date instead
  of resolving as market-local wall clock and re-emitting zone-shifted.
- `FIX_` field operands facing an `IParameter` convert against the parameter's
  native value type, so `Char_t`/`Boolean_t`/date-time/`MonthYear_t`/`Tenor_t`/
  ISO-enum parameters compare correctly (`EQ`/`NE`/inequalities).
- Numeric text in a text control facing a non-numeric literal compares as
  strings instead of throwing `InvalidFieldValueException`.
- Date-less timestamp bounds classify from the parse result, and `localMktTz`
  is validated eagerly at load.
- A declared UTC leap second (`":60"`) normalises by rolling forward one second
  in `ConvertFromWireValueFormat`, matching the const/control paths; a leap at
  the last representable instant is rejected as an invalid value.
- Empty `String_t`/`Data_t` values are treated as unset, and `MonthYear_t`/
  `Tenor_t` clear from a text control.
- Control/parameter wire conversions round-trip symmetrically:
  `TextControlBase.ToBoolean` decodes through `Boolean_t.ParseWireValue`;
  `Language_t` emits lower-case ISO 639-1 codes; FIX initialisation honours a
  `Boolean_t` parameter's declared wire mapping; unmatched free text reloads as
  `EnumState.NonEnumValue`; matching shapes update the existing `EnumState` in
  place; `FixMessage.ToFix` refuses empty field values, not just null.
- Precision-rounded output is re-validated before wire emission.
- Date-less `Clock_t` `SetValue` strings resolve via the `initValue`
  market-zone path, keeping date-only bindings on their calendar day.
- A numeric `Slider_t` skips its `initValue` pre-seed when the value cannot
  parse, so `UseFixField` still runs.
- Edit evaluation fails fast: an `Edit` with neither `operator` nor
  `logicOperator` throws at `Resolve`; an `EditRef` under a global `Edit` is
  rejected; property-setter failures unwrap from `TargetInvocationException`;
  redundant null assignments are a no-op; evaluating an unresolved edit throws
  a domain error instead of a `NullReferenceException`.
- FIX wire values compare ordinally, and explicit `NumberStyles` apply across
  the numeric parsing surface (`IsNumericFixField` now returns the non-numeric
  default its comment describes).
- Strategy control index hardened: ungrouped radio siblings index correctly,
  `Reset` is sender-scoped, and a control's `Id` rejects renames once the
  control belongs to a panel (the setter stays public for construction-time
  and deserialization assignment).
- `StrategyParameterType` (FIX tag 959) codes corrected for `Language_t` and
  `Tenor_t`.
- Wire parsing hardening: doubled SOH rejected, empty wire values filtered from
  tag 960, bare numeric user tags resolve.
- `{NULL}` sentinel mapping mirrored at the reference-type base; `ToEnumState`
  uses the subclass wire converter; `ConstValue` coalesces in messages.
- `decimal.MaxValue` is no longer used as an invalid sentinel; `EnumState` id
  arrays are cloned; clock FIX-load conversion failures are caught.
- `Description_t` string conversion is null-safe; same-typed numeric edit
  operands compare natively; `WireValue` is annotated `DisallowNull`.
- Layout indexes refresh on `Move`; a removed control detaches from its panel;
  a shared radio parameter is reported once.
- `EditValueConverter.ConvertToComparableType` guards null operands before the
  null-prototype early return, so a `(null, null)` call throws the documented
  `IllegalUseOfNullError`.
- `Price_t`/`PriceOffset_t`/`Percentage_t` class remarks now name the FIXatdl
  Errata default `minValue` of 0 these types apply, and the explicit negative
  bound (e.g. `minValue="-1"`) that opts out of it.
- `Float_t` wire parsing rejects exponent notation and whitespace padding, and
  `ValueConverter`'s decimal conversion rejects exponent notation (the XML
  Schema decimal grammar excludes it); both previously parsed scaled values
  such as `"1E2"` as `100`.
- Time-only bound/operand classification and the date-time parameter conversion
  path apply the parse path's leap-second and excess-fraction normalisation, so
  valid values such as `"23:59:60"` or a 9-digit fraction are not misclassified
  as date-bearing or rejected.
- Parameter updates from controls deduplicate only radio groups; non-radio
  controls sharing a parameter keep the collection-order overwrite (last one
  wins) instead of silently dropping later controls' values.
- A numeric `Slider_t` no longer restores a value seeded by an earlier
  `LoadInitValue` when a later call has a null or unparseable `initValue`.
- `FIX_` field operands facing a `Boolean_t` parameter parse through the
  parameter's declared wire mapping (custom true/false tokens), rejecting
  undeclared tokens exactly as the literal path does.
- `ControlCollection` finalizes removal detachment and layout-index refresh
  before the change notification reaches observers, and `Clear()` detaches
  every removed control.
- Removing every control of a panel one by one no longer retains the panel
  from the strategy's control index; emptied sender-tracking entries are
  dropped.

## [1.1.2] — 2026-09-12

### Fixed

- Release pipeline uses the NuGet policy creator for OIDC login. Packaging only;
  no library change.

## [1.1.1] — 2026-09-12

### Changed

- Packages publish to NuGet.org under the FixPortal organization.

## [1.1.0] — 2026-09-12

### Added

- `StrategiesReader` accepts a caller-supplied `XmlSchemaSet` for XML Schema
  validation before deserialization. The FIXatdl 1.1 XSD is FIX Protocol
  Limited's licensed schema and is not vendored here; supply your own.
- Host-registered custom `Parameter` `xsi:type` extensions, so a vendor
  parameter type whose CLR type lives in the caller's assembly can be resolved.
- Type-directed `FIX_` field comparisons, driven by a FIX field type dictionary
  rather than string comparison.
- Declared UTC leap seconds are normalised rather than rejected.
- Offset-bearing `Clock` `initValue` resolves without a `localMktTz`.

### Fixed

- Offset-bearing timestamp bounds anchor against UTC, not zone-local time.
- Bare-hour offset suffixes are recognised as offset-anchored.
- An offset-time `initValue` anchors "today" in its own offset, not UTC.
- `ClrType` is validated eagerly, and a duplicate registration reports a clear
  error instead of failing later.
- A caller-supplied `XmlSchemaSet` is compiled eagerly, avoiding a race.

## [1.0.6] — 2026-09-12

### Added

- `StrategyParametersGrpEmitter` emits the FIX StrategyParametersGrp sequence
  (tags 957–960) from core, for hosts using `Tag957Support` transport instead of
  or alongside direct per-parameter FIX tags.

## [1.0.5] — 2026-09-12

### Added

- `docs/conformance.md` — assessed FIXatdl 1.1 scope and limits against the
  December 2010 errata, with the shared adapter scenarios.

### Fixed

- Selection identity and typed initialization edge cases.
- FIXatdl control mappings and parameter constraints.
- Typed edit comparisons and required operands.

### Removed

- `PublicApiAnalyzers` and the `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt`
  tracking files (2026-07-04). The public surface is no longer guarded by a
  build-breaking analyzer; semantic versioning is the contract.

## [1.0.1] — 2026-06-03

### Changed

- Cleared IDE and style warnings; CA1062 and CA2007 policy locked.

## [1.0.0] — 2026-06-03

First stable release of the modernised fork.

### Added

- FIXatdl 1.1 parsing, validation, and FIX-tag emission targeting `net10.0`.

### Changed

- Namespace `Atdl4net.*` → `FixPortal.FixAtdl.*`.
- `Common.Logging` → `Microsoft.Extensions.Logging.Abstractions`.
- `System.Configuration` glue replaced with the `FixAtdlOptions` POCO.
- Nullable reference types enabled throughout.

### Removed

- The upstream WPF rendering layer; the library is UI-agnostic. Editable
  desktop and browser forms live in the
  [WPF](https://github.com/FixPortal/fixportal-fixatdl-wpf) and
  [React](https://github.com/FixPortal/fixportal-fixatdl-react) adapters.

## [0.1.0] — 2026-05-24

First packaged fork of [Atdl4net](https://github.com/atdl4net/atdl4net),
pre-release.

[Unreleased]: https://github.com/FixPortal/fixportal-fixatdl/compare/v1.1.2...HEAD
[1.1.2]: https://github.com/FixPortal/fixportal-fixatdl/compare/v1.1.1...v1.1.2
[1.1.1]: https://github.com/FixPortal/fixportal-fixatdl/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/FixPortal/fixportal-fixatdl/compare/v1.0.6...v1.1.0
[1.0.6]: https://github.com/FixPortal/fixportal-fixatdl/compare/v1.0.5...v1.0.6
[1.0.5]: https://github.com/FixPortal/fixportal-fixatdl/compare/v1.0.1...v1.0.5
[1.0.1]: https://github.com/FixPortal/fixportal-fixatdl/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/FixPortal/fixportal-fixatdl/compare/v0.1.0...v1.0.0
[0.1.0]: https://github.com/FixPortal/fixportal-fixatdl/releases/tag/v0.1.0
