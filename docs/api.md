# API reference — `FixPortal.FixAtdl`

> Consumer-facing public surface of package 1.1.x. Walkthroughs stay in
> [usage.md](usage.md); this page is the list of types a host actually
> calls. XML comments on the packed assembly are the per-member contract.

Anything public in `Xml.Serialization`, `ThrowHelper`, ISO code tables, or
the control visitor is extension machinery, not a host API, and may change
in a minor release.

## Parse

| Type | Namespace | What it is |
|---|---|---|
| `StrategiesReader` | `FixPortal.FixAtdl.Xml` | Entry point. `Load(Stream)` / `Load(string path)` deserialize, `ResolveAll`, and inject the reader clock into every `Clock_t`. Concurrent `Load` on one instance is supported. XXE-hardened (`DtdProcessing.Prohibit`, `XmlResolver = null`). |
| `StrategyLoaded` | same | Fires per strategy after deserialization and **before** cross-reference resolution. Treat as "parsed", not "valid". |
| `CustomParameterType` | `FixPortal.FixAtdl.Xml.Serialization` | Host registration for a vendor `Parameter` `xsi:type`. Checked at reader construction. |

Constructor arguments on `StrategiesReader` are all optional: `ILoggerFactory`,
NodaTime `IClock`, `XmlSchemaSet`, `IReadOnlyList<CustomParameterType>`.

## Model

| Type | What a host uses it for |
|---|---|
| `Strategies_t` | Document root. Indexer `strategies["TWAP"]`, `Strategies` collection, `Tag957Support`, `StrategyIdentifierTag`. |
| `Strategy_t` | Aggregate. `Parameters`, `Controls`, `StrategyEdits`, `StrategyLayout`, `Name`, `WireValue` (algo identifier). Methods below. |
| `IParameter` / `Parameter_t<T>` | Wire layer. `Name`, `FixTag`, `Type`, `Use`, `WireValue`, `IsSet`, `Reset()`, `MutableOnCxlRpl`. `definedByFIX` is informational only. |
| `Control_t` and the 15 `*_t` controls | UI-agnostic entered-value layer. `Id`, `ParameterRef`, `SetValue` / `GetCurrentValue`, `StateRules`. There is **no** `Enabled`/`Visible` on the control. |
| `StateRule_t` | One rule: `Enabled`/`Visible`/`Value` effects plus an `Edit` condition. `CurrentState` is the evaluated boolean. |
| `StrategyEdit_t` | Strategy-level business rule with error text. |
| `StrategyLayout_t` / `StrategyPanel_t` | Nested panel tree for a custom renderer. The adapters walk this; a headless host can ignore it. |

### `Strategy_t` methods

| Method | Role |
|---|---|
| `LoadInitialControlValues(FixFieldValueProvider)` | Seed controls from `initValue` / `UseFixField`. |
| `LoadParameterValues(FixFieldValueProvider, bool)` | Seed parameters from inbound FIX. |
| `UpdateControlValuesFromParameters()` | Push parameter values into controls. |
| `TryUpdateParameterValuesFromControls(bool, out …)` | Push control values into parameters; collect every failure when `shortCircuit: false`. |
| `RunAllStateRules()` | Evaluate every rule's condition into `CurrentState`. Does **not** apply effects. |
| `EvaluateAllStrategyEdits(IInitialFixValueProvider, bool)` | Run strategy-level edits. |
| `Reset()` | Clear parameters and controls, then re-sync. |

## FIX emission

| Type | Namespace | What it is |
|---|---|---|
| `ParameterCollection.GetOutputValues()` | `Model.Collections` | Direct per-parameter tags. Skips unset optionals and parameters with no `fixTag`. Throws `MissingMandatoryValueException` for an unset required parameter. |
| `FixTagValuesCollection` | `FixPortal.FixAtdl.Fix` | Result of `GetOutputValues`. `ToFix()` joins `tag=value` with SOH and **rejects** a value that itself contains SOH. |
| `StrategyParametersGrpEmitter.Emit(Strategy_t)` | same | Tags 957–960 for `Tag957Support` transport. Unset parameters omitted; tag 957 omitted when nothing is set. Rejects SOH in the parameter **name** (958) and **value** (960). |
| `FixFieldValueProvider` / `IInitialFixValueProvider` | same | Inbound FIX values for init/`UseFixField` and `FIX_` operands. |
| `FixMessage` | same | Parses a raw FIX string into an immutable read-only tag map. Not a session engine. |

Tag 959 type codes follow FIX 5.0 SP2 (`Language_t=26`, `Tenor_t=29`).
Unknown types fall back to String (14).

## Validation

| Type | Role |
|---|---|
| `ValidationResult` | `IsValid`, `IsMissing`, `ErrorText` on the control-update path. |
| `EditEvaluator<T>` / `Edit_t` | AND/OR/XOR/NOT grammar shared by state rules and strategy edits. XOR is exactly-one. |

Decimal wire parsing uses `Atdl.FixDecimalStyles` (no thousands separator).
`Atdl.NullValue` is the `{NULL}` token state rules use to clear a control.

## Exceptions

All in `FixPortal.FixAtdl.Diagnostics.Exceptions`. Every type there derives
from `FixAtdlException`, but that base does **not** cover the whole surface:
the library also raises `InvalidCastException` (control/parameter type
mismatch), `InvalidOperationException` (the emitter's SOH guards),
`ArgumentException` and `NotSupportedException`. See
[usage.md](usage.md#exceptions) for the raise table and the BCL types.

`RenderingException` is retained for the WPF adapter (no layout / no root
panel). This package no longer throws it.

## Adapters

This package is headless. Rendered, editable forms:

- [`FixPortal.FixAtdl.Wpf`](https://github.com/FixPortal/fixportal-fixatdl-wpf)
  consumes `Strategy_t` directly.
- [`@fix-portal/fixatdl-react`](https://github.com/FixPortal/fixportal-fixatdl-react)
  consumes a JSON `AtdlStrategyDto` your backend maps from `Strategy_t`.
