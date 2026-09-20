# Logger & Clock DI Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire a real `ILoggerFactory` into the FIXatdl deserialization pipeline and give `Clock_t` an injectable `TimeProvider`, then delete the dead `NullLogger` tracing scaffolding from the ~34 reflectively-constructed model objects and static utilities.

**Architecture:** Constructor-inject `ILoggerFactory` into the only two composition-root services — `StrategiesReader` (public entry) and the `ElementFactory` it creates — so a host gets a full deserialization trace on demand. Every other logging class is reflectively constructed (or static) and cannot take injection under this scope, so its dead logger field and all `_log.*` calls are removed. `Clock_t` gains a settable `TimeProvider` property (default `TimeProvider.System`). All changes are additive — no break to the published v0.1.0 surface.

**Tech Stack:** .NET 10, `Microsoft.Extensions.Logging.Abstractions` (already referenced), xUnit v3 + AwesomeAssertions + NSubstitute, `Microsoft.Extensions.TimeProvider.Testing` (added in Task 2). Central Package Management via `Directory.Packages.props`.

**Spec:** `docs/superpowers/specs/2026-05-30-logger-clock-di-design.md`

> **Status (superseded — updated 2026-05-31):** this plan landed as written, but its `Clock_t`
> portion has since been superseded by batch-5 C1 (`7c8517e`). The `TimeProvider` seam below was
> replaced with a NodaTime `IClock`, and the "LocalMktTz timezone resolution is not yet applied"
> note in the Task-2 doc-comment is no longer accurate: `Clock_t` now resolves `localMktTz` to a
> real IANA zone and emits zone-shifted UTC. The only item that remains open — injecting
> `IClock`/zone-provider through the reflective `ElementFactory` instead of the current
> settable-property pattern — is **deliberately parked** (post-1.0, do-not-reopen): the factory has
> no service channel and DI would break the public ctors, while the dependency is already
> overridable and test-controllable. The logger-injection portion of this plan is unaffected.

---

## File Structure

**Modified (injection set):**
- `src/FixPortal.FixAtdl/Xml/StrategiesReader.cs` — add optional `ILoggerFactory` ctor; pass factory to `ElementFactory`.
- `src/FixPortal.FixAtdl/Xml/Serialization/ElementFactory.cs` — add optional `ILoggerFactory` ctor param; convert `static _log` → instance `ILogger<ElementFactory>`; drop `static` from 5 private helpers.

**Modified (Clock):**
- `src/FixPortal.FixAtdl/Model/Controls/Clock_t.cs` — replace `virtual GetCurrentTime()` with a `TimeProvider` property; prune its own dead logger.

**Modified (prune — 33 files, listed in Tasks 3–5).**

**Created (tests):**
- `tests/FixPortal.FixAtdl.Tests/TestDoubles/RecordingLoggerFactory.cs` — in-memory recording `ILoggerFactory`.
- `tests/FixPortal.FixAtdl.Tests/Diagnostics/LoggingWiringTests.cs` — asserts a supplied factory receives records from both categories.
- `tests/FixPortal.FixAtdl.Tests/Model/Controls/ClockTimeProviderTests.cs` — `FakeTimeProvider` drives `initValueMode==1`.

**Modified (test infra):**
- `Directory.Packages.props` — add `Microsoft.Extensions.TimeProvider.Testing` version.
- `tests/FixPortal.FixAtdl.Tests/FixPortal.FixAtdl.Tests.csproj` — reference it.

---

## Task 1: Wire ILoggerFactory into StrategiesReader + ElementFactory

**Files:**
- Create: `tests/FixPortal.FixAtdl.Tests/TestDoubles/RecordingLoggerFactory.cs`
- Create: `tests/FixPortal.FixAtdl.Tests/Diagnostics/LoggingWiringTests.cs`
- Modify: `src/FixPortal.FixAtdl/Xml/StrategiesReader.cs:26-27` (field), `:51` and `:80` ctor insertion, `:113` factory creation
- Modify: `src/FixPortal.FixAtdl/Xml/Serialization/ElementFactory.cs:25-26` (field), `:43-52` (ctor), and `static` on `:260 :279 :554 :594 :616`

- [ ] **Step 1: Write the recording logger test double**

Create `tests/FixPortal.FixAtdl.Tests/TestDoubles/RecordingLoggerFactory.cs`:

```csharp
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace FixPortal.FixAtdl.Tests.TestDoubles;

/// <summary>
/// An in-memory <see cref="ILoggerFactory"/> that records every log entry so tests can assert
/// the library wired logging through. Captures the logger category, level, and rendered message.
/// </summary>
public sealed class RecordingLoggerFactory : ILoggerFactory
{
    public sealed record Entry(string Category, LogLevel Level, string Message);

    public ConcurrentQueue<Entry> Records { get; } = new();

    public ILogger CreateLogger(string categoryName) => new RecordingLogger(categoryName, Records);

    public void AddProvider(ILoggerProvider provider) { }

    public void Dispose() { }

    private sealed class RecordingLogger(string category, ConcurrentQueue<Entry> sink) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
            => sink.Enqueue(new Entry(category, logLevel, formatter(state, exception)));
    }
}
```

- [ ] **Step 2: Write the failing wiring test**

Create `tests/FixPortal.FixAtdl.Tests/Diagnostics/LoggingWiringTests.cs`:

```csharp
using System.Text;
using FixPortal.FixAtdl.Tests.TestDoubles;
using FixPortal.FixAtdl.Xml;
using FixPortal.FixAtdl.Xml.Serialization;

namespace FixPortal.FixAtdl.Tests.Diagnostics;

/// <summary>
/// Proves that a host-supplied ILoggerFactory is threaded through the deserialization pipeline:
/// both StrategiesReader (load-level) and ElementFactory (per-object construction) must emit records.
/// </summary>
public class LoggingWiringTests
{
    [Fact]
    public async Task Load_with_supplied_factory_records_from_reader_and_factory()
    {
        var recorder = new RecordingLoggerFactory();
        var xml = await File.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        new StrategiesReader(recorder).Load(stream);

        var categories = recorder.Records.Select(r => r.Category).ToList();
        categories.Should().Contain(typeof(StrategiesReader).FullName);
        categories.Should().Contain(typeof(ElementFactory).FullName);
    }
}
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test tests/FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~LoggingWiringTests"`
Expected: FAIL — `StrategiesReader` has no constructor taking `ILoggerFactory` (compile error), and `ElementFactory` records nothing.

- [ ] **Step 4: Update StrategiesReader**

In `src/FixPortal.FixAtdl/Xml/StrategiesReader.cs`, replace the field (lines 26-27):

```csharp
    // FP Enhancement: 2026-05-23 — TODO wire injected logger when refactoring class to accept ILogger.
    private readonly NullLogger _log = NullLogger.Instance;
```

with:

```csharp
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<StrategiesReader> _log;

    /// <summary>
    /// Initializes a new <see cref="StrategiesReader"/>.
    /// </summary>
    /// <param name="loggerFactory">Optional logger factory. When null, no logging is produced
    /// (<see cref="NullLoggerFactory"/>). Supply one to trace loading and deserialization.</param>
    public StrategiesReader(ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _log = _loggerFactory.CreateLogger<StrategiesReader>();
    }
```

Then in `LoadStrategies` (line 113) change:

```csharp
        ElementFactory factory = new(SchemaDefinitions.Strategies_t, typeof(Strategy_t));
```

to:

```csharp
        ElementFactory factory = new(SchemaDefinitions.Strategies_t, typeof(Strategy_t), _loggerFactory);
```

(The existing `using Microsoft.Extensions.Logging;` and `using Microsoft.Extensions.Logging.Abstractions;` stay — both are now used.)

- [ ] **Step 5: Update ElementFactory**

In `src/FixPortal.FixAtdl/Xml/Serialization/ElementFactory.cs`, replace the field (lines 25-26):

```csharp
    // FP Enhancement: 2026-05-23 — TODO wire injected logger when refactoring class to accept ILogger.
    private static readonly NullLogger _log = NullLogger.Instance;
```

with:

```csharp
    private readonly ILogger<ElementFactory> _log;
```

Replace the constructor (lines 43-52) with one that takes the factory and assigns `_log` first:

```csharp
    /// <summary>
    /// Initializes a new <see cref="ElementFactory"/>.
    /// </summary>
    /// <param name="elementDefinition">The root element definition used for deserialization.</param>
    /// <param name="notifyCreationOfType">The type whose creation should raise <see cref="ClassDeserialized"/>.</param>
    /// <param name="loggerFactory">Optional logger factory; when null, no logging is produced.</param>
    public ElementFactory(ElementDefinition elementDefinition, Type notifyCreationOfType, ILoggerFactory? loggerFactory = null)
    {
        _log = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<ElementFactory>();

        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("ElementFactory created; root ElementName='{ElementName}'.", elementDefinition.ElementName);
        }

        _elementDefinition = elementDefinition;
        _notifyCreationOfType = notifyCreationOfType;
    }
```

Because `_log` is now an instance field, remove the `static` keyword from these five private helpers (their bodies are unchanged; they are only ever called from instance methods):

- Line 260: `private static object CreateRawObject(Type outerType, Type[] innerTypes, ...)` → `private object CreateRawObject(Type outerType, Type[] innerTypes, ...)`
- Line 279: `private static object CreateRawObject(Type targetType, Type[] argTypes, ...)` → `private object CreateRawObject(Type targetType, Type[] argTypes, ...)`
- Line 554: `private static object ReadAttribute(IEnumerable<XAttribute> attributes, XName attributeName, Type type)` → drop `static`
- Line 594: `private static object ReadAttribute(IEnumerable<XAttribute> attributes, XName attributeName, Type enumType, Dictionary<string, Enum> enumValues)` → drop `static`
- Line 616: `private static void SetPropertyValue(PropertyInfo property, object target, object value)` → drop `static`

(Keep `using Microsoft.Extensions.Logging;` and `using Microsoft.Extensions.Logging.Abstractions;` — both still used.)

- [ ] **Step 6: Run the test to verify it passes**

Run: `dotnet test tests/FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~LoggingWiringTests"`
Expected: PASS.

- [ ] **Step 7: Build to confirm no warnings**

Run: `dotnet build src/FixPortal.FixAtdl/FixPortal.FixAtdl.csproj`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 8: Commit**

```bash
git add tests/FixPortal.FixAtdl.Tests/TestDoubles/RecordingLoggerFactory.cs tests/FixPortal.FixAtdl.Tests/Diagnostics/LoggingWiringTests.cs src/FixPortal.FixAtdl/Xml/StrategiesReader.cs src/FixPortal.FixAtdl/Xml/Serialization/ElementFactory.cs
git commit -m "feat(logging): inject ILoggerFactory into StrategiesReader and ElementFactory"
```

---

## Task 2: Give Clock_t an injectable TimeProvider

**Files:**
- Modify: `Directory.Packages.props` (Test ItemGroup)
- Modify: `tests/FixPortal.FixAtdl.Tests/FixPortal.FixAtdl.Tests.csproj` (PackageReference ItemGroup)
- Create: `tests/FixPortal.FixAtdl.Tests/Model/Controls/ClockTimeProviderTests.cs`
- Modify: `src/FixPortal.FixAtdl/Model/Controls/Clock_t.cs` (field 26-27, ctor 35-42, `LoadDefaultFromInitValue` 90-101, `GetCurrentTime` 104-111)

- [ ] **Step 1: Add the FakeTimeProvider package version**

In `Directory.Packages.props`, inside the `<ItemGroup Label="Test">`, add:

```xml
    <PackageVersion Include="Microsoft.Extensions.TimeProvider.Testing" Version="9.10.0" />
```

(If restore reports that exact version is unavailable, use the latest 9.x `Microsoft.Extensions.TimeProvider.Testing` listed by `dotnet package search Microsoft.Extensions.TimeProvider.Testing`.)

- [ ] **Step 2: Reference the package in the test project**

In `tests/FixPortal.FixAtdl.Tests/FixPortal.FixAtdl.Tests.csproj`, inside the existing `<ItemGroup>` of `PackageReference`s, add:

```xml
    <PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" />
```

- [ ] **Step 3: Write the failing Clock test**

Create `tests/FixPortal.FixAtdl.Tests/Model/Controls/ClockTimeProviderTests.cs`:

```csharp
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Controls;
using Microsoft.Extensions.Time.Testing;

namespace FixPortal.FixAtdl.Tests.Model.Controls;

/// <summary>
/// initValueMode==1 means "use the current time if the initValue time has already passed".
/// These tests pin "now" via an injected TimeProvider so the branch is deterministic.
/// </summary>
public class ClockTimeProviderTests
{
    [Theory]
    [InlineData(13, 13)] // now (13:00) is after initValue (12:00) -> control takes now
    [InlineData(11, 12)] // now (11:00) is before initValue (12:00) -> control keeps initValue
    public void InitValueMode1_uses_injected_TimeProvider(int nowHour, int expectedHour)
    {
        var fake = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, nowHour, 0, 0, TimeSpan.Zero));
        var clock = new Clock_t("clk")
        {
            InitValue = new DateTime(2026, 1, 1, 12, 0, 0),
            InitValueMode = 1,
            TimeProvider = fake,
        };

        clock.LoadInitValue(FixFieldValueProvider.Empty);

        var value = (DateTime?)clock.GetCurrentValue();
        value.Should().Be(new DateTime(2026, 1, 1, expectedHour, 0, 0));
    }
}
```

- [ ] **Step 4: Run the test to verify it fails**

Run: `dotnet test tests/FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~ClockTimeProviderTests"`
Expected: FAIL — `Clock_t` has no `TimeProvider` property (compile error).

- [ ] **Step 5: Update Clock_t — prune its logger and add TimeProvider**

In `src/FixPortal.FixAtdl/Model/Controls/Clock_t.cs`:

Remove the dead logger field (lines 26-27):

```csharp
    // FP Enhancement: 2026-05-23 — TODO wire injected logger when refactoring class to accept ILogger.
    private static readonly NullLogger _log = NullLogger.Instance;
```

Simplify the constructor (lines 35-42) — its only body was a trace block:

```csharp
    public Clock_t(string id)
        : base(id)
    {
    }
```

Remove the debug block inside `SetValueFromParameter` (lines 127-130) so it ends at the assignment:

```csharp
    public override void SetValueFromParameter(IParameter parameter)
    {
        IControlConvertible value = parameter.GetValueForControl();

        _value = value.ToDateTime();
    }
```

Replace the `GetCurrentTime()` seam (lines 104-111) with a settable `TimeProvider` property:

```csharp
    /// <summary>
    /// The time source used when <see cref="InitValueMode"/> == 1. Defaults to the system clock;
    /// assign a fake in tests. (LocalMktTz timezone resolution is not yet applied — see remarks on
    /// LoadDefaultFromInitValue; both values are still compared in the host's local representation.)
    /// </summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;
```

In `LoadDefaultFromInitValue` (the `InitValueMode == 1` branch, lines 90-97), read from the provider:

```csharp
        if (InitValueMode == 1)
        {
            // initValueMode 1: use the current time if the initValue time has already passed. Snapshot
            // "now" ONCE from the injected TimeProvider (the original read DateTime.Now twice, risking a
            // sub-tick inconsistency, and was untestable).
            DateTime now = TimeProvider.GetLocalNow().DateTime;

            _value = now > InitValue.Value ? now : InitValue;
        }
```

Remove the now-unused `using Microsoft.Extensions.Logging;` and `using Microsoft.Extensions.Logging.Abstractions;` (lines 16-17).

- [ ] **Step 6: Run the test to verify it passes**

Run: `dotnet test tests/FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~ClockTimeProviderTests"`
Expected: PASS (both theory rows).

- [ ] **Step 7: Build to confirm no warnings**

Run: `dotnet build src/FixPortal.FixAtdl/FixPortal.FixAtdl.csproj`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 8: Commit**

```bash
git add Directory.Packages.props tests/FixPortal.FixAtdl.Tests/FixPortal.FixAtdl.Tests.csproj tests/FixPortal.FixAtdl.Tests/Model/Controls/ClockTimeProviderTests.cs src/FixPortal.FixAtdl/Model/Controls/Clock_t.cs
git commit -m "feat(clock): inject TimeProvider into Clock_t; replace virtual GetCurrentTime seam"
```

---

## Prune pattern (applies to Tasks 3–5)

For each file below, make these edits, then rely on the existing test suite as the regression guard (these classes have no behaviour beyond the dead logging):

1. Delete the comment line `// FP Enhancement: 2026-05-23 — TODO wire injected logger when refactoring class to accept ILogger.`
2. Delete the logger field — one of:
   - `private static readonly NullLogger _log = NullLogger.Instance;`
   - `private readonly NullLogger _log = NullLogger.Instance;`
   - `private static readonly ILogger _log = NullLogger.Instance;` (AtdlValueType, ThrowHelper)
3. Delete every `_log.*` call site:
   - **Guarded debug blocks** — remove the whole block:
     ```csharp
     if (_log.IsEnabled(LogLevel.Debug))
     {
         _log.LogDebug(...);
     }
     ```
     If that block was the entire body of a method/constructor, leave an empty body (`{ }`).
   - **Unguarded calls** — remove the single line `_log.LogWarning(...);` / `_log.LogError(...);` (these sit in `catch`/guard blocks that already `return false` or fall through; removing only the log line preserves control flow).
4. Remove the now-unused logging usings `using Microsoft.Extensions.Logging;` and `using Microsoft.Extensions.Logging.Abstractions;` (leave any other usings).

Worked example — instance-field + guarded block (`StateRuleCollection`-style):

```csharp
// before
    // FP Enhancement: 2026-05-23 — TODO wire injected logger when refactoring class to accept ILogger.
    private readonly NullLogger _log = NullLogger.Instance;
    ...
        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("Some message {Arg0}", x);
        }
// after: both removed entirely
```

Worked example — unguarded `LogError` in a swallow path (`NumericControlBase`):

```csharp
// before
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            _log.LogError("Unable to set control {Arg0} to value '{Arg1}' ...", Id, value);

            return false;
        }
// after
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            return false;
        }
```

> The unguarded `LogWarning`/`LogError` sites are: `InitializableControl` (2× `LogWarning`), `BinaryControlBase`, `NumericControlBase`, `ListControlBase`, `AtdlValueType`, `AtdlReferenceType` (1× `LogError` each). Removing them is behaviour-preserving (all are `NullLogger` today). Watch the `catch (… ex …)` blocks: if removing the log leaves `ex` unused and the analyzer flags it (e.g. `when (ex is …)` still uses `ex`, but a bare `catch (FooException ex)` would not), change to `catch (FooException)`.

---

## Task 3: Prune ThrowHelper

**Files:**
- Modify: `src/FixPortal.FixAtdl/Diagnostics/ThrowHelper.cs`

- [ ] **Step 1: Remove the logger and its 12 call sites**

In `src/FixPortal.FixAtdl/Diagnostics/ThrowHelper.cs`:
- Delete the comment (line 21) and field (line 22) `private static readonly ILogger _log = NullLogger.Instance;`.
- Delete all 12 `_log.LogError(...)` lines (at 41, 58, 76, 94, 111, 129, 147, 166, 191, 209, 232). Each sits between the `CreateException`/`BuildRethrown` assignment and the `return`, e.g.:

  ```csharp
  // before
      T ex = CreateException<T>(source, message, null);

      _log.LogError(ex, "Exception created by ThrowHelper");

      return ex;
  // after
      T ex = CreateException<T>(source, message, null);

      return ex;
  ```

- Remove `using Microsoft.Extensions.Logging;` (line 11) and `using Microsoft.Extensions.Logging.Abstractions;` (line 12). Keep `using System.Globalization;`, `using System.Reflection;`, and `using FixPortal.FixAtdl.Diagnostics.Exceptions;`.

- [ ] **Step 2: Build**

Run: `dotnet build src/FixPortal.FixAtdl/FixPortal.FixAtdl.csproj`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 3: Run the full test suite**

Run: `dotnet test tests/FixPortal.FixAtdl.Tests`
Expected: PASS, 0 failed (the original 23 cases plus 3 new cases — 1 from Task 1, 2 theory rows from Task 2 — = 26).

- [ ] **Step 4: Commit**

```bash
git add src/FixPortal.FixAtdl/Diagnostics/ThrowHelper.cs
git commit -m "refactor(diagnostics): drop dead exception logging from ThrowHelper"
```

---

## Task 4: Prune controls and Support base classes (20 files)

**Files (apply the Prune pattern to each):**

Controls (14 — Clock_t was handled in Task 2):
- `src/FixPortal.FixAtdl/Model/Controls/CheckBox_t.cs`
- `src/FixPortal.FixAtdl/Model/Controls/CheckBoxList_t.cs`
- `src/FixPortal.FixAtdl/Model/Controls/DoubleSpinner_t.cs`
- `src/FixPortal.FixAtdl/Model/Controls/DropDownList_t.cs`
- `src/FixPortal.FixAtdl/Model/Controls/EditableDropDownList_t.cs`
- `src/FixPortal.FixAtdl/Model/Controls/HiddenField_t.cs`
- `src/FixPortal.FixAtdl/Model/Controls/Label_t.cs`
- `src/FixPortal.FixAtdl/Model/Controls/MultiSelectList_t.cs`
- `src/FixPortal.FixAtdl/Model/Controls/RadioButton_t.cs`
- `src/FixPortal.FixAtdl/Model/Controls/RadioButtonList_t.cs`
- `src/FixPortal.FixAtdl/Model/Controls/SingleSelectList_t.cs`
- `src/FixPortal.FixAtdl/Model/Controls/SingleSpinner_t.cs`
- `src/FixPortal.FixAtdl/Model/Controls/Slider_t.cs`
- `src/FixPortal.FixAtdl/Model/Controls/TextField_t.cs`

Support bases (6):
- `src/FixPortal.FixAtdl/Model/Controls/Support/BinaryControlBase.cs` (incl. 1× `LogError`)
- `src/FixPortal.FixAtdl/Model/Controls/Support/EnumState.cs`
- `src/FixPortal.FixAtdl/Model/Controls/Support/InitializableControl.cs` (incl. 2× `LogWarning`)
- `src/FixPortal.FixAtdl/Model/Controls/Support/ListControlBase.cs` (incl. 1× `LogError`)
- `src/FixPortal.FixAtdl/Model/Controls/Support/NumericControlBase.cs` (incl. 1× `LogError`)
- `src/FixPortal.FixAtdl/Model/Controls/Support/TextControlBase.cs`

- [ ] **Step 1: Apply the Prune pattern to all 20 files**

Follow the **Prune pattern** above for each. Note the four files flagged with `LogError`/`LogWarning` — remove those unguarded lines too, preserving the surrounding `return`/control flow.

- [ ] **Step 2: Build**

Run: `dotnet build src/FixPortal.FixAtdl/FixPortal.FixAtdl.csproj`
Expected: Build succeeded, 0 warnings. (If an analyzer flags an unused `catch (X ex)` after a log removal, change it to `catch (X)`.)

- [ ] **Step 3: Run the full test suite**

Run: `dotnet test tests/FixPortal.FixAtdl.Tests`
Expected: PASS, 0 failed (26 cases).

- [ ] **Step 4: Commit**

```bash
git add src/FixPortal.FixAtdl/Model/Controls
git commit -m "refactor(controls): remove dead NullLogger tracing from controls and base classes"
```

---

## Task 5: Prune elements, collections, value types, validation, FIX (11 files)

**Files (apply the Prune pattern to each):**

Elements (3):
- `src/FixPortal.FixAtdl/Model/Elements/Edit_t.cs`
- `src/FixPortal.FixAtdl/Model/Elements/EditRef_t.cs`
- `src/FixPortal.FixAtdl/Model/Elements/Parameter_t.cs`

Evaluation base (1):
- `src/FixPortal.FixAtdl/Validation/EditEvaluator.cs`

Collections (4):
- `src/FixPortal.FixAtdl/Model/Collections/EditEvaluatingCollection.cs`
- `src/FixPortal.FixAtdl/Model/Collections/ReadOnlyControlCollection.cs`
- `src/FixPortal.FixAtdl/Model/Collections/StateRuleCollection.cs`
- `src/FixPortal.FixAtdl/Model/Collections/StrategyEditCollection.cs`

Value types (2 — both incl. 1× `LogError`):
- `src/FixPortal.FixAtdl/Model/Types/Support/AtdlValueType.cs`
- `src/FixPortal.FixAtdl/Model/Types/Support/AtdlReferenceType.cs`

Validation + FIX (2):
- `src/FixPortal.FixAtdl/Validation/ControlValidationState.cs`
- `src/FixPortal.FixAtdl/Fix/FixFieldValueProvider.cs`

- [ ] **Step 1: Apply the Prune pattern to all 11 files**

Follow the **Prune pattern**. `AtdlValueType` / `AtdlReferenceType` carry a `LogError` in a conversion-failure path — remove the unguarded line, keep the surrounding throw/return. `AtdlValueType` uses `private static readonly ILogger _log` (not `NullLogger`); remove it the same way.

- [ ] **Step 2: Build**

Run: `dotnet build src/FixPortal.FixAtdl/FixPortal.FixAtdl.csproj`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 3: Run the full test suite**

Run: `dotnet test tests/FixPortal.FixAtdl.Tests`
Expected: PASS, 0 failed (26 cases).

- [ ] **Step 4: Commit**

```bash
git add src/FixPortal.FixAtdl/Model/Elements src/FixPortal.FixAtdl/Model/Collections src/FixPortal.FixAtdl/Model/Types src/FixPortal.FixAtdl/Validation src/FixPortal.FixAtdl/Fix
git commit -m "refactor(model): remove dead NullLogger tracing from elements, collections, types, validation"
```

---

## Task 6: Verification sweep

**Files:** none (verification only)

- [ ] **Step 1: Confirm no residual logger scaffolding remains**

Run: `git grep -n "TODO wire injected logger"`
Expected: no output.

Run: `git grep -n "NullLogger.Instance"`
Expected: no output (the only remaining `NullLogger*` references are `NullLoggerFactory.Instance` in `StrategiesReader` and `ElementFactory`).

Run: `git grep -n "NullLogger" -- "src/*.cs"`
Expected: exactly two matches — `StrategiesReader.cs` and `ElementFactory.cs` (both `NullLoggerFactory.Instance`).

- [ ] **Step 2: Full clean build, all projects**

Run: `dotnet build FixPortal.FixAtdl.sln`
Expected: Build succeeded, 0 warnings.

(If the solution file has a different name, discover it with `dotnet build` from the repo root — there is a single `.sln`.)

- [ ] **Step 3: Full test run**

Run: `dotnet test`
Expected: PASS, 0 failed (26 cases).

- [ ] **Step 4: Confirm public-surface compatibility**

Run: `git grep -n "new StrategiesReader()" -- "tests/*.cs"`
Expected: the existing parameterless call sites still present and compiling (proves the optional-ctor change is non-breaking).

- [ ] **Step 5: Final commit (only if Step 1–4 surfaced fixes)**

```bash
git add -A
git commit -m "chore(logging): verification sweep — confirm no dead logger scaffolding remains"
```

---

## Self-Review notes

- **Spec coverage:** §1 injection set → Task 1; §2 prune set → Tasks 3–5 (ThrowHelper §2 sub-decision → Task 3; the `LogWarning`/`LogError` note → called out in the Prune pattern and Tasks 4–5); §3 Clock_t → Task 2; §4 tests → Tasks 1 (wiring) & 2 (Clock); §5 compatibility → Task 6 Step 4. Success criteria (no TODO markers, host gets trace, no `DateTime.Now` in Clock_t, 0 warnings, tests pass) → Task 6.
- **File count:** prune set = ThrowHelper (Task 3) + 20 (Task 4) + 11 (Task 5) = 32 files, plus Clock_t pruned in Task 2 = 33 dead-logger files, plus the 2 injection-set files re-wired = the full 35 originally bearing a logger reference. (StateRule_t / StrategyEdit_t carry no own logger — covered by pruning their base EditEvaluator.)
- **Type consistency:** `RecordingLoggerFactory.Entry(Category, Level, Message)` used consistently; `TimeProvider` property name matches between Clock_t and the test; `ILoggerFactory` ctor parameter is optional in both `StrategiesReader` and `ElementFactory`.
