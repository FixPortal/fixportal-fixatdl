# Batch 5 — Phase C — Parse-Fidelity Conformance + Sonar Clearance — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Land the remaining batch-5 conformance fixes (H3, H4, M4, C2), the two Phase-A follow-ups, and clear all 24 SonarAnalyzer findings so `TreatWarningsAsErrors` can be turned on.

**Architecture:** `FixPortal.FixAtdl` is a reflective, definition-driven FIXatdl 1.1 reader/model (`ElementFactory` + `SchemaDefinitions` + `ValueConverter`). Conformance fixes are surgical and confined; the Sonar clearance is behaviour-preserving. The library is a reader/model, not an XML serialiser — tests parse inline XML or construct objects directly and assert behaviour. NodaTime is confined to the `Clock_t` seam (added in Phase A).

**Tech Stack:** .NET 10, C# latest, NodaTime 3.x, xUnit v3, AwesomeAssertions (`using AwesomeAssertions;`, `.Should()`, never `Assert.*`), NSubstitute, NodaTime.Testing `FakeClock`.

**Branch / worktree:** `reviewer-findings-batch7` in `D:\FixPortal\fixportal-fixatdl\.claude\worktrees\reviewer-passes` (already created from `origin/main` @ `edb36be`).

**Standing constraints:**
- IDE0045/IDE0017/IDE0047 are enforced as **errors** — no implicit-else ternary violations, use object initializers, no redundant parens in patterns. The implementer must keep the build clean.
- `ImplicitUsings` is on.
- Run `git add <paths>` and `git commit` as **separate** tool calls (never `&&`).
- The full test suite is **612 green, 23 Sonar-warning baseline** at the start of this branch. "0 new warnings" = no new compiler/analyzer warnings on touched code.
- Do **not** add broker-spec fixtures here (that is Phase D — real client data, needs obfuscation). All Phase-C tests use inline XML or direct construction.

---

## File Structure

**Conformance + follow-ups:**
- `src/FixPortal.FixAtdl/Model/Elements/EnumPair_t.cs` — add `int? Index` (H3).
- `src/FixPortal.FixAtdl/Xml/SchemaDefinitions.cs` — map `EnumPair@index` (H3); reroute timestamp/time-only `maxValue`/`minValue` to time-only-aware raw setters (C2).
- `src/FixPortal.FixAtdl/Model/Elements/Parameter_t.cs` — document the `DefinedByFix` inert contract (H4).
- `src/FixPortal.FixAtdl/Model/Types/Float_t.cs` — make `Round` static + document rounding mode (M4 + S2325).
- `src/FixPortal.FixAtdl/Model/Types/Support/DateTimeTypeBase.cs` — time-only bound capture + time-of-day comparison (C2).
- `src/FixPortal.FixAtdl/Fix/FixDateTime.cs` — add `AdjustToUniversal` (follow-up 1).
- `src/FixPortal.FixAtdl/Model/Controls/Clock_t.cs` — sub-second `ToString` (follow-up 2).

**Sonar (behaviour-preserving):** `ThrowHelper.cs`, `ISimpleDictionary.cs`, `ReadOnlyControlCollection.cs`, `StateRule_t.cs`, `Clock_t.cs`, `BinaryControlBase.cs`, `NumericControlBase.cs`, `EnumState.cs`, `Control_t.cs`, `Edit_t.cs`, `Percentage_t.cs`, `MonthYear.cs`, `ModelUtils.cs`, `ControlValidationState.cs`, `ElementFactory.cs`, `ValueConverter.cs`, `EditEvaluatingCollection.cs`.

**Build config:** `Directory.Build.props` — flip `TreatWarningsAsErrors` to `true` (final task).

**Tests:**
- `tests/FixPortal.FixAtdl.Tests/Validation/EditConformanceTests.cs` — exists (Phase B); C2 may add a sibling file `TimestampBoundConformanceTests.cs`.
- `tests/FixPortal.FixAtdl.Tests/Xml/EnumPairDeserializationTests.cs` — new (H3).
- `tests/FixPortal.FixAtdl.Tests/Controls/ClockControlTests.cs` / `ControlValueTests.cs` — extend (follow-ups).

---

## Task 1: H3 — Capture `EnumPair@index`

**Files:**
- Modify: `src/FixPortal.FixAtdl/Model/Elements/EnumPair_t.cs`
- Modify: `src/FixPortal.FixAtdl/Xml/SchemaDefinitions.cs:220-226`
- Test: `tests/FixPortal.FixAtdl.Tests/Xml/EnumPairDeserializationTests.cs` (new)

**Context:** `EnumPair@index` (broker-82, 128×) is a vendor ordering-hint extension. Design §3.1 decided: capture it as `int? Index` for lossless fidelity; it does not affect wire output. `index` is **optional** (absence is fine — standard FIXatdl 1.1 EnumPair is `enumID`+`wireValue` only).

- [ ] **Step 1: Write the failing test**

Create `tests/FixPortal.FixAtdl.Tests/Xml/EnumPairDeserializationTests.cs`. Mirror the existing inline-XML deserialization test pattern (see `tests/FixPortal.FixAtdl.Tests/Xml/ClockDeserializationTests.cs` for the `Load` helper shape and namespace declarations).

```csharp
using AwesomeAssertions;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Xml;

namespace FixPortal.FixAtdl.Tests.Xml;

public class EnumPairDeserializationTests
{
    private const string EnumPairStrategyXml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Strategies xmlns="http://www.fixprotocol.org/FIXatdl-1-1/Core"
                    xmlns:lay="http://www.fixprotocol.org/FIXatdl-1-1/Layout"
                    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                    strategyIdentifierTag="5001">
          <Strategy name="EnumTest" version="1" wireValue="EnumTest" uiRep="EnumTest" providerID="DEMO">
            <Parameter name="Side" xsi:type="Char_t" fixTag="54" use="required">
              <EnumPair enumID="o1" index="0" wireValue="1" />
              <EnumPair enumID="o2" index="1" wireValue="2" />
              <EnumPair enumID="o3" wireValue="3" />
            </Parameter>
            <lay:StrategyLayout>
              <lay:StrategyPanel title="P" orientation="VERTICAL" collapsible="false" border="Line" />
            </lay:StrategyLayout>
          </Strategy>
        </Strategies>
        """;

    private static Strategies_t Load(string xml)
    {
        using var reader = new StringReader(xml);
        return new FixatdlReader().Load(reader);
    }

    [Fact]
    public void EnumPair_index_is_captured_when_present()
    {
        var strategies = Load(EnumPairStrategyXml);
        var enumPairs = strategies.Strategies[0].Parameters[0].EnumPairs;

        enumPairs["o1"].Index.Should().Be(0);
        enumPairs["o2"].Index.Should().Be(1);
    }

    [Fact]
    public void EnumPair_index_is_null_when_absent()
    {
        var strategies = Load(EnumPairStrategyXml);
        var enumPairs = strategies.Strategies[0].Parameters[0].EnumPairs;

        enumPairs["o3"].Index.Should().BeNull();
    }

    [Fact]
    public void EnumPair_index_does_not_affect_wire_value()
    {
        var strategies = Load(EnumPairStrategyXml);
        var enumPairs = strategies.Strategies[0].Parameters[0].EnumPairs;

        enumPairs["o1"].WireValue.Should().Be("1");
    }
}
```

> **Implementer note:** Confirm the exact loader API (`FixatdlReader().Load(...)`, `Strategies_t`, `Parameters[0].EnumPairs`, indexer access) against existing tests in `tests/FixPortal.FixAtdl.Tests/Xml/`. Adjust the `Load` helper / type names to match the established pattern in that folder — do **not** invent an API. If the existing deserialization tests use a different entry point, copy theirs verbatim.

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~EnumPairDeserializationTests"`
Expected: FAIL — `EnumPair_t` has no `Index` property (compile error), or `index` is dropped.

- [ ] **Step 3: Add the `Index` property to `EnumPair_t`**

In `src/FixPortal.FixAtdl/Model/Elements/EnumPair_t.cs`, after the `WireValue` property:

```csharp
    /// <summary>
    /// Optional vendor extension attribute: an integer ordering hint for this enum pair. Standard
    /// FIXatdl 1.1 <c>EnumPair</c> carries only <c>enumID</c> and <c>wireValue</c>; <c>index</c> is a
    /// captured extension for lossless fidelity and does NOT affect the FIX wire value. Null when the
    /// source ATDL did not supply an <c>index</c>.
    /// </summary>
    public int? Index { get; set; }
```

- [ ] **Step 4: Map the attribute in the schema**

In `src/FixPortal.FixAtdl/Xml/SchemaDefinitions.cs`, in the `EnumPairs` definition (lines 220-226), add the optional `index` attribute alongside `enumID`/`wireValue`:

```csharp
    private static readonly ChildElementDefinition EnumPairs = new(
        new ElementDefinition(AtdlNamespaces.core + "EnumPair", typeof(Model.Elements.EnumPair_t),
            [
                new ElementAttribute("enumID", "EnumId", typeof(string), Required.Mandatory),
                new ElementAttribute("wireValue", "WireValue", typeof(string), Required.Mandatory),
                new ElementAttribute("index", "Index", typeof(int), Required.Optional)
            ]),
            "EnumPairs", typeof(Model.Collections.EnumPairCollection), StandardContainerMethod.Add);
```

(Mirrors the `typeof(int), Required.Optional` pattern used for `precision` at line 138.)

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test tests/FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~EnumPairDeserializationTests"`
Expected: PASS (3/3).

- [ ] **Step 6: Commit**

```
git add src/FixPortal.FixAtdl/Model/Elements/EnumPair_t.cs src/FixPortal.FixAtdl/Xml/SchemaDefinitions.cs tests/FixPortal.FixAtdl.Tests/Xml/EnumPairDeserializationTests.cs
git commit -m "feat(model): capture EnumPair@index as int? Index (batch 5, H3)"
```

---

## Task 2: H4 — Document the `definedByFIX` inert contract

**Files:**
- Modify: `src/FixPortal.FixAtdl/Model/Elements/Parameter_t.cs:65-67`
- Test: none (documentation-only; no behaviour change).

**Context:** Design §3.2 decided: `definedByFIX="true"` marks a parameter as a redefinition of a standard FIX tag. It does **not** change wire output. Keep it informational/inert but make that a *deliberate, documented* contract — not an accidental dead field. Do **not** invent a validation gate.

- [ ] **Step 1: Replace the `DefinedByFix` doc comment**

In `src/FixPortal.FixAtdl/Model/Elements/Parameter_t.cs`, replace the existing summary on the `DefinedByFix` property (currently lines 65-67):

```csharp
    /// <summary>
    /// Indicates whether this parameter is a redefinition of a standard FIX tag (FIXatdl
    /// <c>definedByFIX</c>). The default value is false.
    /// </summary>
    /// <remarks>
    /// This is a captured, <b>deliberately inert</b> informational flag. Per FIXatdl 1.1 it signals that
    /// the wire field already carries FIX-defined semantics/enumerations, but it does NOT alter this
    /// library's wire output and is NOT used as a validation gate: parameter values are validated and
    /// formatted identically whether or not this flag is set. It is surfaced so consumers that need the
    /// distinction can read it; the library applies no behaviour to it by design (batch 5, H4).
    /// </remarks>
    public bool? DefinedByFix { get; set; }
```

- [ ] **Step 2: Verify the build is clean**

Run: `dotnet build src/FixPortal.FixAtdl`
Expected: builds with no new warnings (documentation-only change; `GenerateDocumentationFile` is on, so malformed XML doc would warn — confirm none).

- [ ] **Step 3: Commit**

```
git add src/FixPortal.FixAtdl/Model/Elements/Parameter_t.cs
git commit -m "docs(model): make Parameter_t.DefinedByFix a documented-inert contract (batch 5, H4)"
```

---

## Task 3: M4 + S2325 — `Float_t.Round` static + rounding-mode documentation

**Files:**
- Modify: `src/FixPortal.FixAtdl/Model/Types/Float_t.cs:160-169` (and call sites at 111, 141)
- Test: `tests/FixPortal.FixAtdl.Tests/Model/Types/` — add a rounding test if none exists.

**Context:** M4 (design §3.3): confirm `MidpointRounding.AwayFromZero` is the intended convention and document it. S2325 (Sonar): `Round` does not use instance state and should be static — verified safe (no subclass overrides it; only internal `this.Round()` calls at lines 111, 141; `Percentage_t` calls the inherited method). Folding both: make `Round` static and document the rounding mode.

- [ ] **Step 1: Write the failing test (pin the rounding convention)**

Add to (or create) `tests/FixPortal.FixAtdl.Tests/Model/Types/FloatRoundingTests.cs`. First check whether a Float precision test already exists in the test tree (`grep` for `Precision` under `tests/`); if so, add the theory there instead of a new file.

```csharp
using System.Globalization;
using AwesomeAssertions;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Types;

namespace FixPortal.FixAtdl.Tests.Model.Types;

public class FloatRoundingTests
{
    // Pins MidpointRounding.AwayFromZero: 2.5 -> 3 (not banker's 2), -2.5 -> -3, 2.345 @2dp -> 2.35.
    [Theory]
    [InlineData("2.5", 0, "3")]
    [InlineData("-2.5", 0, "-3")]
    [InlineData("2.345", 2, "2.35")]
    [InlineData("2.344", 2, "2.34")]
    public void Precision_rounds_away_from_zero(string input, int precision, string expected)
    {
        var param = new Parameter_t<Float_t>("p") { /* default Use=Optional */ };
        param.Value.Precision = precision;
        param.WireValue = input;

        param.WireValue.Should().Be(expected);
    }
}
```

> **Implementer note:** Confirm the construction/round-trip API (`Parameter_t<Float_t>`, `param.Value.Precision`, setting/getting `WireValue`) against existing `Float_t`/parameter tests. The intent: set a value with a precision and assert the wire value is rounded away-from-zero. Adapt to the established test idiom if it differs — the assertion (away-from-zero at the midpoint) is the fixed requirement.

- [ ] **Step 2: Run the test to verify it passes (documents current behaviour)**

Run: `dotnet test tests/FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~FloatRoundingTests"`
Expected: PASS — current code already rounds away-from-zero. (This is a characterization test that locks the M4 convention before the S2325 refactor.)

- [ ] **Step 3: Make `Round` static and document the convention**

In `src/FixPortal.FixAtdl/Model/Types/Float_t.cs`, replace the `Round` method (lines 160-169):

```csharp
    /// <summary>
    /// Rounds the supplied value to the specified number of decimal places.
    /// </summary>
    /// <param name="value">Value to be rounded. May be null.</param>
    /// <param name="precision">Number of places to round to.</param>
    /// <returns>If the supplied value is non-null, the rounded value is returned; otherwise returns null.</returns>
    /// <remarks>FIXatdl does not mandate a rounding mode for the <c>precision</c> attribute. This library
    /// uses <see cref="MidpointRounding.AwayFromZero"/> by convention (e.g. 2.5 → 3, -2.5 → -3); trailing
    /// zeros are not padded, which is wire-legal for float fields (batch 5, M4).</remarks>
    protected static decimal? Round(decimal? value, int precision)
    {
        return value != null ? Math.Round((decimal)value, precision, MidpointRounding.AwayFromZero) : null;
    }
```

The call sites at lines 111 (`Round(value, Precision.Value)`) and 141 (`Round(value, (int)Precision)`) need no change — static methods are callable unqualified from instance methods.

- [ ] **Step 4: Run the test + build to verify green and warning-free**

Run: `dotnet build src/FixPortal.FixAtdl` (expect no S2325 on `Round`)
Run: `dotnet test tests/FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~FloatRoundingTests"` (expect PASS)

- [ ] **Step 5: Commit**

```
git add src/FixPortal.FixAtdl/Model/Types/Float_t.cs tests/FixPortal.FixAtdl.Tests/Model/Types/FloatRoundingTests.cs
git commit -m "refactor(types): make Float_t.Round static, document AwayFromZero convention (batch 5, M4/S2325)"
```

---

## Task 4: C2 — Time-only bounds on timestamp/time-only types compare on time-of-day

**Files:**
- Modify: `src/FixPortal.FixAtdl/Model/Types/Support/DateTimeTypeBase.cs`
- Modify: `src/FixPortal.FixAtdl/Xml/SchemaDefinitions.cs:205-210, 212-218`
- Test: `tests/FixPortal.FixAtdl.Tests/Validation/TimestampBoundConformanceTests.cs` (new)

**Context:** A `UTCTimestamp_t`/`TZTimestamp_t` parameter can carry a *time-only* bound (`maxValue="23:59:59"`, broker-431 `p_EndTime`). The `index`→`Value.MaxValue` mapping is `typeof(DateTime)`, so `ValueConverter` → `FixDateTime.TryParse` injects the parse-day's date into a time-only string. The stored `MaxValue` becomes `<parse-day>T23:59:59`, and range validation compares a full timestamp against a date-contaminated bound — non-deterministic across calendar days.

**Decided approach (time-of-day comparison):** route the raw `maxValue`/`minValue` text for the timestamp + time-only type families through a time-only-aware setter. When the bound is written as a bare time-of-day, store a `TimeOnly` bound and compare ONLY the time component; a full datetime is stored as `MaxValue`/`MinValue` exactly as before. Date-only (`UTCDateOnly_t`, `LocalMktDate_t`) and `MonthYear` bounds are unaffected (they share the same raw setter but never match a time-only pattern, so they parse to `MaxValue`/`MinValue` as before).

The two affected schema definitions are `TZTimeOnlyDefinition` (lines 205-210; used by TZTimeOnly_t, TZTimestamp_t, UTCDateOnly_t, UTCTimeOnly_t) and `UTCTimestampDefinition` (lines 212-218; UTCTimestamp_t). `LocalMktDateDefinition` (159-164) and `MonthYearDefinition` (166-171) are left alone. `constValue` is **not** rerouted (time-only `constValue` is dropped to time-of-day by the type's own time-only format on output, so it is not date-contaminated; out of scope per the finding).

- [ ] **Step 1: Write the failing test**

Create `tests/FixPortal.FixAtdl.Tests/Validation/TimestampBoundConformanceTests.cs`. The observable behaviour: a time-only `minValue`/`maxValue` constrains the time-of-day of the value, independent of the value's date.

```csharp
using System.Globalization;
using AwesomeAssertions;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Types;
using FixPortal.FixAtdl.Validation;

namespace FixPortal.FixAtdl.Tests.Validation;

public class TimestampBoundConformanceTests
{
    private static Parameter_t<UTCTimestamp_t> Param(string? minText, string? maxText)
    {
        var p = new Parameter_t<UTCTimestamp_t>("ts");
        if (minText != null) { p.Value.MinValueText = minText; }
        if (maxText != null) { p.Value.MaxValueText = maxText; }
        return p;
    }

    // A time-only minValue="08:00:00" constrains the UTC time-of-day, regardless of the value's date.
    [Theory]
    [InlineData("20260101-09:00:00", true)]   // 09:00 >= 08:00 -> valid
    [InlineData("20991231-09:00:00", true)]   // different date, same verdict -> deterministic
    [InlineData("20260101-07:00:00", false)]  // 07:00 < 08:00 -> invalid
    public void Time_only_min_bound_compares_on_time_of_day(string wireValue, bool expectValid)
    {
        var p = Param(minText: "08:00:00", maxText: null);

        p.WireValue = wireValue;          // round-trips through SetWireValue -> ValidateValue

        // If invalid, SetWireValue throws; assert accordingly.
        // (See implementer note for the precise assertion shape.)
    }

    [Fact]
    public void Full_datetime_bound_still_compares_as_datetime()
    {
        var p = Param(minText: null, maxText: "20260601-12:00:00");

        var act = () => p.WireValue = "20260601-13:00:00";

        act.Should().Throw<FixPortal.FixAtdl.Diagnostics.Exceptions.InvalidFieldValueException>();
    }
}
```

> **Implementer note:** The exact failure surface of an out-of-range wire value is `AtdlValueType.SetWireValue` throwing `InvalidFieldValueException` (see `AtdlValueType.cs:142-145`). Choose the assertion form that matches: for the valid cases assert `p.WireValue` round-trips without throwing; for invalid cases assert `(() => p.WireValue = value).Should().Throw<InvalidFieldValueException>()`. Restructure the `[Theory]` into explicit valid/invalid facts or a theory with an `expectValid` branch — whichever reads cleanly. Confirm `MinValueText`/`MaxValueText` is the property name you introduce in Step 3 (keep them consistent). Verify the `Parameter_t<UTCTimestamp_t>` + `p.Value` construction against existing parameter tests.

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~TimestampBoundConformanceTests"`
Expected: FAIL — `MinValueText`/`MaxValueText` do not exist (compile error), or the date-contaminated comparison gives a non-deterministic / wrong verdict.

- [ ] **Step 3: Add time-only bound capture + comparison to `DateTimeTypeBase`**

In `src/FixPortal.FixAtdl/Model/Types/Support/DateTimeTypeBase.cs`:

(a) Add the raw setters + time-only bound fields after the existing `MaxValue`/`MinValue` properties (after line 33's `MinValue`):

```csharp
    // C2 — time-only bound capture. A maxValue/minValue written as a bare time-of-day (HH:mm:ss[.fff])
    // is a time-of-day constraint, not a date+time one. The reflective parser routes the raw bound text
    // through MaxValueText/MinValueText; a time-only value is stored here (and compared on the time
    // component only), while a full datetime / date-only value continues to populate MaxValue/MinValue.
    private TimeOnly? _maxTimeOfDay;
    private TimeOnly? _minTimeOfDay;

    private static readonly string[] _timeOnlyBoundFormats = ["HH:mm:ss.fff", "HH:mm:ss"];

    /// <summary>Deserialization-only. Receives the raw <c>maxValue</c> attribute text and parses it with
    /// time-only awareness (C2). Not intended for programmatic use; set <see cref="MaxValue"/> directly
    /// for a full date+time bound.</summary>
    public string MaxValueText { set => SetBound(value, isMax: true); }

    /// <summary>Deserialization-only. Receives the raw <c>minValue</c> attribute text and parses it with
    /// time-only awareness (C2). Not intended for programmatic use; set <see cref="MinValue"/> directly
    /// for a full date+time bound.</summary>
    public string MinValueText { set => SetBound(value, isMax: false); }

    private void SetBound(string text, bool isMax)
    {
        if (TimeOnly.TryParseExact(text, _timeOnlyBoundFormats, CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out TimeOnly timeOfDay))
        {
            if (isMax) { _maxTimeOfDay = timeOfDay; } else { _minTimeOfDay = timeOfDay; }
        }
        else
        {
            DateTime parsed = FixDateTime.Parse(text, CultureInfo.InvariantCulture);
            if (isMax) { MaxValue = parsed; } else { MinValue = parsed; }
        }
    }
```

> Note: `FixDateTime` is in `FixPortal.FixAtdl.Fix`; add `using FixPortal.FixAtdl.Fix;` if not already present. `CultureInfo`/`DateTimeStyles` are in `System.Globalization` (already imported). Use `IDE0045`-compliant explicit `if/else` (not a ternary with side effects) — the form above already complies.

(b) Extend `ValidateValue` (lines 41-72) to also apply the time-of-day bounds. Inside the `if (value != null)` block, after the existing `MinValue` check (line 70), add:

```csharp
            TimeOnly valueTimeOfDay = TimeOnly.FromDateTime((DateTime)value);

            if (_maxTimeOfDay != null && valueTimeOfDay > _maxTimeOfDay)
            {
                return new ValidationResult(ValidationResult.ResultType.Invalid, ErrorMessages.MaxValueExceeded, value, _maxTimeOfDay);
            }

            if (_minTimeOfDay != null && valueTimeOfDay < _minTimeOfDay)
            {
                return new ValidationResult(ValidationResult.ResultType.Invalid, ErrorMessages.MinValueExceeded, value, _minTimeOfDay);
            }
```

> For a `UTCDateTimeTypeBase` subclass the value reaching `DateTimeTypeBase.ValidateValue` is already UTC-adjusted (via `GetAdjustedValue`), so `valueTimeOfDay` is the UTC time-of-day — consistent with how the wire value is emitted. This is the documented "compare the time component" behaviour.

- [ ] **Step 4: Reroute the two schema definitions**

In `src/FixPortal.FixAtdl/Xml/SchemaDefinitions.cs`, change `maxValue`/`minValue` in **`TZTimeOnlyDefinition`** (205-210) and **`UTCTimestampDefinition`** (212-218) from `typeof(DateTime)` / `"Value.MaxValue"` to `typeof(string)` / `"Value.MaxValueText"` (and `MinValueText`). Leave `constValue` and (for UTCTimestamp) `localMktTz` unchanged.

`TZTimeOnlyDefinition`:
```csharp
    private static readonly ElementAttribute[] TZTimeOnlyDefinition =
    [
        new ElementAttribute("constValue", "Value.ConstValue", typeof(DateTime), Required.Optional),
        new ElementAttribute("maxValue", "Value.MaxValueText", typeof(string), Required.Optional),
        new ElementAttribute("minValue", "Value.MinValueText", typeof(string), Required.Optional)
    ];
```

`UTCTimestampDefinition`:
```csharp
    private static readonly ElementAttribute[] UTCTimestampDefinition =
    [
        new ElementAttribute("constValue", "Value.ConstValue", typeof(DateTime), Required.Optional),
        new ElementAttribute("maxValue", "Value.MaxValueText", typeof(string), Required.Optional),
        new ElementAttribute("minValue", "Value.MinValueText", typeof(string), Required.Optional),
        new ElementAttribute("localMktTz", "Value.LocalMktTz", typeof(string), Required.Optional)
    ];
```

- [ ] **Step 5: Run the new test + the full DateTime-type test set**

Run: `dotnet test tests/FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~TimestampBoundConformanceTests"` (expect PASS)
Run: `dotnet test tests/FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~DateTime|FullyQualifiedName~Timestamp|FullyQualifiedName~Clock"` (regression sweep for the date/time families)
Expected: all PASS. If any existing test set `MaxValue`/`MinValue` via the (now-unused-by-schema) DateTime properties, those still work — the properties are retained.

- [ ] **Step 6: Commit**

```
git add src/FixPortal.FixAtdl/Model/Types/Support/DateTimeTypeBase.cs src/FixPortal.FixAtdl/Xml/SchemaDefinitions.cs tests/FixPortal.FixAtdl.Tests/Validation/TimestampBoundConformanceTests.cs
git commit -m "fix(types): time-only timestamp bounds compare on time-of-day, no injected date (batch 5, C2)"
```

---

## Task 5: Phase-A follow-up 1 — `FixDateTime.TryParse` AdjustToUniversal + `LoadDefaultFromFixValue` tests

**Files:**
- Modify: `src/FixPortal.FixAtdl/Fix/FixDateTime.cs:29-37`
- Test: `tests/FixPortal.FixAtdl.Tests/Controls/` (Clock_t `LoadDefaultFromFixValue`) + `tests/FixPortal.FixAtdl.Tests/Fix/` (FixDateTime).

**Context:** `FixDateTime.TryParse` uses `AssumeUniversal` without `AdjustToUniversal`, so an offset-less FIX timestamp parses to a `Kind=Local` value (host-shifted) rather than `Kind=Utc`. `Clock_t.LoadDefaultFromFixValue` is correct only incidentally (its `ToInstant` Local-branch shifts back), and is untested. Add `AdjustToUniversal` so the result is canonically `Kind=Utc`, and pin `LoadDefaultFromFixValue` with tests. This aligns `FixDateTime` with the UTC-family wire-parse style fixed in Phase A (M1).

> **Ordering / ripple note:** this changes the `Kind` of every `FixDateTime.TryParse` result (also used by `ValueConverter` for full-datetime bounds/constValue). The C2 task (Task 4) already routes time-only bounds away from this path; full-datetime bounds now parse to `Kind=Utc` and are compared against UTC-adjusted values — consistent. Run the **whole** suite in Step 4, not just the new tests.

- [ ] **Step 1: Write the failing tests**

Create `tests/FixPortal.FixAtdl.Tests/Fix/FixDateTimeTests.cs` (check for an existing one first; extend if present):

```csharp
using System.Globalization;
using AwesomeAssertions;
using FixPortal.FixAtdl.Fix;

namespace FixPortal.FixAtdl.Tests.Fix;

public class FixDateTimeTests
{
    [Fact]
    public void Offsetless_fix_timestamp_parses_as_utc_kind()
    {
        FixDateTime.TryParse("20260601-08:00:00", CultureInfo.InvariantCulture, out DateTime result)
            .Should().BeTrue();

        result.Kind.Should().Be(DateTimeKind.Utc);
        result.Should().Be(new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc));
    }
}
```

And a `Clock_t.LoadDefaultFromFixValue` test — add to the existing `ClockControlTests` class in `tests/FixPortal.FixAtdl.Tests/Model/Controls/ControlValueTests.cs` (the `LoadInitValue` path exercises `LoadDefaultFromFixValue` when a FIX field value is supplied; confirm how `FixFieldValueProvider` carries a value for the control's parameter against existing usage):

```csharp
    [Fact]
    public void Clock_loads_utc_fix_field_value_as_that_instant()
    {
        var clock = new Clock_t("clk");

        // LoadDefaultFromFixValue is invoked by LoadInitValue when the provider yields a value for this
        // control's field. Confirm the provider construction against existing LoadInitValue tests.
        bool loaded = clock.LoadDefaultFromFixValueForTest("20260601-08:00:00"); // see note

        loaded.Should().BeTrue();
        ((DateTime?)clock.GetCurrentValue()).Should().Be(new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc));
    }
```

> **Implementer note:** `LoadDefaultFromFixValue` is `protected`. Do NOT add a test-only public shim. Instead drive it through the real path: build the `FixFieldValueProvider` that supplies a wire value for the control's referenced field and call `clock.LoadInitValue(provider)`, mirroring whatever existing test exercises `LoadDefaultFromFixValue` (grep `LoadInitValue` + `FixFieldValueProvider` in tests). The Clock here has no `localMktTz`/`InitValue`, so the FIX-value branch is taken. Assert `GetCurrentValue()` returns the UTC instant. If no existing test wires a non-empty `FixFieldValueProvider`, model it on the production call site that populates one.

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test tests/FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~FixDateTimeTests"`
Expected: FAIL — `result.Kind` is `Local`, not `Utc`.

- [ ] **Step 3: Add `AdjustToUniversal`**

In `src/FixPortal.FixAtdl/Fix/FixDateTime.cs`, the exact-parse call (line 36), add `| DateTimeStyles.AdjustToUniversal`:

```csharp
        return DateTime.TryParseExact(value, FixDateTimeFormat.AllFormats, provider, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out result) ||
            DateTime.TryParse(value, provider, DateTimeStyles.AllowWhiteSpaces, out result);
```

Update the comment above it to note the result is canonically `Kind=Utc` (aligns with the UTC-family `WireParseStyles`, batch 5 M1 follow-up).

- [ ] **Step 4: Run the FULL suite**

Run: `dotnet test tests/FixPortal.FixAtdl.Tests`
Expected: all PASS (612 prior + new). If a date/time test relied on the old `Kind=Local` result, investigate — the corrected `Kind=Utc` is the intended behaviour; update the test's expectation only if it was asserting the buggy host-shift.

- [ ] **Step 5: Commit**

```
git add src/FixPortal.FixAtdl/Fix/FixDateTime.cs tests/FixPortal.FixAtdl.Tests/Fix/FixDateTimeTests.cs tests/FixPortal.FixAtdl.Tests/Model/Controls/ControlValueTests.cs
git commit -m "fix(fix): FixDateTime.TryParse normalises to UTC (AdjustToUniversal); test Clock LoadDefaultFromFixValue (batch 5, Phase-A follow-up)"
```

---

## Task 6: Phase-A follow-up 2 — `Clock_t.ToString` sub-second fidelity

**Files:**
- Modify: `src/FixPortal.FixAtdl/Model/Controls/Clock_t.cs:260-265`
- Test: `tests/FixPortal.FixAtdl.Tests/Model/Controls/ControlValueTests.cs` (ClockControlTests).

**Context:** `Clock_t.ToString` always formats with `FixDateTimeFormat.FixDateTime` (`yyyyMMdd-HH:mm:ss`), dropping sub-second precision. Emit milliseconds when the instant carries them, using `FixDateTimeFormat.FixDateTimeMs` (`yyyyMMdd-HH:mm:ss.fff`); keep seconds-only output when there is no sub-second component (so existing whole-second round-trips are unchanged).

- [ ] **Step 1: Write the failing tests**

Add to `ClockControlTests` in `tests/FixPortal.FixAtdl.Tests/Model/Controls/ControlValueTests.cs`:

```csharp
    [Fact]
    public void Clock_to_string_includes_milliseconds_when_present()
    {
        var clock = new Clock_t("clk");
        clock.SetValue(new DateTime(2026, 6, 1, 8, 0, 0, 250, DateTimeKind.Utc));

        clock.ToString(null!).Should().Be("20260601-08:00:00.250");
    }

    [Fact]
    public void Clock_to_string_omits_milliseconds_when_whole_second()
    {
        var clock = new Clock_t("clk");
        clock.SetValue(new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc));

        clock.ToString(null!).Should().Be("20260601-08:00:00");
    }
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test tests/FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~ClockControlTests"`
Expected: the millisecond test FAILs (returns `20260601-08:00:00`); the whole-second test passes.

- [ ] **Step 3: Implement sub-second-aware formatting**

In `src/FixPortal.FixAtdl/Model/Controls/Clock_t.cs`, replace `ToString` (lines 260-265):

```csharp
    public override string ToString(IParameter targetParameter)
    {
        if (_value == null)
        {
            return null!;
        }

        DateTime utc = _value.Value.ToDateTimeUtc();

        // Emit milliseconds only when present, so whole-second values keep the compact seconds form
        // while sub-second precision is no longer silently dropped (batch 5, Phase-A follow-up).
        string format = utc.Millisecond == 0 ? FixDateTimeFormat.FixDateTime : FixDateTimeFormat.FixDateTimeMs;

        return utc.ToString(format, CultureInfo.InvariantCulture);
    }
```

> The ternary assigns a pure value (no side effect / no `throw`) and has an explicit else value — IDE0045-compliant. This is **not** the S3358 spot (that is line 192 in `SetValue`, handled in Task 7).

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test tests/FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~ClockControlTests"`
Expected: all PASS.

- [ ] **Step 5: Commit**

```
git add src/FixPortal.FixAtdl/Model/Controls/Clock_t.cs tests/FixPortal.FixAtdl.Tests/Model/Controls/ControlValueTests.cs
git commit -m "fix(controls): Clock_t.ToString preserves sub-second precision (batch 5, Phase-A follow-up)"
```

---

## Task 7: Sonar — S3358 nested-ternary extractions (×8)

**Files (all `src/`):** `Model/Controls/Clock_t.cs:192`, `Model/Controls/Support/BinaryControlBase.cs:115,174`, `Model/Controls/Support/NumericControlBase.cs:116`, `Model/Elements/Edit_t.cs:444,471`, `Model/Types/Percentage_t.cs:137`, `Model/Types/Support/MonthYear.cs:32`.

**Context:** S3358 forbids nested ternaries. Each is a behaviour-preserving rewrite to an explicit `if`/`switch` or a flattened single-level expression. **No test changes** — these are pure refactors; the existing suite is the guard. Be precise to keep IDE0045 (no implicit-else) and IDE0047 (no redundant parens) happy.

- [ ] **Step 1: `Clock_t.cs:192`** — the `SetValue` else branch (lines 188-195). Rewrite the nested ternary as explicit control flow:

```csharp
            else if (isDateTime)
            {
                _value = ToInstant((DateTime)newValue);
            }
            else if (newValue == null)
            {
                _value = null;
            }
            else
            {
                throw ThrowHelper.New<InternalErrorException>(this, InternalErrors.UnexpectedArgumentType,
                    newValue.GetType().FullName, "System.String, System.DateTime");
            }
```

(Replaces the `else { _value = isDateTime ? ... : newValue == null ? ... : throw ...; }` block. The outer `if (isString) { ... }` stays.)

- [ ] **Step 2: `BinaryControlBase.cs:115`** — the enum-state assignment. Rewrite:

```csharp
                EnumState state = value.ToEnumState(parameter.EnumPairs);

                if (state[CheckedEnumRef])
                {
                    _value = true;
                }
                else if (state[UncheckedEnumRef])
                {
                    _value = false;
                }
                else
                {
                    _value = null;
                }
```

- [ ] **Step 3: `BinaryControlBase.cs:174`** — the `SetValue` else branch (lines 170-177), same shape as Clock_t Step 1:

```csharp
            else if (isBool)
            {
                _value = (bool?)newValue;
            }
            else if (newValue == null)
            {
                _value = null;
            }
            else
            {
                throw ThrowHelper.New<InternalErrorException>(this, InternalErrors.UnexpectedArgumentType,
                    newValue.GetType().FullName!, "System.String, System.Boolean");
            }
```

- [ ] **Step 4: `NumericControlBase.cs:116`** — same `SetValue` else-branch shape (lines 113-121):

```csharp
            else if (isDecimal)
            {
                _value = (decimal?)newValue;
            }
            else if (newValue == null)
            {
                _value = null;
            }
            else
            {
                throw ThrowHelper.New<InternalErrorException>(this, InternalErrors.UnexpectedArgumentType,
                    newValue.GetType().FullName, "System.String, System.Decimal");
            }
```

- [ ] **Step 5: `Edit_t.cs:444`** — `NormaliseNumericString` (lines 442-446). Flatten by extracting the inner parse:

```csharp
    private static object NormaliseNumericString(object fieldValue)
    {
        if (fieldValue is not string value)
        {
            return fieldValue;
        }

        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal number) ? number : value;
    }
```

- [ ] **Step 6: `Edit_t.cs:471`** — `GetFixFieldValue` (lines 466-472). Flatten:

```csharp
        bool gotValue = additionalValues.TryGetValue(fixField, out var value);

        if (!gotValue)
        {
            result = null;
        }
        else
        {
            result = decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal number) ? number : value;
        }
```

> Confirm `result`'s declaration/scope around line 462-475; adjust to assign correctly (it may be an `out`/local already declared above). Keep the single-level ternary on the inner parse (allowed; only *nested* ternaries trip S3358).

- [ ] **Step 7: `Percentage_t.cs:137`** — `GetNativeValue` (line 137). Flatten the nested `ConstValue != null ? MultiplyBy100 == true ? ... : ... : _value`:

```csharp
        decimal? value;
        if (ConstValue == null)
        {
            value = _value;
        }
        else
        {
            value = MultiplyBy100 == true ? ConstValue / 100 : ConstValue;
        }
```

> Confirm the remainder of `GetNativeValue` uses `value` as before.

- [ ] **Step 8: `MonthYear.cs:32`** — `ToString` (line 32). Flatten the nested `Week != null ? ... : Day != null ? ... : string.Empty`:

```csharp
        string suffix;
        if (Week != null)
        {
            suffix = string.Format(CultureInfo.InvariantCulture, "w{0}", Week);
        }
        else if (Day != null)
        {
            suffix = string.Format(CultureInfo.InvariantCulture, "{0:00}", Day);
        }
        else
        {
            suffix = string.Empty;
        }
```

> `MonthYear` is a `struct` with `readonly` `ToString` — keep the `override readonly string ToString()` signature.

- [ ] **Step 9: Build + run full suite**

Run: `dotnet build src/FixPortal.FixAtdl` (expect 0 S3358)
Run: `dotnet test tests/FixPortal.FixAtdl.Tests` (expect all green — behaviour unchanged)

- [ ] **Step 10: Commit**

```
git add src/FixPortal.FixAtdl/Model/Controls/Clock_t.cs src/FixPortal.FixAtdl/Model/Controls/Support/BinaryControlBase.cs src/FixPortal.FixAtdl/Model/Controls/Support/NumericControlBase.cs src/FixPortal.FixAtdl/Model/Elements/Edit_t.cs src/FixPortal.FixAtdl/Model/Types/Percentage_t.cs src/FixPortal.FixAtdl/Model/Types/Support/MonthYear.cs
git commit -m "refactor: extract nested ternaries to explicit control flow (Sonar S3358)"
```

---

## Task 8: Sonar — remaining mechanical findings (S2292 ×2, S2376, S3060, S3246, S4136, S3963, S125, S2328)

**Context:** Behaviour-preserving cleanups. **No test changes** except where a refactor adds an observable member (none here). The existing suite is the guard. Two items need judgement (S2328 → justified suppression; S3246 → covariance) — apply exactly as specified.

- [ ] **Step 1: S4136 — `ThrowHelper.cs` overload adjacency.** Move the `NewWithParamName<T>` method (line 45, the single non-`New` method sitting between `New` overloads) so all `New<T>` overloads are contiguous. Place `NewWithParamName` immediately **after** the last `New<T>` overload (before the `Rethrow` group). Pure move; no signature/body change.

- [ ] **Step 2: S3246 — `ISimpleDictionary<out T>` covariance.** In `src/FixPortal.FixAtdl/Model/Collections/ISimpleDictionary.cs`, change the declaration:

```csharp
public interface ISimpleDictionary<out T>
```

`T` appears only in output positions (`T this[string key] { get; }`; `Contains(string)` has no `T`), so `out` is safe. Build the whole solution — implementers (`ReadOnlyControlCollection`, `ParameterCollection`, `EditEvaluatingCollection`) and consumers (`IResolvable`, `Edit_t`, `EditRef_t`, `EditEvaluator`) compile unchanged.

- [ ] **Step 3: S2292 ×2 — explicit-interface `Parent` over backing field.** Both `ReadOnlyControlCollection.cs:326-329` and `StateRule_t.cs:73-76` implement `IParentable<T>.Parent` as `get => _owner; set => _owner = value;` over a private `_owner` field that is read elsewhere in the class.

  For each: introduce a **private auto-property** and delegate the explicit interface member to it, replacing all `_owner` references with the property. Example for `StateRule_t`:

  ```csharp
  // remove: private Control_t _owner = null!;
  private Control_t Owner { get; set; } = null!;
  ...
  Control_t IParentable<Control_t>.Parent
  {
      get => Owner; set => Owner = value;
  }
  ```

  Then replace every other `_owner` usage in the file with `Owner`. Do the same for `ReadOnlyControlCollection` (`_owner` → private `Owner` auto-property; `Strategy_t Owner { get; set; }`).

  > **Implementer:** grep each file for `_owner` and replace ALL occurrences. Build to confirm none missed. If `_owner` is read in many places and the rename is noisy, this is still the correct fix (S2292 wants the manual backing field gone).

- [ ] **Step 4: S2376 — `ControlValidationState.ParameterValidationResult` write-only.** In `src/FixPortal.FixAtdl/Validation/ControlValidationState.cs:66`, add a getter delegating to the field:

```csharp
    public ValidationResult ParameterValidationResult
    {
        get => _parameterValidationResult;
        set => _parameterValidationResult = value;
    }
```

(The field `_parameterValidationResult` is read internally at lines 47, 98, 119, 133 — leave those as-is, or switch them to the property; either compiles. Minimal change: just add the getter.)

- [ ] **Step 5: S3060 — `Control_t.IsToggleable` type-test on `this`.** Replace the `this is BinaryControlBase` test with a virtual/override. In `src/FixPortal.FixAtdl/Model/Elements/Control_t.cs:81`:

```csharp
    /// <summary>
    /// Indicates whether this control can be toggled (i.e., is a checkbox or radiobutton).
    /// </summary>
    public virtual bool IsToggleable => false;
```

In `src/FixPortal.FixAtdl/Model/Controls/Support/BinaryControlBase.cs`, add the override (place near the other overrides):

```csharp
    /// <summary>Binary controls (CheckBox_t/RadioButton_t) are toggleable.</summary>
    public override bool IsToggleable => true;
```

> Confirm `BinaryControlBase` derives (transitively) from `Control_t` (it does) so `override` binds. Build the solution — any other `IsToggleable` reader is unaffected.

- [ ] **Step 6: S3963 — `ModelUtils` static constructor.** In `src/FixPortal.FixAtdl/Utility/ModelUtils.cs`, remove the static ctor by moving its body into a static factory method and inline-initialising `_types`:

```csharp
    private static readonly Type[] _types = BuildModelTypes();
    private static readonly Dictionary<string, MethodInfo> _methodInfoCache = [];

    private static Type[] BuildModelTypes()
    {
        Type[] allTypes;

        try
        {
            allTypes = Assembly.GetExecutingAssembly().GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // One unloadable type would otherwise bubble a TypeInitializationException out of the type
            // initializer and brick every consumer of ModelUtils for the process lifetime.
            allTypes = [.. ex.Types.Where(t => t != null).Cast<Type>()];
        }

        // Materialise once so GetTypeFromName does not re-run the LINQ predicate on every call.
        return [.. allTypes.Where(t => t.IsClass && t.Namespace == "FixPortal.FixAtdl.Model.Types" && !t.IsAbstract)];
    }
```

Delete the `static ModelUtils()` constructor. Field initialisation order: `_types`'s initialiser runs at type-init time exactly as the ctor did.

- [ ] **Step 7: S125 — `BinaryControlBase.cs:29` "commented-out code" false-positive.** Lines 26-31 are the M3 explanatory comment from Phase B; Sonar mis-flags it because tokens like `Reset()` look code-like. Reword the comment to remove code-like tokens (no parentheses-call syntax, no `null =` assignment-looking fragments). Replace the block with prose that reads as documentation, e.g.:

```csharp
    // Defaults to a concrete false (not null) so an unset binary control reads as "false" for
    // EQ "false" StateRules even before LoadDefaults runs, matching the post-LoadDefaults default.
    // Because false is a value that IS sent over FIX, an unset control also reads as present for
    // EX and NX edits, giving EX true and NX false. The reset path deliberately restores null,
    // meaning "do not send"; that asymmetry between construction and reset is intentional.
```

> Keep the meaning identical; just avoid `Reset()`, `(`/`)` call syntax, and `null = "..."` fragments that trip S125. Confirm S125 clears on rebuild; if it persists on a specific line, reword that line further (do not suppress — this is genuinely a comment).

- [ ] **Step 8: S2328 — `EnumState.GetHashCode` references mutable fields → justified suppression.** `EnumState` is a deliberately mutable state holder; its `Equals`/`GetHashCode` are consistent at any point in time and it is **not** used as a hash-table key anywhere (verified). Removing `GetHashCode` would break the `Equals` contract. Suppress S2328 with a justification at the method. In `src/FixPortal.FixAtdl/Model/Controls/Support/EnumState.cs`, immediately above `public override int GetHashCode()` (line 153):

```csharp
#pragma warning disable S2328 // "GetHashCode" should not reference mutable fields
    // EnumState is a deliberately mutable control-state holder and is never used as a hash-table key.
    // Equals and GetHashCode are kept consistent over the SAME field set (_enumStates, _nonEnumValue) so
    // the contract holds at any instant; making the type immutable purely to satisfy this rule would be a
    // disproportionate redesign of list-control state (batch 5, Sonar disposition).
    public override int GetHashCode()
```

And after the method's closing brace:

```csharp
#pragma warning restore S2328
```

> Use `#pragma warning disable/restore S2328` (analyzer rule ID works directly). Do not wrap unrelated members.

- [ ] **Step 9: Build + full suite**

Run: `dotnet build src/FixPortal.FixAtdl` (expect S2292/S2376/S3060/S3246/S4136/S3963/S125/S2328 all cleared)
Run: `dotnet test tests/FixPortal.FixAtdl.Tests` (expect all green)

- [ ] **Step 10: Commit**

```
git add src/FixPortal.FixAtdl/Diagnostics/ThrowHelper.cs src/FixPortal.FixAtdl/Model/Collections/ISimpleDictionary.cs src/FixPortal.FixAtdl/Model/Collections/ReadOnlyControlCollection.cs src/FixPortal.FixAtdl/Model/Elements/StateRule_t.cs src/FixPortal.FixAtdl/Validation/ControlValidationState.cs src/FixPortal.FixAtdl/Model/Elements/Control_t.cs src/FixPortal.FixAtdl/Model/Controls/Support/BinaryControlBase.cs src/FixPortal.FixAtdl/Utility/ModelUtils.cs src/FixPortal.FixAtdl/Model/Controls/Support/EnumState.cs
git commit -m "refactor: clear mechanical Sonar findings (S2292/S2376/S3060/S3246/S4136/S3963/S125; S2328 justified suppression)"
```

---

## Task 9: Sonar — S3776 cognitive-complexity refactors (×6)

**Files:** `Xml/Serialization/ElementFactory.cs:352,423`, `Xml/Serialization/ValueConverter.cs:43`, `Model/Collections/EditEvaluatingCollection.cs:64`, `Model/Collections/ReadOnlyControlCollection.cs:254`, `Model/Controls/Support/EnumState.cs:371`.

**Context:** Each method exceeds the cognitive-complexity threshold (15). Reduce by **extracting cohesive helper methods** — behaviour-preserving. **No test changes**; the existing suite (which exercises deserialization heavily) is the guard. After each extraction, rebuild to confirm the rule clears (the helper must actually move enough branching out of the parent).

- [ ] **Step 1: `ElementFactory.ProcessAttributes` (352, 28→15).** Extract the two property-setting branches inside the `foreach`. After the mandatory/null guards, the body becomes:

```csharp
        if (attrDefn.Property.Contains('.'))
        {
            SetIndirectPropertyValue(targetType, attrDefn, target, value);
        }
        else
        {
            SetDirectPropertyValue(targetType, attrDefn, target, value);
        }
```

Add two private helpers carrying the existing indirect-property block (the `names = Split('.')`, outer/inner property resolution, all four guards, `SetPropertyValue(property, innerObject, value)`) and the direct block respectively. Signatures:

```csharp
private void SetIndirectPropertyValue(Type targetType, ElementAttribute attrDefn, object target, object value)
private void SetDirectPropertyValue(Type targetType, ElementAttribute attrDefn, object target, object value)
```

Move the existing code verbatim into them. This drops the `foreach` body well under 15.

- [ ] **Step 2: `ElementFactory.ProcessChildren` (423, 25→15).** Extract the inner `foreach (XElement childElement in matchingChildElements)` body into a helper, and the matching-element selection into another:

```csharp
private IEnumerable<XElement> GetMatchingChildElements(ChildElementDefinition childDefinition, ElementDefinition targetDefinition, bool hasContainerElement, XElement sourceElement)
private void ProcessMatchingChild(ElementDefinition definition, ChildElementDefinition childDefinition, ElementDefinition targetDefinition, Type targetType, object target, XElement childElement)
```

`GetMatchingChildElements` returns the container-vs-direct element query (it may return empty when the container is absent — fold the `if (containerElement == null) continue;` into "return empty"). The outer loop then does:

```csharp
        foreach (ChildElementDefinition childDefinition in definition.ChildElements!)
        {
            bool isRecursiveDefinition = childDefinition.ElementDefinition is RecursiveTypeElementDefinition;
            bool hasContainerElement = !isRecursiveDefinition && childDefinition.ContainerElementName != null;
            ElementDefinition targetDefinition = isRecursiveDefinition ? definition : childDefinition.ElementDefinition;

            foreach (XElement childElement in GetMatchingChildElements(childDefinition, targetDefinition, hasContainerElement, sourceElement))
            {
                ProcessMatchingChild(definition, childDefinition, targetDefinition, targetType, target, childElement);
            }
        }
```

`ProcessMatchingChild` carries the `childObject = targetDefinition switch {...}`, the property lookup + null guard, and the `try/catch (FixAtdlException)/catch (ArgumentException)` around `ProcessChildProperty`. Move verbatim.

- [ ] **Step 3: `ValueConverter.ConvertTo` (43, 20→15).** The per-case `try { return Convert.ToX(...) } catch (FormatException/OverflowException) { throw ... }` blocks drive the complexity. Extract a single generic wrapper and call it from the numeric/fixtag cases:

```csharp
private static object ParseOrThrow(string value, Type targetType, Func<string, object> convert)
{
    try
    {
        return convert(value);
    }
    catch (Exception ex) when (ex is FormatException or OverflowException)
    {
        throw ThrowHelper.New<InvalidFieldValueException>(ExceptionContext, ex, ErrorMessages.DataConversionError1, value, targetType.Name);
    }
}
```

Then the `Int32`, `Decimal`, and `FixTag` cases collapse to single lines, e.g.:

```csharp
        case "System.Int32":
            return ParseOrThrow(value, targetType, v => Convert.ToInt32(v, CultureInfo.InvariantCulture));

        case "System.Decimal":
            return ParseOrThrow(value, targetType, v => Convert.ToDecimal(v, CultureInfo.InvariantCulture));

        case "FixPortal.FixAtdl.Fix.FixTag":
            return ParseOrThrow(value, targetType, v => new FixTag(Convert.ToInt32(v, CultureInfo.InvariantCulture)));
```

Leave `System.Boolean` (its catch is `FormatException` only — keep as-is or route through a bool-specific wrapper), `System.String`, `System.Char`, `System.DateTime`, and the `default` branch unchanged. Removing three try/catch blocks brings it under 15. Confirm with rebuild; if still ≥16, also extract the `System.Char` and `System.DateTime` blocks into small helpers.

- [ ] **Step 4: `EditEvaluatingCollection.Evaluate` (64, 19→15).** Extract the per-operator state transition (the `switch (LogicOperator)` body) into a helper that updates running state and reports short-circuit:

```csharp
private static bool ApplyOperator(LogicOperator_t logicOperator, bool itemState, ref bool newState, ref int xorCount)
{
    // returns true when evaluation can short-circuit
    switch (logicOperator)
    {
        case LogicOperator_t.And:
            newState &= itemState;
            return !newState;

        case LogicOperator_t.Or:
            newState |= itemState;
            return newState;

        case LogicOperator_t.Not:
            // Schema permits a single operand; evaluate as "no operand is true" so a (schema-invalid)
            // multi-operand NOT is deterministic. Collapses to !operand for one operand.
            if (itemState)
            {
                newState = false;
                return true;
            }
            newState = true;
            return false;

        case LogicOperator_t.Xor:
            // "one and only one": true iff exactly one operand is true.
            if (itemState)
            {
                xorCount++;
            }
            newState = xorCount == 1;
            return false;

        default:
            return false;
    }
}
```

The loop becomes:

```csharp
    foreach (IEdit<T> item in Items)
    {
        item.Evaluate(additionalValues);

        if (ApplyOperator(LogicOperator.Value, item.CurrentState, ref newState, ref xorCount))
        {
            break;
        }
    }
```

> `LogicOperator` is `LogicOperator_t?` — the null guard at the top of `Evaluate` stays; pass `LogicOperator.Value` (or capture into a non-null local after the guard). Keep the initial `newState = LogicOperator == LogicOperator_t.And` and `xorCount = 0` setup. Behaviour identical (the old code set `shortCircuit` then `break`ed at loop top; the new code breaks immediately — equivalent because nothing ran after the switch).

- [ ] **Step 5: `ReadOnlyControlCollection.UpdateRelatedHelperControls` (254, 19→15).** Extract the innermost toggle-application block into a helper:

```csharp
private void ApplyHelperControlToggle(Control_t sourceControl, bool result)
{
    // If the control is a radio button we can only set directly; un-setting is done via its companion.
    if (sourceControl is CheckBox_t || !result)
    {
        sourceControl.SetValue(!result);
    }
    else
    {
        SetCompanionRadioButton((sourceControl as RadioButton_t)!);
    }
}
```

The loop body becomes:

```csharp
    foreach (StateRule_t stateRule in control.StateRules)
    {
        Edit_t<Control_t> edit = stateRule.Edit;

        if (stateRule.Value != Atdl.NullValue || edit.Operator != Operator_t.Equal)
        {
            continue;
        }

        string sourceControlId = edit.Field;

        if (IsValidControlId(sourceControlId) && this[sourceControlId].IsToggleable
            && bool.TryParse(edit.Value, out bool result))
        {
            ApplyHelperControlToggle(this[sourceControlId], result);
        }
    }
```

> Inverting the first `if` to a `continue` guard plus extracting the toggle removes two nesting levels. Confirm `this[sourceControlId]` is cheap (dictionary lookup) — calling it twice is fine; or hoist into a local `Control_t sourceControl = this[sourceControlId];` after the `IsValidControlId` check. Preserve exact behaviour.

- [ ] **Step 6: `EnumState.ToWireValue` (371, 16→15).** Just over the threshold. Extract the inner append decision:

```csharp
private static void AppendWireValue(StringBuilder sb, string value, ref bool hasAtLeastOneValue)
{
    if (hasAtLeastOneValue)
    {
        sb.AppendFormat(CultureInfo.InvariantCulture, " {0}", value);
    }
    else
    {
        sb.Append(value);
        hasAtLeastOneValue = true;
    }
}
```

The loop body becomes:

```csharp
        for (int n = 0; n < _enumStates.Length; n++)
        {
            if (!_enumStates[n])
            {
                continue;
            }

            string value = enumPairs.GetWireValueFromEnumId(_enumIds[n]);

            // {NULL} is typically mutually exclusive; skip it in the wire value.
            if (value != Atdl.NullValue)
            {
                AppendWireValue(sb, value, ref hasAtLeastOneValue);
            }
        }
```

- [ ] **Step 7: Build + full suite**

Run: `dotnet build src/FixPortal.FixAtdl` (expect 0 S3776)
Run: `dotnet test tests/FixPortal.FixAtdl.Tests` (expect all green — these methods are heavily exercised by deserialization tests)

- [ ] **Step 8: Commit**

```
git add src/FixPortal.FixAtdl/Xml/Serialization/ElementFactory.cs src/FixPortal.FixAtdl/Xml/Serialization/ValueConverter.cs src/FixPortal.FixAtdl/Model/Collections/EditEvaluatingCollection.cs src/FixPortal.FixAtdl/Model/Collections/ReadOnlyControlCollection.cs src/FixPortal.FixAtdl/Model/Controls/Support/EnumState.cs
git commit -m "refactor: reduce cognitive complexity by extracting helpers (Sonar S3776)"
```

---

## Task 10: Flip `TreatWarningsAsErrors` + finalize

**Files:**
- Modify: `Directory.Build.props:6-8`

**Context:** With all 24 Sonar findings cleared, warnings-as-errors should now pass a clean rebuild. Flip the flag and remove the stale "temporarily false" comment.

- [ ] **Step 1: Flip the flag**

In `Directory.Build.props`, replace lines 6-8:

```xml
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
```

Remove the two-line `<!-- Temporarily false: ... -->` comment above it (the Sonar backlog is now cleared).

- [ ] **Step 2: Full clean rebuild as the gate**

Run: `dotnet build FixPortal.FixAtdl.sln -c Debug --no-incremental`
Expected: **0 errors, 0 warnings.** If anything fails, the offending finding was missed in Tasks 7-9 — fix it (extract/suppress as appropriate) before proceeding. Do not weaken the flag.

- [ ] **Step 3: Full test suite**

Run: `dotnet test FixPortal.FixAtdl.sln`
Expected: all green (612 prior + the conformance/follow-up tests added in Tasks 1, 3, 4, 5, 6).

- [ ] **Step 4: Commit**

```
git add Directory.Build.props
git commit -m "build: enable TreatWarningsAsErrors now the Sonar backlog is cleared (batch 5)"
```

---

## Finalization (controller — after all tasks)

1. **Final holistic code review** — dispatch a final reviewer over the whole branch diff (`git diff origin/main...HEAD`), focused on: C2's interaction with the Phase-A UTC handling (no double-counting bounds; UTC-adjusted time-of-day comparison is intended); the FixDateTime `AdjustToUniversal` ripple (no test asserted the old host-shift); and that every S3776 extraction is behaviour-preserving (not just threshold-gaming).
2. **Update docs** — append a Phase-C disposition note to `docs/batch-5-conformance-review.md` (H3/H4/M4/C2 now resolved) and tick the design spec's relevant items if tracked.
3. **Use superpowers:finishing-a-development-branch** to open the PR: `gh pr create --repo FixPortal/fixportal-fixatdl --base main` (rebase-merge; GitHub issues are disabled on this repo). Title e.g. `batch 5 Phase C — parse-fidelity conformance (H3/H4/M4/C2) + Sonar clearance → warnings-as-errors`.
4. **Do NOT** add broker-spec fixtures (Phase D) or start the deferred adversarial sweep.

---

## Self-Review (plan author)

- **Spec coverage:** H3 (Task 1), H4 (Task 2), M4 (Task 3), C2 (Task 4) — all design §3/§1.8 conformance items. Phase-A follow-ups 1 & 2 (Tasks 5, 6). All 24 Sonar findings: S3358×8 (Task 7), S2292×2/S2376/S3060/S3246/S4136/S3963/S125/S2328 (Task 8), S3776×6 (Task 9). Flag flip (Task 10). ✓
- **Type consistency:** `MaxValueText`/`MinValueText` introduced in Task 4 Step 3 and referenced by the same names in Task 4 Step 1 test, Step 4 schema, and the implementer note. `IsToggleable` virtual/override (Task 8 Step 5) is consistent with its reader in `ReadOnlyControlCollection` (Task 9 Step 5). ✓
- **Placeholder scan:** every code step shows code; implementer notes flag the few API-shape confirmations (loader entry point, `FixFieldValueProvider` wiring, `result` scoping in `Edit_t`) that must be matched to existing tests rather than invented — these are verification instructions, not placeholders. ✓
- **Ordering:** C2 (Task 4) precedes the FixDateTime `AdjustToUniversal` change (Task 5) so the bound-reroute is in place before the parse-Kind shift; both run the full suite. Sonar tasks (7-9) follow conformance so they refactor the final code shape. Flag flip is last. ✓
