# FixPortal.FixAtdl Phase 1 — Test Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Each task follows superpowers:test-driven-development / characterization-testing: write a failing/pinning test, run it, confirm, commit frequently.

**Goal:** Raise line/branch coverage across the core namespaces to the tiered, core-weighted bar locked with the user, broaden Stryker mutation testing beyond the single-file pilot, and add a CI coverage floor — without changing production behaviour (characterization-first; any behaviour change is out of scope and deferred to Phase 2).

**Architecture:** Pure test-addition phase. The library API is exercised through two proven entry patterns already used in the suite: (1) **direct construction** of model objects (`new Int_t()`, `new Parameter_t<T>(name)`, `new Clock_t("id")`, the `KeyedCollection` subclasses) for isolated unit tests; (2) **fixture parsing** via `new StrategiesReader().Load(stream)` against `tests/.../Fixtures/*.xml` for integration-shaped tests. No source under `src/` is modified except `stryker-config.json` (mutate scope) and `.github/workflows/build-and-test.yml` (coverage gate).

**Tech Stack:** xUnit v3 (`[Fact]`/`[Theory]`/`[InlineData]`), AwesomeAssertions (`.Should()`), NSubstitute (for `IParameter`/`IControlConvertible` seams only where a real object is impractical), `Microsoft.Extensions.TimeProvider.Testing` (`FakeTimeProvider`, already referenced), coverlet.collector + ReportGenerator (wired in Phase 0), dotnet-stryker 4.14.2.

**Roadmap:** `docs/superpowers/plans/2026-05-30-fixatdl-1.0-roadmap.md` (this is Phase 1; gate per roadmap = Phase 0 merged ✅).

**Baseline:** `docs/coverage-baseline.md` — overall 32% line / 19% branch at the start of this phase.

---

## Locked exit bar (tiered, core-weighted)

Per-namespace **line** floors, **branch ≥ 45%** across the core set, Stryker score recorded:

| Namespace | Baseline line % | Phase 1 line floor |
|-----------|----------------:|-------------------:|
| `Model.Types.*` | 13% | **≥ 65%** |
| `Validation.*` | 20% | **≥ 65%** |
| `Fix.*` | 26% | **≥ 65%** |
| `Model.Collections.*` | 26% | **≥ 60%** |
| `Model.Elements.*` | 39% | **≥ 55%** |
| `Model.Controls.*` | 6% | **≥ 50%** |
| `Xml.Serialization.*` | 70% | **hold ≥ 70%** |
| `Model.Reference.*` | 0% | best-effort (not gated) |

The longer-term ≥80%/≥70% target is deferred to a future coverage pass after Phase 1 (NOT Phase 2, which is TODO-clearing).

## How each task measures its namespace coverage

After writing tests, regenerate the report (same tooling as Phase 0) and read the per-class numbers, aggregating to the namespace:

```
dotnet test D:\fix-portal\fixportal-fixatdl\FixPortal.FixAtdl.sln --collect:"XPlat Code Coverage" --results-directory D:\fix-portal\fixportal-fixatdl\coverage\raw
dotnet reportgenerator -reports:D:\fix-portal\fixportal-fixatdl\coverage\raw\**\coverage.cobertura.xml -targetdir:D:\fix-portal\fixportal-fixatdl\coverage\report -reporttypes:"TextSummary;JsonSummary"
```

Read `coverage/report/Summary.txt` (per-class %) and sum covered/coverable lines across the namespace's classes to confirm the floor. (`coverage/` is git-ignored — never commit it.)

---

## File Structure

All new files under `tests/FixPortal.FixAtdl.Tests/`, mirroring the source namespace layout:

- `Model/Types/ValueTypeConversionTests.cs` — numeric/bool/char/string `_t` round-trips (Task 1)
- `Model/Types/Support/MonthYearTests.cs`, `Model/Types/Support/TenorTests.cs` — the struct parsers (Task 1)
- `Model/Types/DateTimeTypeTests.cs` — UTC/TZ/LocalMktDate `_t` types (Task 1)
- `Validation/EditEvaluationTests.cs`, `Validation/EditValueConverterTests.cs`, `Validation/ControlValidationStateTests.cs` (Task 2)
- `Fix/FixMessageTests.cs`, `Fix/FixTagValuesCollectionTests.cs`, `Fix/FixPrimitivesTests.cs` (FixTag/NumInGroup/FixDateTime) (Task 3)
- `Model/Collections/KeyedCollectionTests.cs`, `Model/Collections/EditEvaluatingCollectionTests.cs` (Task 4)
- `Model/Elements/EditTests.cs`, `Model/Elements/ElementPropertyTests.cs` (Task 5)
- `Model/Controls/ControlValueTests.cs` (Task 6)
- Modify: `stryker-config.json`, `.github/workflows/build-and-test.yml` (Task 7)

Existing `GlobalUsings.cs` already provides `AwesomeAssertions`, `NSubstitute`, `Xunit`. Each test file adds its own `using FixPortal.FixAtdl.*;` namespaces.

---

## Task 1: Model.Types — wire-value conversions (→ ≥65% line)

**Why first:** Largest leverage. Most `_t` types sit at 0% and share one behaviour — string⇄native wire conversion — so a parameterized theory per type lifts coverage fast. These types encode the actual FIXatdl value semantics, so they carry the heaviest bar.

**Files:**
- Create: `tests/FixPortal.FixAtdl.Tests/Model/Types/ValueTypeConversionTests.cs`
- Create: `tests/FixPortal.FixAtdl.Tests/Model/Types/Support/MonthYearTests.cs`
- Create: `tests/FixPortal.FixAtdl.Tests/Model/Types/Support/TenorTests.cs`
- Create: `tests/FixPortal.FixAtdl.Tests/Model/Types/DateTimeTypeTests.cs`

**Verified API (use exactly):**
- Construct via parameter: `var p = new Parameter_t<Int_t>("name");` (`Parameter_t<T> where T : IParameterType, new()`, ctor takes `string name`).
- Set wire → native: `p.WireValue = "100";` (drives `SetWireValue` → `ConvertFromWireValueFormat`). Getter `p.WireValue` drives `ConvertToWireValueFormat`. `p.GetCurrentValue()` returns the boxed native value.
- Invalid wire input throws `FixPortal.FixAtdl.Diagnostics.Exceptions.InvalidFieldValueException` (Float/Int via wrapped FormatException/OverflowException; Boolean/Char directly).
- `Amt_t : Float_t` (no overrides — same behaviour). `Boolean_t` defaults `"Y"`/`"N"`. `Char_t` requires exactly one char. `String_t` is identity, empty→omitted.
- Support structs: `MonthYear.Parse(string)` / `Tenor.Parse(string)` are `public static`, return the struct; `.ToString()` renders the wire form; invalid input throws `System.ArgumentException`.

- [ ] **Step 1: Write the value-type round-trip test (failing → compile-run)**

Create `tests/FixPortal.FixAtdl.Tests/Model/Types/ValueTypeConversionTests.cs`:

```csharp
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Types;

namespace FixPortal.FixAtdl.Tests.Model.Types;

/// <summary>
/// Characterizes the string-wire ⇄ native round-trip for the scalar FIXatdl value types,
/// exercised through Parameter_t&lt;T&gt; (the proven entry point: WireValue set/get drives the
/// type's ConvertFrom/ConvertToWireValueFormat).
/// </summary>
public class ValueTypeConversionTests
{
    [Theory]
    [InlineData("100", 100)]
    [InlineData("-5", -5)]
    [InlineData("0", 0)]
    public void Int_t_round_trips_valid_wire_values(string wire, int expected)
    {
        var p = new Parameter_t<Int_t>("Qty");
        p.WireValue = wire;
        p.GetCurrentValue().Should().Be(expected);
        p.WireValue.Should().Be(wire);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("9999999999999999999999")] // overflow
    public void Int_t_rejects_unparseable_wire_value(string bad)
    {
        var p = new Parameter_t<Int_t>("Qty");
        var act = () => p.WireValue = bad;
        act.Should().Throw<InvalidFieldValueException>();
    }

    [Theory]
    [InlineData("Y", true)]
    [InlineData("N", false)]
    public void Boolean_t_maps_default_wire_tokens(string wire, bool expected)
    {
        var p = new Parameter_t<Boolean_t>("Flag");
        p.WireValue = wire;
        p.GetCurrentValue().Should().Be(expected);
    }

    [Fact]
    public void Boolean_t_rejects_unknown_token()
    {
        var p = new Parameter_t<Boolean_t>("Flag");
        var act = () => p.WireValue = "X";
        act.Should().Throw<InvalidFieldValueException>();
    }

    [Fact]
    public void Char_t_requires_exactly_one_character()
    {
        var p = new Parameter_t<Char_t>("Side");
        var act = () => p.WireValue = "AB";
        act.Should().Throw<InvalidFieldValueException>();
    }

    [Theory]
    [InlineData("123.45")]
    [InlineData("0.1")]
    public void Float_t_round_trips_decimals(string wire)
    {
        var p = new Parameter_t<Float_t>("Px");
        p.WireValue = wire;
        p.WireValue.Should().Be(wire);
    }
}
```

- [ ] **Step 2: Run it, confirm pass**

Run: `dotnet test D:\fix-portal\fixportal-fixatdl\FixPortal.FixAtdl.sln --filter "FullyQualifiedName~ValueTypeConversionTests"`
Expected: all green. If `GetCurrentValue()` returns a differently-typed box than expected for any type, adjust the assertion to the actual native type (characterization — pin what it really does) and note it; do not change source.

- [ ] **Step 3: Write the support-struct tests**

Create `tests/FixPortal.FixAtdl.Tests/Model/Types/Support/MonthYearTests.cs`:

```csharp
using FixPortal.FixAtdl.Model.Types.Support;

namespace FixPortal.FixAtdl.Tests.Model.Types.Support;

public class MonthYearTests
{
    [Theory]
    [InlineData("202601")]    // YYYYMM
    [InlineData("20260115")]  // YYYYMMDD
    public void Parse_then_ToString_round_trips(string wire)
    {
        MonthYear.Parse(wire).ToString().Should().Be(wire);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2026")]      // too short
    [InlineData("202613")]    // month 13
    public void Parse_rejects_invalid_input(string bad)
    {
        var act = () => MonthYear.Parse(bad);
        act.Should().Throw<ArgumentException>();
    }
}
```

Create `tests/FixPortal.FixAtdl.Tests/Model/Types/Support/TenorTests.cs` mirroring the pattern with valid `"D5"`, `"W13"`, `"M3"`, `"Y1"` round-trips and invalid `""`, `"X5"` rejections (`ArgumentException`).

- [ ] **Step 4: Cover the remaining types to the floor**

Add theories (same Parameter_t round-trip pattern) until `Model.Types.*` reaches ≥65% line. Behaviour checklist — cover at least:
- Numeric: `Int_t`, `Float_t`/`Amt_t`, `Percentage_t`, `Qty_t`, `Price_t`, `PriceOffset_t`, `SeqNum_t`, `NumInGroup_t`, `Length_t`, `TagNum_t` (valid + invalid + null-omit).
- Text/enum: `String_t` (identity, empty→omit, SOH-rejection via validation), `Char_t`, `Boolean_t` (default + custom `TrueWireValue`/`FalseWireValue`), `Country_t`, `Currency_t`, `Exchange_t`, `Language_t`, `Data_t`, `MultipleCharValue_t`, `MultipleStringValue_t`.
- Wrappers: `MonthYear_t`, `Tenor_t` (round-trip via Parameter_t, plus invalid→`InvalidFieldValueException`).
- Date/time in `DateTimeTypeTests.cs`: `UTCTimestamp_t`, `UTCTimeOnly_t`, `UTCDateOnly_t`, `TZTimeOnly_t`, `TZTimestamp_t`, `LocalMktDate_t` (round-trip valid wire strings; invalid→`InvalidFieldValueException`).

Commit after each file or logical group (frequent commits): `git add <file>` then `git commit -m "test(types): characterize <area> wire conversions"`.

- [ ] **Step 5: Verify the floor + commit**

Run the coverage measurement (see "How each task measures"). Confirm `Model.Types.*` ≥ 65% line and contributes to branch ≥ 45%. Final commit if not already done.

**Task 1 Acceptance Gate:** `Model.Types.*` ≥ 65% line; all tests green; no source changed.

---

## Task 2: Validation (→ ≥65% line)

**Files:**
- Create: `tests/FixPortal.FixAtdl.Tests/Validation/EditEvaluationTests.cs`
- Create: `tests/FixPortal.FixAtdl.Tests/Validation/EditValueConverterTests.cs`
- Create: `tests/FixPortal.FixAtdl.Tests/Validation/ControlValidationStateTests.cs`

**Context:** `Validation.EditEvaluator<T>` (22%), `EditValueConverter` (21%), `ControlValidationState` (0%), `ValidationResult` (100% already). An existing `EditEvaluatorTests.cs` already drives edits via a fixture — extend that style. The richest logic is `Edit_t<T>.Evaluate` (operator + logic-operator dispatch) and `EditEvaluatingCollection.Evaluate` (And/Or/Not/Xor) — exercised here through the validation seam.

**Verified API:**
- `EditValueConverter.ConvertToComparableType(object typeInstanceToMatch, string value)` → `IComparable`; throws `InvalidFieldValueException` on bad format, `InvalidCastException` on incompatible type.
- `ControlValidationState(string controlId)`; `.Add(StrategyEdit_t)`, `.Evaluate(FixFieldValueProvider)`, props `CurrentState`, `ControlValidationResult`, `ErrorText`.
- `ValidationResult` props `IsValid`, `IsMissing`, `ErrorText`; `ValidationResult.ValidResult` static.
- `FixFieldValueProvider.Empty` static.

- [ ] **Step 1: Write the EditValueConverter test (worked example)**

Create `tests/FixPortal.FixAtdl.Tests/Validation/EditValueConverterTests.cs`:

```csharp
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Validation;

namespace FixPortal.FixAtdl.Tests.Validation;

public class EditValueConverterTests
{
    [Fact]
    public void Converts_numeric_string_against_int_prototype()
    {
        IComparable result = EditValueConverter.ConvertToComparableType(0, "42");
        result.Should().Be(42);
    }

    [Fact]
    public void Throws_on_unparseable_numeric_string()
    {
        var act = () => EditValueConverter.ConvertToComparableType(0, "not-a-number");
        act.Should().Throw<InvalidFieldValueException>();
    }
}
```

Run: `dotnet test ... --filter "FullyQualifiedName~EditValueConverterTests"` → green. If the `typeInstanceToMatch` prototype must be a specific type (not a bare `int`), adjust to the real expected prototype the converter switches on (characterize actual behaviour).

- [ ] **Step 2: Edit evaluation tests**

In `EditEvaluationTests.cs`, extend the existing `EditEvaluatorTests` fixture pattern to cover the gaps: each `Operator_t` (Equal, NotEqual, GreaterThan, LessThan, GreaterThanOrEqual, LessThanOrEqual, Exist, NotExist) and each `LogicOperator_t` (And, Or, Not, Xor) including short-circuit paths, built as `new Edit_t<IParameter> { Field=..., Operator=..., Value=... }` resolved via `((IResolvable<Strategy_t, IParameter>)edit).Resolve(twap, twap.Parameters)` then `edit.Evaluate()`. Mirror the verbatim existing test:

```csharp
[Theory]
[InlineData(Operator_t.Equal, "100", "100", true)]
[InlineData(Operator_t.NotEqual, "100", "101", true)]
[InlineData(Operator_t.GreaterThan, "101", "100", true)]
public async Task Single_edit_evaluates_comparison(Operator_t op, string paramValue, string editValue, bool expected)
{
    var xml = await File.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
    var twap = LoadTwap(xml); // existing helper
    twap.Parameters["Participation"].WireValue = paramValue;
    var edit = new Edit_t<IParameter> { Field = "Participation", Operator = op, Value = editValue };
    ((IResolvable<Strategy_t, IParameter>)edit).Resolve(twap, twap.Parameters);
    edit.Evaluate();
    edit.CurrentState.Should().Be(expected);
}
```

(Reuse the `LoadTwap`/`Load` helpers — if they live in the existing test class, lift them into a shared `TestDoubles`/helper or duplicate minimally per the codebase's current approach.)

- [ ] **Step 3: ControlValidationState tests**

Cover `new ControlValidationState("c1")` construction, `CurrentState` default, `Add`/`Remove` of a `StrategyEdit_t`, `Evaluate(FixFieldValueProvider.Empty)`, and `ErrorText`/`ControlValidationResult` after evaluation. Use a real `StrategyEdit_t` built from a fixture-resolved edit where practical; NSubstitute only for interface seams that can't be constructed.

- [ ] **Step 4: Verify floor + commit**

`Validation.*` ≥ 65% line, branch contributes to ≥ 45%. Commit per file.

**Task 2 Acceptance Gate:** `Validation.*` ≥ 65% line; tests green; no source changed.

---

## Task 3: Fix emission (→ ≥65% line)

**Files:**
- Create: `tests/FixPortal.FixAtdl.Tests/Fix/FixMessageTests.cs`
- Create: `tests/FixPortal.FixAtdl.Tests/Fix/FixTagValuesCollectionTests.cs`
- Create: `tests/FixPortal.FixAtdl.Tests/Fix/FixPrimitivesTests.cs`

**Context:** `FixMessage` (6.6%, `: Dictionary<FixField,string>`), `FixTagValuesCollection` (30%), `FixFieldValueProvider` (12.5%), `FixDateTime` (0%), `FixTag` (77%), `NumInGroup` (0%). `ParameterCollectionOutputTests` already covers the parameter→tags emission path; this task covers the FIX primitives directly.

**Verified API:**
- `FixMessage()` / `FixMessage(string rawMessage)` (throws `FixParseException` on malformed); `.ToFix()` returns SOH-delimited string; `SOH = '\x01'`, `Separator = '='`.
- `FixTagValuesCollection()` / `(string)` / `(FixMessage)`; `.Add(FixTag, string)` (throws `FixParseException` on duplicate); indexers `[FixField]` and `[string]`; `.TryGetValue(...)`; `.ToFix()`; `FixTagValuesCollection.Empty`; collection-initializer syntax `FixTagValuesCollection x = []; x.Add(168, "...");` is proven in existing tests.
- `FixTag(int)` throws `ArgumentOutOfRangeException` if `<= 0`; implicit conversions to/from `int` and `FixField`.
- `NumInGroup(int)` throws `ArgumentOutOfRangeException` if `< 0`; implicit to/from `int`.
- `FixDateTime.TryParse(string, IFormatProvider, out DateTime)` / `Parse(string, IFormatProvider)` (throws `InvalidCastException`).

- [ ] **Step 1: Worked example — FixTag / NumInGroup primitives**

Create `tests/FixPortal.FixAtdl.Tests/Fix/FixPrimitivesTests.cs`:

```csharp
using System.Globalization;
using FixPortal.FixAtdl.Fix;

namespace FixPortal.FixAtdl.Tests.Fix;

public class FixPrimitivesTests
{
    [Fact]
    public void FixTag_rejects_non_positive_tag_number()
    {
        var act = () => new FixTag(0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FixTag_implicitly_converts_to_and_from_int()
    {
        FixTag tag = 168;
        ((int)tag).Should().Be(168);
        tag.ToString().Should().Be("168");
    }

    [Fact]
    public void NumInGroup_rejects_negative()
    {
        var act = () => new NumInGroup(-1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FixDateTime_parses_utc_timestamp()
    {
        FixDateTime.TryParse("20260101-09:30:00", CultureInfo.InvariantCulture, out var dt).Should().BeTrue();
        dt.Year.Should().Be(2026);
    }
}
```

Run filtered; confirm green. If `FixDateTime.TryParse`'s expected format differs, characterize the real accepted format.

- [ ] **Step 2: FixTagValuesCollection tests**

Cover: empty construction + collection initializer; `Add` + indexer get; duplicate `Add` → `FixParseException`; `ToFix()` produces `tag=value<SOH>` segments (assert on `'\x01'` delimiter and `'='`); `TryGetValue` hit/miss; round-trip `new FixTagValuesCollection(toFixString)`.

- [ ] **Step 3: FixMessage + FixFieldValueProvider tests**

Cover: `new FixMessage("35=D\x0149=SENDER\x01")`-style parse into the dictionary; malformed → `FixParseException`; `.ToFix()` round-trip; `FixFieldValueProvider.Empty`; `FixFieldValueProvider(provider, parameters).TryGetValue(...)` hit/miss using a small `ParameterCollection`.

- [ ] **Step 4: Verify floor + commit**

`Fix.*` ≥ 65% line. Commit per file.

**Task 3 Acceptance Gate:** `Fix.*` ≥ 65% line; tests green; no source changed.

---

## Task 4: Model.Collections (→ ≥60% line)

**Files:**
- Create: `tests/FixPortal.FixAtdl.Tests/Model/Collections/KeyedCollectionTests.cs`
- Create: `tests/FixPortal.FixAtdl.Tests/Model/Collections/EditEvaluatingCollectionTests.cs`

**Context — cheap wins:** `MarketCollection`, `SecurityTypeCollection` are trivial `KeyedCollection<,>` subclasses (override `GetKeyForItem` only). `RegionCollection` adds `GetApplicableRegions()`/`IsApplicableTo()`. `CountryCollection(Region_t)` validates region membership in `Add`. `EditCollection` has `HasEdit`/`Clone<T>`. `EnumPairCollection` has `GetWireValueFromEnumId`/`TryParseWireValue`. `EditEvaluatingCollection<T>` (6.3%) holds the And/Or/Not/Xor logic in `Evaluate`.

- [ ] **Step 1: Worked example — KeyedCollection wrappers + EnumPairCollection**

```csharp
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Elements;

namespace FixPortal.FixAtdl.Tests.Model.Collections;

public class KeyedCollectionTests
{
    [Fact]
    public void EnumPairCollection_resolves_wire_value_by_enum_id()
    {
        var pairs = new EnumPairCollection
        {
            new EnumPair_t { EnumId = "BUY", WireValue = "1" },
            new EnumPair_t { EnumId = "SELL", WireValue = "2" },
        };

        pairs.GetWireValueFromEnumId("SELL").Should().Be("2");
        pairs.TryParseWireValue("1", out var enumId).Should().BeTrue();
        enumId.Should().Be("BUY");
    }

    [Fact]
    public void EnumPairCollection_throws_for_unknown_enum_id()
    {
        var pairs = new EnumPairCollection();
        var act = () => pairs.GetWireValueFromEnumId("NOPE");
        act.Should().Throw<InvalidOperationException>();
    }
}
```

Run filtered; confirm green. (Verify `EnumPair_t`'s settable properties `EnumId`/`WireValue` against source; adjust if a ctor is required.)

- [ ] **Step 2: EditEvaluatingCollection logic**

Build `EditEvaluatingCollection<IParameter>` instances, add `Edit_t<IParameter>` children with known `CurrentState`, set `LogicOperator` to each of And/Or/Not/Xor, call `Evaluate(FixFieldValueProvider.Empty)`, assert `CurrentState` and the short-circuit behaviour; assert `Evaluate` with null `LogicOperator` throws `InvalidOperationException`; assert `Sources` aggregates child sources on insert.

- [ ] **Step 3: Remaining collections to the floor**

Cover `RegionCollection.GetApplicableRegions()`/`IsApplicableTo()`, `CountryCollection.Add` success + region-mismatch `ArgumentException`, `EditCollection.HasEdit`/`Clone<T>`, the trivial `MarketCollection`/`SecurityTypeCollection` add+key round-trips, and `ReadOnlyControlCollection` construction + `Contains`/indexer (deeper methods optional). Commit per group.

**Task 4 Acceptance Gate:** `Model.Collections.*` ≥ 60% line; tests green; no source changed.

---

## Task 5: Model.Elements (→ ≥55% line)

**Files:**
- Create: `tests/FixPortal.FixAtdl.Tests/Model/Elements/EditTests.cs`
- Create: `tests/FixPortal.FixAtdl.Tests/Model/Elements/ElementPropertyTests.cs`

**Context:** `Edit_t` (non-generic, 0%, property bag), `Edit_t<T>` (43%, evaluation logic), `EditRef_t<T>` (0%, ctor `(string id)`, Resolve+delegate), `ListItem_t` (33%, property bag + `ToString`→UiRep), `StateRule_t` (17%, `EditEvaluator<Control_t>`, `ToString` formatting), `Parameter_t<T>` (55%), `StrategyEdit_t` (0%). The simpler 0% elements `Country_t`/`Description_t`/`Market_t`/`Region_t`/`SecurityType_t` are property bags — cheap property round-trips.

- [ ] **Step 1: Worked example — property-bag elements**

```csharp
using FixPortal.FixAtdl.Model.Elements;

namespace FixPortal.FixAtdl.Tests.Model.Elements;

public class ElementPropertyTests
{
    [Fact]
    public void ListItem_t_ToString_returns_ui_rep()
    {
        var item = new ListItem_t { EnumId = "BUY", UiRep = "Buy", IsSelected = true };
        item.ToString().Should().Be("Buy");
    }

    [Fact]
    public void Edit_t_default_has_empty_child_edits()
    {
        var edit = new Edit_t();
        edit.Edits.Should().BeEmpty();
    }
}
```

Run filtered; confirm green.

- [ ] **Step 2: Edit_t<T> evaluation + EditRef_t**

In `EditTests.cs`, cover `Edit_t<T>.Evaluate` paths not reached by Task 2 (logic-operator branch via child `Edits`; `Exist`/`NotExist`; the missing-operators `InvalidOperationException`). Cover `EditRef_t<T>(id)` resolution against a fixture strategy's edits and `Evaluate` delegation; unresolved access → `InternalErrorException`; unknown id Resolve → `ReferencedObjectNotFoundException`.

- [ ] **Step 3: StateRule_t + remaining property bags**

Cover `StateRule_t` `Enabled`/`Value`/`Visible` props and `ToString` formatting (null vs set fields); `StrategyEdit_t` `InternalId` non-empty + `ErrorMessage`; property round-trips for `Country_t`/`Market_t`/`Region_t`/`SecurityType_t`/`Description_t`. Commit per group.

**Task 5 Acceptance Gate:** `Model.Elements.*` ≥ 55% line; tests green; no source changed.

---

## Task 6: Model.Controls (→ ≥50% line)

**Files:**
- Create: `tests/FixPortal.FixAtdl.Tests/Model/Controls/ControlValueTests.cs`

**Context:** Controls are normally reflectively constructed, but every concrete control has a `public Ctrl_t(string id)` ctor, so tests construct them **directly** (proven by `ClockTimeProviderTests`). Bases at 0%: `BinaryControlBase`, `NumericControlBase`, `TextControlBase` (4.7%), `ListControlBase` (6.9%), `EnumState` (0%). Concrete controls mostly 0%. Testing through a representative subclass of each base lifts the base + subclass together.

**Verified API:**
- `new TextField_t("id")`, `new CheckBox_t("id")`, `new DropDownList_t("id")`, `new SingleSpinner_t("id")`, `new Clock_t("id")`, etc. — all take only `string id`.
- Shared surface: `GetCurrentValue()` (returns bool?/decimal/string/DateTime?/EnumState), `SetValue(object)`, `Reset()`, `LoadInitValue(FixFieldValueProvider)` (on `InitializableControl<T>`), conversions `ToBoolean/ToDecimal/ToInt32/ToChar/ToString/ToDateTime(IParameter[, IFormatProvider])`.
- `EnumState(string[] enumIds)` ctor; copy ctor `EnumState(EnumState)` (null → `ArgumentException`).
- `FixFieldValueProvider.Empty` for init.

- [ ] **Step 1: Worked example — TextField + CheckBox + SetValue/Reset**

```csharp
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Controls;

namespace FixPortal.FixAtdl.Tests.Model.Controls;

public class ControlValueTests
{
    [Fact]
    public void TextField_set_and_get_round_trips()
    {
        var control = new TextField_t("c_note") { InitValue = "hello" };
        control.LoadInitValue(FixFieldValueProvider.Empty);
        control.GetCurrentValue().Should().Be("hello");

        control.SetValue("world");
        control.GetCurrentValue().Should().Be("world");

        control.Reset();
        control.GetCurrentValue().Should().BeNull();
    }

    [Fact]
    public void CheckBox_default_init_value_loads()
    {
        var control = new CheckBox_t("c_flag") { InitValue = true };
        control.LoadInitValue(FixFieldValueProvider.Empty);
        control.GetCurrentValue().Should().Be(true);
    }
}
```

Run filtered; confirm green. If `Reset()` on a fresh `TextField_t` yields `""` rather than null, characterize the real behaviour (pin it; don't change source).

- [ ] **Step 2: List controls + EnumState**

Cover `DropDownList_t` with `ListItems` populated (build `ListItemCollection` of `ListItem_t`), `GetCurrentValue()` returning an `EnumState`, `HasEnumeratedState`; direct `EnumState(new[]{"A","B"})` set/get of individual enum bits and the copy ctor (+ null → `ArgumentException`).

- [ ] **Step 3: Numeric + Clock + remaining controls to floor**

Cover `SingleSpinner_t`/`DoubleSpinner_t`/`Slider_t` (numeric init + `ToDecimal`), `Clock_t` (extend beyond the existing TimeProvider test: `LocalMktTz`, `InitValueMode` null/0), `Label_t`/`HiddenField_t`/`RadioButton_t`/`MultiSelectList_t`/`SingleSelectList_t`/`RadioButtonList_t`/`CheckBoxList_t`/`EditableDropDownList_t` construction + value round-trips. Commit per group.

**Task 6 Acceptance Gate:** `Model.Controls.*` ≥ 50% line; tests green; no source changed.

---

## Task 7: Broaden Stryker + add CI coverage gate + re-measure

**Files:**
- Modify: `stryker-config.json`
- Modify: `.github/workflows/build-and-test.yml`
- Modify: `docs/coverage-baseline.md` (append a post-Phase-1 "achieved" section)

- [ ] **Step 1: Broaden the Stryker mutate scope**

Edit `stryker-config.json` `mutate` from the single `**/ParameterCollection.cs` to the core namespaces now under test. Replace:

```json
    "mutate": [
      "**/ParameterCollection.cs"
    ]
```

with:

```json
    "mutate": [
      "**/Model/Types/**/*.cs",
      "**/Validation/**/*.cs",
      "**/Fix/**/*.cs",
      "**/Model/Collections/**/*.cs"
    ]
```

- [ ] **Step 2: Run Stryker, record the score**

Run: `dotnet stryker --config-file stryker-config.json`
Expected: completes; record the mutation score. Target ≥ the configured `low` threshold (70). If below, add tests that kill the surviving mutants (mutation testing finds assertion gaps the line-coverage bar misses) until ≥70, or document specific survivors that are equivalent mutants.

- [ ] **Step 3: Add a coverage floor to CI**

In `.github/workflows/build-and-test.yml`, after the test step, add a step that collects coverage and fails the build if overall line coverage drops below the achieved Phase 1 floor. Use coverlet's threshold via `dotnet test` args (simplest, no extra tooling on the runner):

```yaml
      - name: Test with coverage floor
        run: dotnet test FixPortal.FixAtdl.sln -c Release --no-build --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
```

Then add a ReportGenerator + threshold check, OR (preferred, fewer moving parts) set per-project coverlet thresholds in the test `.csproj`/`runsettings`. Read the existing `build-and-test.yml` first and follow its established step style; wire the threshold to the real achieved overall number (record it). Keep the gate at the *achieved* level, not aspirational, so CI is green on merge.

- [ ] **Step 4: Append achieved numbers to the baseline doc**

In `docs/coverage-baseline.md`, append a section recording the post-Phase-1 overall + per-namespace line/branch % and the Stryker score, dated, so the next phase has the new baseline.

- [ ] **Step 5: Commit**

`git add stryker-config.json .github/workflows/build-and-test.yml docs/coverage-baseline.md` then commit `test(coverage): broaden Stryker scope + add CI coverage floor + record Phase 1 results`.

**Task 7 Acceptance Gate:** Stryker runs over the core namespaces with a recorded score ≥ 70 (or documented equivalents); CI enforces a coverage floor at the achieved level; all tests green; baseline doc updated.

---

## Phase 1 Exit Gate (all tasks)

- Per-namespace line floors met: Types/Validation/Fix ≥65%, Collections ≥60%, Elements ≥55%, Controls ≥50%, Xml.Serialization held ≥70%.
- Branch coverage ≥ 45% across the core set.
- Stryker score recorded ≥ 70 over the broadened scope.
- CI coverage floor in place and green.
- No production source changed (test-only phase); behaviour changes deferred to Phase 2.

## Self-Review

**1. Spec coverage** — the locked tiered bar maps one task per gated namespace (Tasks 1–6) plus mutation+CI (Task 7); the `Xml.Serialization` "hold ≥70%" needs no new work (already 70%) and is verified, not built; `Model.Reference` is explicitly best-effort. All roadmap Phase 1 scope (test hardening + Stryker broadening + CI gate) is covered.

**2. Placeholder scan** — every task has at least one complete, compilable worked test grounded in verified APIs (Parameter_t round-trip, MonthYear.Parse, EditValueConverter, FixTag, EnumPairCollection, TextField_t, EditEvaluator fixture pattern). Remaining breadth is expressed as explicit behaviour checklists with the coverage gate as the completion criterion — appropriate for coverage-driven test work, not a "TBD". Steps that might hinge on an exact runtime detail (e.g. `Reset()` → null vs "", `FixDateTime` format, `GetCurrentValue()` boxed type) instruct the implementer to characterize the real behaviour and pin it, never to change source.

**3. Type/name consistency** — type names, ctor signatures, method names, and exception types are taken verbatim from source reads: `Parameter_t<T>(string)`, `WireValue`/`GetCurrentValue()`, `InvalidFieldValueException`, `MonthYear.Parse`, `Tenor.Parse`, `EditValueConverter.ConvertToComparableType`, `ControlValidationState(string)`, `FixTag(int)`/`NumInGroup(int)`/`ArgumentOutOfRangeException`, `FixTagValuesCollection.Add(FixTag,string)`/`FixParseException`, `EditEvaluatingCollection<T>.Evaluate`/`LogicOperator`, `EditRef_t<T>(string)`, `EnumState(string[])`, control `(string id)` ctors. Namespaces match (`FixPortal.FixAtdl.Model.Types`, `.Validation`, `.Fix`, `.Model.Collections`, `.Model.Elements`, `.Model.Controls`, `.Diagnostics.Exceptions`).
