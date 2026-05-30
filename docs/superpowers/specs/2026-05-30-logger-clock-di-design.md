# Logger & Clock DI Refactor — Design

**Date:** 2026-05-30
**Status:** Approved (design) — pending implementation plan
**Scope:** `FixPortal.FixAtdl` library

## Problem

~34 source files carry placeholder scaffolding left over from the modernisation
pass:

```csharp
// FP Enhancement: 2026-05-23 — TODO wire injected logger when refactoring class to accept ILogger.
private static readonly NullLogger _log = NullLogger.Instance;
```

The field is always `NullLogger.Instance`, so every `_log.LogDebug(...)` call is
dead — it logs nowhere and was never wired to a real logger. Separately,
`Clock_t` reads "now" via a `virtual DateTime GetCurrentTime() => DateTime.Now`
seam, which violates the house rule against reading the clock from a static call.

The goal is to make logging actually reachable by a host application and to give
`Clock_t` a proper injectable time source, **without** a breaking change to the
published (`v0.1.0`) public surface.

## Key architectural constraint

The model objects — all 15 controls, their `Support` base classes, the elements
(`Edit_t`, `EditRef_t`, `Parameter_t`, `StateRule_t`, `StrategyEdit_t`), the
collections, and the value types — are **constructed reflectively** by
`ElementFactory`. Every control is built through a single shared
`MultiTypeElementDefinition Control_t` whose only constructor parameter is
`(string ID)` (`SchemaDefinitions.cs:475-501`). There is no per-type constructor
seam, so plain constructor injection cannot reach these objects without:

1. adding a new `SourceType` (e.g. `InjectedService`) to `ConstructorParameter`,
2. giving `ElementFactory` a service bundle / `ILoggerFactory`, and
3. changing every control constructor + base class to accept it.

That "model-service seam" was **considered and rejected** for this pass: it is
high churn for what is debug-level tracing, and it threads a service bundle
through value objects (a service-locator smell).

Consequently the **only** classes constructed at a composition root reachable
for ordinary constructor injection are:

- `StrategiesReader` — the public entry point; a host does `new StrategiesReader()`.
- `ElementFactory` — `new`-ed internally by `StrategiesReader`.

`EditEvaluator<T>` is **not** injectable: its only concrete subclasses,
`StateRule_t` and `StrategyEdit_t`, are reflectively constructed
(`SchemaDefinitions.cs:346,:559`). `FixFieldValueProvider` is `new`-ed by the
reflectively-built `Strategy_t`. `ControlValidationState` is created per-control
during validation. `ThrowHelper` and `ModelUtils` are `static` classes. None of
these can take constructor injection under the chosen scope.

## Design

### 1. Injection set — wire a real logger into the deserialization pipeline

**`StrategiesReader`**

- Add an optional constructor: `public StrategiesReader(ILoggerFactory? loggerFactory = null)`.
- Store `_loggerFactory = loggerFactory ?? NullLoggerFactory.Instance`.
- Create `_log = _loggerFactory.CreateLogger<StrategiesReader>()` (instance field, replacing the current `NullLogger`).
- The existing parameterless `new StrategiesReader()` continues to work
  (source-compatible — the new parameter is optional).

**`ElementFactory`**

- Add `ILoggerFactory` to the constructor (optional, defaulting to
  `NullLoggerFactory.Instance` to keep its `public` ctor source-compatible):
  `public ElementFactory(ElementDefinition elementDefinition, Type notifyCreationOfType, ILoggerFactory? loggerFactory = null)`.
- `StrategiesReader.LoadStrategies` passes its `_loggerFactory` through when it
  constructs the factory.
- Convert the current `private static readonly NullLogger _log` to an instance
  `ILogger<ElementFactory> _log = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<ElementFactory>()`.
- Turn the two private `static CreateRawObject` helper overloads into instance
  methods so they can use the instance `_log`. They are internal-only and only
  use `_log` for debug tracing + `ThrowHelper`, so this is safe.

**Rationale.** `ElementFactory` already emits rich per-object construction
tracing (`CreateObject`, `GetConstructorParameters`, `ProcessAttributes`,
`ReadAttribute`, `SetPropertyValue`, …). Wiring it to a real `ILoggerFactory`
gives a host a full deserialization trace on demand, bookended by
`StrategiesReader`'s load-level messages. That is the genuine, coherent logging
surface for a parsing library.

### 2. Prune set — remove dead tracing

From every reflectively-constructed model object and static utility listed
below, delete: the `// FP Enhancement … TODO wire injected logger` comment, the
`NullLogger _log` field, and **all** `_log.*` call sites — both the guarded
`if (_log.IsEnabled(LogLevel.Debug)) { _log.LogDebug(...); }` tracing blocks
**and** the unguarded `LogWarning`/`LogError` calls. Remove the now-unused
`Microsoft.Extensions.Logging[.Abstractions]` usings where they become redundant.

> **Note — non-Debug calls.** A handful of model classes carry `LogWarning` /
> `LogError` (not just `LogDebug`) in **log-and-swallow** failure paths:
> `InitializableControl` (2× `LogWarning` when a control cannot init from a FIX
> field), `BinaryControlBase` / `NumericControlBase` / `ListControlBase` (1× each,
> `LogError` then `return false`), and `AtdlValueType` / `AtdlReferenceType` (1×
> each, conversion failure). All are against `NullLogger` today, so removing them
> is **behaviour-preserving** — the method still returns `false` / continues. It
> does, however, drop the latent intent to surface *why* a load/convert failed;
> restoring that visibility is exactly what the deferred model-service seam would
> enable, and is the concrete motivation recorded under "out of scope".

- **Controls (15):** `CheckBox_t`, `CheckBoxList_t`, `Clock_t`, `DoubleSpinner_t`,
  `DropDownList_t`, `EditableDropDownList_t`, `HiddenField_t`, `Label_t`,
  `MultiSelectList_t`, `RadioButton_t`, `RadioButtonList_t`, `SingleSelectList_t`,
  `SingleSpinner_t`, `Slider_t`, `TextField_t`.
- **Control `Support` bases (6):** `BinaryControlBase`, `EnumState`,
  `InitializableControl`, `ListControlBase`, `NumericControlBase`,
  `TextControlBase`.
- **Elements:** `Edit_t`, `EditRef_t`, `Parameter_t`.
- **Evaluation base:** `EditEvaluator` (its subclasses `StateRule_t` /
  `StrategyEdit_t` carry no own `_log` field — pruning the base covers them;
  their remaining `IDisposable` / `Unbind` TODOs are unrelated and out of scope).
- **Collections (4):** `EditEvaluatingCollection`, `ReadOnlyControlCollection`,
  `StateRuleCollection`, `StrategyEditCollection`.
- **Value types (2):** `AtdlValueType`, `AtdlReferenceType`.
- **Validation:** `ControlValidationState`.
- **Fix:** `FixFieldValueProvider`.

#### Sub-decision — `ThrowHelper`

`ThrowHelper` is the one class using `LogError` (not `LogDebug`), at 12 sites,
logging each exception as it is created or rethrown. It is `static`, dead
(`NullLogger`), and **double-reports**: the exception is thrown and surfaces to
the caller regardless. Decision: **remove** the `_log.LogError` calls and the
`_log` field. A host that wants exception logging does it at its own catch
boundary (standard practice). Keeping exception logging inside `ThrowHelper`
would require a static ambient logger, which the project's conventions rule out.

### 3. `Clock_t` time source

- Replace `protected virtual DateTime GetCurrentTime() => DateTime.Now` with a
  settable property:
  `public TimeProvider TimeProvider { get; set; } = TimeProvider.System;`
- In `LoadDefaultFromInitValue` (the `InitValueMode == 1` branch), read
  `DateTime now = TimeProvider.GetLocalNow().DateTime;` — preserving the original
  local-time semantics (`DateTime.Now`).
- No constructor change: the property is set after reflective construction;
  production uses the `TimeProvider.System` default, tests assign a fake.
- `LocalMktTz` timezone resolution remains **out of scope** (documented future
  work) — this pass only replaces the time source.

### 4. Tests

Match the existing test project (`xunit.v3`, `AwesomeAssertions`, `NSubstitute`).

- **Logging wiring proof:** call `StrategiesReader.Load` with a supplied
  `ILoggerFactory` and assert debug records were produced. Because asserting
  `ILogger.Log<TState>` via NSubstitute is awkward, use a small in-memory
  recording `ILogger`/`ILoggerProvider` test double rather than a substitute.
- **Clock injection:** add the `Microsoft.Extensions.TimeProvider.Testing`
  package to the test project; use `FakeTimeProvider` to assert that `Clock_t`
  with `InitValueMode == 1` honours the injected time (e.g. a fake "now" after
  the `initValue` yields the fake now; a fake "now" before yields `initValue`).

### 5. Backward compatibility

All changes are additive: optional constructor parameters and a new settable
property. `new StrategiesReader()`, `new ElementFactory(def, type)`, and existing
`Clock_t` usage all continue to compile and behave identically when no
logger/clock is supplied. No breaking change to the `v0.1.0` public surface.

## Out of scope (future work)

- The model-service seam (injecting services into reflectively-constructed model
  objects) — would be the path to logging/clock inside controls/elements if ever
  needed.
- `Clock_t.LocalMktTz` timezone-aware comparison.
- Other deferred batch-3 items (`ThrowHelper` paramName threading G-D,
  `IParentable<T>.Parent` nullability G-G, `ConvertToComparableType` null-RHS
  semantics O-G2) — unrelated to this pass.

## Success criteria

- No `// TODO wire injected logger` markers remain in `src/`.
- A host passing an `ILoggerFactory` to `StrategiesReader` receives a
  deserialization trace; passing nothing keeps current (silent) behaviour.
- `Clock_t` reads time through an injectable `TimeProvider`; no direct
  `DateTime.Now` remains in it.
- Build clean (0 warnings); existing 23 tests plus the 2 new tests pass.
