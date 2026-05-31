# Batch 5 Phase D — Obfuscated Broker Conformance Fixtures Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add two obfuscated, real-world-derived FIXatdl 1.1 documents as test fixtures and conformance tests that prove the parser survives real-world input and that the Phase A–C findings (C1/C2/H3) hold on it.

**Architecture:** Two scrubbed XML fixtures live under `tests/.../Fixtures/RealWorld/` and are copied to the test output by the existing `Fixtures\**\*.xml` MSBuild glob. A single xUnit v3 test class loads them through the production `StrategiesReader` and asserts structural invariants plus targeted finding behaviours. The library is load-only (no XML serializer), so "round-trip" means parse-survives plus the FIX-wire path, not XML re-serialization.

**Tech Stack:** .NET 10, xUnit v3, AwesomeAssertions (global usings), NodaTime (`FakeClock`), FixPortal.FixAtdl model/parser.

**Source-data boundary (read first):** The two source documents are real client data; this repo is **public**. Task 1 (the obfuscation transform + commit) is performed by the **controller**, not delegated to a subagent — the real source files and the rename map never enter the repo. Tasks 2–5 are implementer tasks that operate only on the already-committed, already-scrubbed fixtures and must never read the real source files.

---

## File Structure

- `tests/FixPortal.FixAtdl.Tests/Fixtures/RealWorld/tz-clock.xml` — scrubbed timezone spec (15 strategies; `Clock_t`+`localMktTz`, time-only `maxValue`, `SecurityTypes`). **Committed in Task 1.**
- `tests/FixPortal.FixAtdl.Tests/Fixtures/RealWorld/regions-enums.xml` — scrubbed multi-region spec (13 strategies; `Regions`, `Char_t` enums with `@index`, `{NULL}`, deep panels). **Committed in Task 1.**
- `tests/FixPortal.FixAtdl.Tests/Conformance/RealWorldSpecConformanceTests.cs` — all conformance tests. **Created in Task 2; extended in Tasks 3–5.**
- `docs/batch-5-conformance-review.md` — identity reference scrubbed. **Edited in Task 1.**

## Known fixture facts (post-scrub; assertions below depend on these)

- **tz-clock.xml:** `Strategies` count = **15**; first strategy `name="VWAP"` with `providerID="DEMO"`; `SecurityType name="CS"`; `p_StartTime` & `p_EndTime` are `UTCTimestamp_t` (tags 7113/7114); `p_EndTime` has `maxValue="23:59:59"` (time-only); two `Clock_t` controls `i_StartTime` (`initValue="08:00:00"`) and `i_EndTime` (`initValue="23:59:59"`), both `localMktTz="Europe/Berlin"`; `p_Benchmark` is a `String_t` whose `EnumPair enumID="e_Default"` has **no** `index` attribute.
- **regions-enums.xml:** `Strategies` count = **13**; strategy `name="DARK_NA"` (first) with `Region name="TheAmericas" inclusion="Include"` and `Char_t` param `name="Urgency"` (6 EnumPairs `o1..o6`, index `0..5`); strategy `name="SOR_NA"` with `Char_t` param `name="OrdType"` whose EnumPairs are `o1`→index 0, `o2`→index 1, `o3`→index **3**, `o4`→index **4** (non-contiguous); `{NULL}` appears as both an `EnumPair@wireValue` and a `StateRule@value`.

---

### Task 1: Generate, scrub, and commit the fixtures + scrub the findings doc (CONTROLLER)

**This task is executed by the controller, not an implementer subagent.** It is documented here for traceability and review.

**Files:**
- Create (scratch, NOT committed): `D:\Centerprise\work\_atdl-review\obfuscate-specs.ps1`
- Create (committed): `tests/FixPortal.FixAtdl.Tests/Fixtures/RealWorld/tz-clock.xml`, `tests/FixPortal.FixAtdl.Tests/Fixtures/RealWorld/regions-enums.xml`
- Modify (committed): `docs/batch-5-conformance-review.md`

- [ ] **Step 1: Transform.** The scratch script reads the two source docs, strips all XML comments, fixes the encoding declaration to UTF-8, applies an ordered case-insensitive brand→generic replacement map (provider id → `DEMO`; firm/product brand names → generic functional descriptors; industry-standard algo-type names and all FIX tags retained), inserts a neutral provenance comment, and writes the two fixtures into the repo. Replacement map and denylist live only in the script.

- [ ] **Step 2: Leak-scan gate.** The script ends by scanning each generated fixture (case-insensitive) for every original identifier; it throws if any survives. Expected output: two `OK … leak-scan clean` lines.

- [ ] **Step 3: Scrub the findings doc.** In `docs/batch-5-conformance-review.md`, replace the identity-bearing cell in the source table (the one naming the firm and its products) with a neutral structural description, e.g.:

```
| Id 7 / Id 8 | 82 / 1038 | 1,395 lines | byte-identical; multi-region spec — Regions, Char_t enums, deep panels |
```

- [ ] **Step 4: Verify no other leak in committed files.** Run a case-insensitive scan for the original firm/brand terms across `docs/` and `tests/`; expect zero hits outside the scratch dir.

- [ ] **Step 5: Commit.**

```
git add tests/FixPortal.FixAtdl.Tests/Fixtures/RealWorld/tz-clock.xml tests/FixPortal.FixAtdl.Tests/Fixtures/RealWorld/regions-enums.xml docs/batch-5-conformance-review.md
git commit -m "test(conformance): add obfuscated broker fixtures; scrub findings-doc identity"
```

---

### Task 2: Conformance test scaffold + parse-survives / structural invariants

**Files:**
- Create: `tests/FixPortal.FixAtdl.Tests/Conformance/RealWorldSpecConformanceTests.cs`

- [ ] **Step 1: Write the failing tests** (create the file with the shared scaffold + structural-invariant facts):

```csharp
using System.Globalization;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Controls;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Types;
using FixPortal.FixAtdl.Xml;
using NodaTime;
using NodaTime.Testing;
// AwesomeAssertions and Xunit are global usings (see GlobalUsings.cs).

namespace FixPortal.FixAtdl.Tests.Conformance;

/// <summary>
/// Conformance tests over two obfuscated, real-world-derived FIXatdl 1.1 documents. They prove the
/// parser survives real-world-shaped input and that the Phase A–C findings (C1 timezone shift,
/// C2 time-only bounds, H3 EnumPair@index capture) hold on it. Fixtures are synthetic: all firm-,
/// product-, and identity-bearing content was removed; see the fixture provenance comments.
/// </summary>
public class RealWorldSpecConformanceTests
{
    private const string TzClockFixture = "Fixtures/RealWorld/tz-clock.xml";
    private const string RegionsEnumsFixture = "Fixtures/RealWorld/regions-enums.xml";

    private static Strategies_t Load(string relativePath)
    {
        using var stream = File.OpenRead(relativePath);
        return new StrategiesReader().Load(stream);
    }

    [Fact]
    public void TzClock_fixture_parses_with_expected_strategy_count()
    {
        Load(TzClockFixture).Count.Should().Be(15);
    }

    [Fact]
    public void RegionsEnums_fixture_parses_with_expected_strategy_count()
    {
        Load(RegionsEnumsFixture).Count.Should().Be(13);
    }

    [Fact]
    public void TzClock_first_strategy_exposes_securitytypes_and_utctimestamp_params()
    {
        var vwap = Load(TzClockFixture)["VWAP"];

        vwap.SecurityTypes.Select(s => s.Name).Should().Contain("CS");
        vwap.Parameters["p_StartTime"].FixTag!.Value.Should().Be((FixTag)7113);
        vwap.Parameters["p_EndTime"].FixTag!.Value.Should().Be((FixTag)7114);
    }

    [Fact]
    public void TzClock_first_strategy_has_two_berlin_clock_controls()
    {
        var clocks = Load(TzClockFixture)["VWAP"].Controls.OfType<Clock_t>().ToList();

        clocks.Select(c => c.Id).Should().BeEquivalentTo("i_StartTime", "i_EndTime");
        clocks.Should().OnlyContain(c => c.LocalMktTz == "Europe/Berlin");
    }

    [Fact]
    public void RegionsEnums_first_strategy_exposes_region_and_char_enum()
    {
        var dark = Load(RegionsEnumsFixture)["DARK_NA"];

        dark.Regions.Select(r => r.Name).Should().Contain("TheAmericas");
        dark.Parameters["Urgency"].EnumPairs.Count.Should().Be(6);
    }
}
```

- [ ] **Step 2: Run to verify they fail.** Run from the worktree root:
  `dotnet test tests/FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~RealWorldSpecConformanceTests"`
  Expected: FAIL — the fixtures are not yet present in the test output, or `FixTag`/collection accessors mismatch. (If Task 1 fixtures are already copied, the tests should actually PASS at this step, confirming the fixtures parse — that is acceptable for a fixture-presence test; proceed.)

- [ ] **Step 3: Make them pass.** No production code changes are expected (all behaviour already exists). If a test fails on an API detail, adjust the assertion to the real accessor (e.g. `FixTag` comparison, `SecurityTypes`/`Regions` element member names) — do not change library code.

- [ ] **Step 4: Run to verify pass.** Same command. Expected: 5 tests PASS.

- [ ] **Step 5: Commit.**

```
git add tests/FixPortal.FixAtdl.Tests/Conformance/RealWorldSpecConformanceTests.cs
git commit -m "test(conformance): parse-survives + structural invariants over broker fixtures"
```

---

### Task 3: C1 — Berlin Clock control emits correctly zone-shifted UTC

**Files:**
- Modify: `tests/FixPortal.FixAtdl.Tests/Conformance/RealWorldSpecConformanceTests.cs`

- [ ] **Step 1: Write the failing tests** (add these methods to the class). They mirror `ClockTimeZoneTests`/`ClockDeserializationTests` but source the control from the parsed fixture. `Strategy_t.Controls` is a flat collection of all controls across nested panels. A `FakeClock` is injected so a time-only `initValue` anchors deterministically; `InitValueMode` is set explicitly (the fixture omits the attribute):

```csharp
    private static Clock_t StartTimeClock(Instant now)
    {
        var clock = Load(TzClockFixture)["VWAP"].Controls
            .OfType<Clock_t>().First(c => c.Id == "i_StartTime");
        clock.Clock = new FakeClock(now);
        clock.InitValueMode = 0;
        clock.LoadInitValue(FixFieldValueProvider.Empty);
        return clock;
    }

    [Fact]
    public void C1_berlin_0800_initValue_emits_0700_utc_in_winter()
    {
        // 2026-01-15 CET (UTC+1): 08:00 Berlin -> 07:00Z.
        StartTimeClock(Instant.FromUtc(2026, 1, 15, 12, 0, 0))
            .ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should().Be(new DateTime(2026, 1, 15, 7, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void C1_berlin_0800_initValue_emits_0600_utc_in_summer()
    {
        // 2026-07-15 CEST (UTC+2): 08:00 Berlin -> 06:00Z.
        StartTimeClock(Instant.FromUtc(2026, 7, 15, 12, 0, 0))
            .ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should().Be(new DateTime(2026, 7, 15, 6, 0, 0, DateTimeKind.Utc));
    }
```

- [ ] **Step 2: Run to verify.** `dotnet test tests/FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~RealWorldSpecConformanceTests"`. Expected: the two new `C1_*` tests PASS (the C1 fix already exists). If they fail, confirm the control is found and `LocalMktTz`/`InitValue` were parsed — do not change library code.

- [ ] **Step 3: Commit.**

```
git add tests/FixPortal.FixAtdl.Tests/Conformance/RealWorldSpecConformanceTests.cs
git commit -m "test(conformance): C1 Berlin clock emits zone-shifted UTC on broker fixture"
```

---

### Task 4: C2 — time-only `maxValue` compares on time-of-day, date-independent

**Files:**
- Modify: `tests/FixPortal.FixAtdl.Tests/Conformance/RealWorldSpecConformanceTests.cs`

- [ ] **Step 1: Write the failing tests** (add these methods). The parsed `p_EndTime` is a `Parameter_t<UTCTimestamp_t>`; the cast reaches the underlying type's `MaxValueText`. The behavioural assertion uses a far-future date: under the pre-C2 (buggy) behaviour a bare `23:59:59` bound parsed as a year-0001 `DateTime` and rejected every real date — accepting a 2099 value proves the time-only fix:

```csharp
    [Fact]
    public void C2_time_only_maxValue_is_captured_as_text()
    {
        var endTime = (Parameter_t<UTCTimestamp_t>)Load(TzClockFixture)["VWAP"].Parameters["p_EndTime"];
        endTime.Value.MaxValueText.Should().Be("23:59:59");
    }

    [Theory]
    [InlineData("20260101-23:59:59")]
    [InlineData("20991231-23:59:59")]
    public void C2_time_only_maxValue_accepts_valid_time_on_any_date(string wireValue)
    {
        var endTime = Load(TzClockFixture)["VWAP"].Parameters["p_EndTime"];
        var act = () => endTime.WireValue = wireValue;
        act.Should().NotThrow();
    }
```

- [ ] **Step 2: Run to verify.** Same `--filter` command. Expected: the three new `C2_*` cases PASS. If the cast fails, confirm the parsed parameter type via the debugger/log — the deserialized `UTCTimestamp_t` parameter is `Parameter_t<UTCTimestamp_t>`; do not change library code.

- [ ] **Step 3: Commit.**

```
git add tests/FixPortal.FixAtdl.Tests/Conformance/RealWorldSpecConformanceTests.cs
git commit -m "test(conformance): C2 time-only maxValue is date-independent on broker fixture"
```

---

### Task 5: H3 — `EnumPair@index` captured verbatim (incl. non-contiguous + null-when-absent)

**Files:**
- Modify: `tests/FixPortal.FixAtdl.Tests/Conformance/RealWorldSpecConformanceTests.cs`

- [ ] **Step 1: Write the failing tests** (add these methods). `SOR_NA/OrdType` has non-contiguous indices (skips 2), proving `@index` is captured verbatim rather than inferred positionally; `VWAP/p_Benchmark` in the tz fixture has an EnumPair with no `index`, giving a real null-when-absent case; and the index must not perturb the wire value:

```csharp
    [Fact]
    public void H3_enumPair_index_is_captured_including_non_contiguous_values()
    {
        var ordType = Load(RegionsEnumsFixture)["SOR_NA"].Parameters["OrdType"];

        ordType.EnumPairs["o1"].Index.Should().Be(0);
        ordType.EnumPairs["o2"].Index.Should().Be(1);
        ordType.EnumPairs["o3"].Index.Should().Be(3); // non-contiguous: index 2 is skipped
        ordType.EnumPairs["o4"].Index.Should().Be(4);
    }

    [Fact]
    public void H3_enumPair_index_does_not_perturb_wire_value()
    {
        var ordType = Load(RegionsEnumsFixture)["SOR_NA"].Parameters["OrdType"];
        ordType.EnumPairs["o1"].WireValue.Should().Be("1");
    }

    [Fact]
    public void H3_enumPair_index_is_null_when_attribute_absent()
    {
        var benchmark = Load(TzClockFixture)["VWAP"].Parameters["p_Benchmark"];
        benchmark.EnumPairs["e_Default"].Index.Should().BeNull();
    }
```

- [ ] **Step 2: Run to verify.** Same `--filter` command. Expected: the three new `H3_*` tests PASS (the `Index` property exists from Phase C).

- [ ] **Step 3: Run the whole conformance class + full suite.**
  `dotnet test tests/FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~RealWorldSpecConformanceTests"` (all green), then
  `dotnet test` (full suite green; `TreatWarningsAsErrors` is enforced, so any warning fails the build).

- [ ] **Step 4: Commit.**

```
git add tests/FixPortal.FixAtdl.Tests/Conformance/RealWorldSpecConformanceTests.cs
git commit -m "test(conformance): H3 EnumPair@index captured verbatim on broker fixture"
```

---

## Self-Review

**Spec coverage:** derive-and-scrub transform + leak guard (Task 1) ✓; both fixtures committed (Task 1) ✓; findings-doc cleanup (Task 1) ✓; parse-survives + structural invariants (Task 2) ✓; C1 (Task 3) ✓; C2 (Task 4) ✓; H3 (Task 5) ✓; round-trip via FIX-wire path (Task 4 `WireValue`) ✓. H1/M2/M3 presence is implicitly covered by parse-survives (the fixtures contain list controls, two-field edits, and binary controls that all parse in Task 2's load); no separate task — they were "where present" in the spec and add no regression value beyond parse-survives, so YAGNI.

**Placeholder scan:** none — every code step shows complete code and an exact `dotnet test --filter` command with expected result.

**Type consistency:** `Load` returns `Strategies_t`; `["name"]` → `Strategy_t`; `.Parameters["name"]` → `IParameter` (has `FixTag`, `WireValue`, `EnumPairs`); cast to `Parameter_t<UTCTimestamp_t>` for `.Value.MaxValueText`; `.Controls.OfType<Clock_t>()` flat lookup; `Clock_t` has `Clock`/`InitValueMode`/`LoadInitValue`/`ToDateTime`/`LocalMktTz`/`Id`; `EnumPairs["id"].Index` is `int?`. All verified against existing tests (`ClockDeserializationTests`, `TimestampBoundConformanceTests`, `EnumPairDeserializationTests`, `StrategiesParserTests`) and model source.
