# Changelog

Notable changes to `FixPortal.FixAtdl` and, from 1.3.0, `FixPortal.FixAtdl.Contracts`
(released together at the same version). Format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project uses
[semantic versioning](https://semver.org/spec/v2.0.0.html).

Dates are the release-tag date, in UTC. Entries are consumer-facing: build,
CI, and test-infrastructure commits are omitted unless they change what a
consumer sees. No `1.0.2`–`1.0.4` release was tagged.

The repository history starts at the 1.2.0 release. Versions from 1.1.2 remain
installable from NuGet.org; their notes are kept below, but their tags were
retired with the pre-release history.

## [Unreleased]

## [1.4.0] — 2026-10-07

### Fixed

- `Float_t` and the types derived from it, including `Percentage_t`, validate a value after
  rounding to `Precision`. Setting `Precision` after `constValue`, or assigning a constant
  while `Precision` is already set, throws `InvalidFieldValueException` when the rounded
  value falls outside `MinValue` or `MaxValue` and leaves the previous precision and
  constant in place. `Percentage_t` compares the rounded native fraction with those bounds.
  The message shows the amounts in whole-percent units.
- `UTCTimestamp_t` and `UTCTimeOnly_t` truncate to milliseconds when a wire value or a
  control value is assigned. A sub-millisecond instant is not rejected when the message
  is built.
- A time-only leap second rolls forward to the next second and stays on `0001-01-01`.
  `9999-12-31T23:59:60` is rejected. A minimum or maximum bound of a leap second is
  rejected when the parameter is loaded. `23:59:59` remains a valid maximum.
- `Language_t` matches an inbound wire value to an `EnumPair` without regard to case.
  The value it emits is still lower-case.
- `Int_t` accepts a FIX integer with an explicit leading `+` or `-` and rejects
  surrounding whitespace, a thousands separator, a decimal point, and an exponent.
  `SeqNum_t` and the other non-negative integer types accept a leading `+` and reject a
  leading `-`.
- An empty `constValue` is stored as unset. `0` and `false` stay set.
- A source control that already has a `ParameterRef` keeps that parameter when a helper
  is refreshed. An unbound source is still inferred from the helper. A re-entrant change
  to `ControlCollection` throws before a control's `Parent` is rewritten.
- `StrategyParametersGrp` throws `InvalidOperationException` when a set parameter has an
  empty or whitespace name and a non-empty wire value. A null name is still omitted, as
  is an unset parameter.
- A strategy edit compared with a non-numeric `FIX_` field now carries
  `comparisonType` `String_t` on the contract. `FIX_ClOrdID == "1"` does not match
  `"0001"`. A numeric `FIX_` field is left untyped and still compares as a number.
  `regions-enums` is the golden that records this.
- A `Boolean_t` parameter compared with another field carries `trueWireValue` and
  `falseWireValue` (default `Y` and `N`). `StateRuleEvaluator` matches those tokens with
  an ordinal comparison. Boolean edit literals are matched the same way.
- Numeric state-rule coercion rejects `"1,000"`, `"1000+"`, and `"1000-"`. When only one
  side of an ordering comparison parses as a number, the two sides are ordered as text,
  so `"123" < "NONE"` is true.
- `UTCDateOnly_t`, `UTCTimeOnly_t`, `UTCTimestamp_t`, and `LocalMktDate_t` comparisons
  reject a time-only or unparseable operand. `Clock_t` comparisons stay permissive.
- `FixValueFormatter` reads a JSON number as `long`, then `decimal`, then `double`, and
  rejects a fractional value for an integer FIX field. A whole number outside the `Int64`
  range throws `ArgumentOutOfRangeException`. Enum identifiers use the invariant culture.
- A strategy edit with neither `Edit` nor `EditRef` throws `AtdlParseException` instead
  of becoming an empty `AND`. The AST builder rejects a compare that sets both a value
  and `field2`, an operator combined with logic or children, and an empty `AND`, `OR`,
  or `XOR`.
- A parameter `constValue` that is not already a scalar is mapped from its wire value.
  `initFixField` accepts a positive numeric tag, digits only, or a known `FIX_` name. It is
  omitted when the name is empty, unknown, zero, signed, or surrounded by whitespace.
- `AND` and `OR` still return as soon as the result is known. The corpus cases
  `and_short_circuit_skips_invalid_data_operand` (expected `false`) and
  `or_short_circuit_skips_invalid_data_operand` (expected `true`) record that. The React
  evaluator currently returns null for both. A `Data_t` comparison that is actually
  evaluated throws.

## [1.3.1] — 2026-10-07

### Fixed

- The package README on NuGet.org: the quick start prints numeric FIX tags instead of
  `FixField` names, the status table names `@fix-portal/fixatdl-react` 0.4.0 and WPF 1.0.6, and the
  three-pack diagram text shows `FixPortal.FixAtdl.Contracts` doing the DTO mapping.
  No code changes; `FixPortal.FixAtdl.Contracts` is released at 1.3.1 to keep the two
  packages at the same version.

## [1.3.0] — 2026-10-06

### Added

- New package `FixPortal.FixAtdl.Contracts`: the JSON contract consumed by `@fix-portal/fixatdl-react` (DTO records, `AtdlDtoMapper.Map`, the state-rule AST builder and evaluator, `AtdlContractJson.Options`). Released in lockstep with `FixPortal.FixAtdl`. The shared state-rule corpus now lives in `contracts/state-rule-cases.json`.

## [1.2.0] — 2026-10-03

### Added

- `FixMessage.GetValues` and `FixTagValuesCollection.GetValues` return every
  occurrence of a repeated tag in wire order, for hosts reading
  repeating-group members.

### Changed

- `AtdlValueType<T>` and `AtdlReferenceType<T>` now derive from a new public abstract base
  `AtdlParameterTypeBase<TStorage>` (in `Model.Types.Support`), which carries the value/wire/conversion
  machinery the two pivots previously duplicated. Source- and behaviour-compatible for consumers:
  every member keeps its existing signature and semantics. Compiled consumers that call members
  inherited from the pivots must be recompiled, as the declaring type of those members has moved.
- `FixMessage` wire parsing no longer rejects a repeated tag: every occurrence
  is preserved in wire order, enumeration and `ToFix()` cover them all (a
  parsed message round-trips byte-for-byte), and the scalar members — indexer,
  `TryGetValue`, `ContainsKey`, `Count`, `Keys`, `Values`, `FixFields` —
  resolve to the first occurrence. Group semantics (`NoXXX` count fields and
  entry boundaries) are not interpreted; that stays with the host.
  `FixTagValuesCollection.Add` still rejects a duplicate tag: an outbound
  repeated scalar is a modeling error, not a group.
- `ThrowHelper.Rethrow` and the `ValidationResult` constructor now take
  `params object?[]` instead of `params object[]` — a nullable-annotation
  correction only; binary-compatible, so existing compiled callers are
  unaffected.

### Fixed

- Empty parameter refreshes reset only bound controls and no longer infer helper checkbox state; unbound controls keep their current value.
- Required reference parameters now clear stale values when their controls are emptied, and populated helper rules apply after all bound controls sync.
- String-based FIX field lookups reject comma-joined names instead of resolving them as enum combinations.
- NuGet package author and repository metadata now inherit the fork attribution from `Directory.Build.props`.
- Helper rules now select ungrouped radio companions only when they share the same parameter.
- Null FIX field values are treated as absent by `TryGetValue`, and custom argument exceptions can use a string-only constructor.
- Registered custom flags attributes accept defined bit combinations and reject undefined bits.
- Spinner controls now reject a fractional value bound to an integer parameter instead of silently truncating it (1.5 previously went on the wire as 1); fractional values remain supported on float parameters via `DoubleSpinner_t`.
- Enum parsing rejects bare numeric strings: currency, country and language wire values such as "3" or "036" no longer map to a wrong (but defined) enum member, and the `FixTagValuesCollection` string indexer no longer resolves a numeric name such as "35" to a field — the Try-pattern lookups still accept numeric tags, including user-defined ones.
- An enum wire value that itself contains a delimiter (e.g. "A,B") now resolves by exact match instead of being split into rejected tokens.
- `FixDateTime.Parse` throws `FormatException` (previously `InvalidCastException`) for unparseable input; a `:60` seconds field is normalised as a leap second only at 23:59 UTC (a `:60` at any other minute is now rejected rather than silently shifted a minute); time-only values anchor to a deterministic date (0001-01-01, UTC) instead of the host's current date; and exact FIX formats parse culture-invariantly, so a non-invariant caller culture can no longer make a valid timestamp fail.
- `ThrowHelper.Rethrow` single-argument overloads format through the same guarded path as the params overload: a brace-bearing template surfaces verbatim instead of throwing `FormatException` from the error-reporting path, and when the original exception type cannot be reconstructed the original instance now carries the source and XML line info.
- Visitor dispatch caches by `Type` identity, so same-named visitor types from different assemblies no longer collide on a name-keyed cache entry.
- Attribute-processing failures during strategy load now name the element being processed instead of leaving the element-name slot blank.
- `StrategyParametersGrpEmitter` skips a parameter whose name is null, empty or whitespace instead of emitting a `958=` field this library's own parser rejects.
- Nullable annotations corrected to describe routinely-absent values: `Edit_t.Field2`/`Id`/`Value`, `Control_t.ToolTip`, `BinaryControlBase.CheckedEnumRef`/`UncheckedEnumRef`, and the `value` parameter of `FixDateTime.TryParse` are now `string?`; `Control_t.TryConvertToInt`/`TryConvertToUint`/`TryConvertToDecimal` documentation no longer promises `false` for input that throws.

## 1.1.6 — 2026-09-20

### Changed

- Corrected the FIXatdl Inspector link in the package README.

## 1.1.5 — 2026-09-20

### Fixed

- Nullable annotations now describe unset validation results, optional control
  attributes, and failed FIX-field lookups instead of suppressing nullability
  warnings with `null!`.
- Empty parameters now reset their bound controls when control values are
  refreshed; schema validation errors reject null input without throwing while
  constructing the diagnostic.
- Fractional offsetless timestamps use the exact FIX format table, unsigned
  XML parameter constants deserialize with their declared type, non-flag enum
  constants reject comma-separated values, and FIX tag 851 is treated as
  numeric.
- Missing control lookups now follow dictionary semantics and throw
  `KeyNotFoundException` instead of returning an undocumented null.
- Fixture tests no longer mutate the process-wide working directory; schema
  acceptance tests assert the loaded strategy, and fixture-backed XML docs and
  constructor inputs now reject malformed state explicitly.
- Tenor serialization now uses the shared diagnostic helper, and previously
  uncovered exception and edit-conversion branches have regression coverage.
- Resetting an empty parameter now also refreshes related helper controls so
  their state rules cannot remain stale.

### Changed

- `FixMessage` is now a sealed, read-only parsed message instead of inheriting
  the mutable `Dictionary` API; use `FixTagValuesCollection` to build or edit
  outbound tag values.
- Removed the unused `FixAtdlOptions` placeholder; configuration will be added
  when the library has real runtime options to expose.

- README hero and GitHub social preview drop the angel wings from the XML
  card. The markdown image URL is unchanged.

## 1.1.4 — 2026-09-17

### Fixed

- A time-only clock value set through `Clock_t.SetValue` on a control with a
  `localMktTz` was resolved against the literal year-1 date the time-only
  sentinel carries, where a zone still runs on its pre-standard Local Mean
  Time. `America/New_York` is `-04:56:02` there, so a 10:31 edit emitted
  `00010101-15:27:02` — the wrong calendar day, and seconds of LMT offset in a
  value entered to the minute. It now anchors to the market's today first,
  matching what a bare `initValue` time-of-day has always done. This is the
  path every UI time picker uses, so 1.1.3 is not safe for a host that edits a
  zoned `Clock_t`.

## 1.1.3 — 2026-09-17

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
  `FixPortal/fixportal-fixatdl` repository. The project is now at version 1.1.3;
  release 1.1.3 is tagged.

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

## 1.1.2 — 2026-09-12

### Fixed

- Release pipeline uses the NuGet policy creator for OIDC login. Packaging only;
  no library change.

## 1.1.1 — 2026-09-12

### Changed

- Packages publish to NuGet.org under the FixPortal organization.

## 1.1.0 — 2026-09-12

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

## 1.0.6 — 2026-09-12

### Added

- `StrategyParametersGrpEmitter` emits the FIX StrategyParametersGrp sequence
  (tags 957–960) from core, for hosts using `Tag957Support` transport instead of
  or alongside direct per-parameter FIX tags.

## 1.0.5 — 2026-09-12

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

## 1.0.1 — 2026-06-03

### Changed

- Cleared IDE and style warnings; CA1062 and CA2007 policy locked.

## 1.0.0 — 2026-06-03

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

## 0.1.0 — 2026-05-24

First packaged fork of [Atdl4net](https://github.com/atdl4net/atdl4net),
pre-release.

[Unreleased]: https://github.com/FixPortal/fixportal-fixatdl/compare/v1.4.0...HEAD
[1.4.0]: https://github.com/FixPortal/fixportal-fixatdl/compare/v1.3.1...v1.4.0
[1.3.1]: https://github.com/FixPortal/fixportal-fixatdl/compare/v1.3.0...v1.3.1
[1.3.0]: https://github.com/FixPortal/fixportal-fixatdl/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/FixPortal/fixportal-fixatdl/releases/tag/v1.2.0
