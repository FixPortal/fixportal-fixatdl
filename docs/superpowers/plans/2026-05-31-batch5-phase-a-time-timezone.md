# Phase A — Time & Timezone (C1 + M1) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make a `Clock_t` control whose `initValue` is expressed in a `localMktTz` IANA zone emit the correct **UTC** instant on a `UTCTimestamp_t` FIX field, DST-aware, and fix the UTC value-type wire parse so a parsed value is canonically `Kind=Utc`.

**Architecture:** Introduce NodaTime at exactly one seam — `Clock_t` plus a small `InitValueClock` parse holder. `Clock_t` stores its resolved value as a NodaTime `Instant?` (a UTC point-in-time); it converts *local market wall-clock → UTC* at load time using the injected `IClock` + `IDateTimeZoneProvider`, returns the **local** representation for display (`GetCurrentValue`) and the **UTC** instant at the wire boundary (`ToDateTime`). NodaTime types never leak onto the public model/value surface — controls still return BCL `DateTime?`. Separately, the UTC value-type family is routed through the existing `WireParseStyles` virtual with `AdjustToUniversal` so the dead hardcoded override is retired.

**Tech Stack:** .NET 10, NodaTime 3.3.2 (+ NodaTime.Testing 3.3.2 in tests), xUnit v3, AwesomeAssertions, NSubstitute. Reflective definition-driven deserialization (`ElementFactory` + `SchemaDefinitions` + `ValueConverter`).

**Scope note (read before starting):** The design doc (`docs/superpowers/specs/2026-05-31-batch5-conformance-fixes-design.md`) lists C2 under "Time & timezone". C2 has two halves. The **Clock `initValue` time-only** half is resolved *for free* here — C1 parses `initValue` with NodaTime `LocalTimePattern`, so no spurious date is ever injected. The **`UTCTimestamp_t` time-only bound** half (`maxValue="23:59:59"`) is a type/validation-layer change requiring a bound-representation decision; it is deferred to **Phase C** (parse-fidelity) and is explicitly **out of scope here**. This plan delivers **C1 + M1** as a coherent, independently shippable unit.

**Branch / worktree:** All work on `reviewer-findings-batch5` in the review worktree `D:\fix-portal\fixportal-fixatdl\.claude\worktrees\reviewer-passes`. Per house rules, commit `git add <paths>` and `git commit` as **separate** tool calls.

---

## File Structure

| File | Responsibility | Change |
|---|---|---|
| `Directory.Packages.props` | Central package versions | Add `NodaTime` (runtime), `NodaTime.Testing` (test); drop `Microsoft.Extensions.TimeProvider.Testing` |
| `src/FixPortal.FixAtdl/FixPortal.FixAtdl.csproj` | Library refs | Add `NodaTime` PackageReference |
| `tests/FixPortal.FixAtdl.Tests/FixPortal.FixAtdl.Tests.csproj` | Test refs | Add `NodaTime.Testing`; drop `Microsoft.Extensions.TimeProvider.Testing` |
| `src/FixPortal.FixAtdl/Model/Controls/InitValueClock.cs` | **New.** NodaTime-backed holder for `Clock_t.initValue` (time-only `LocalTime` or full `LocalDateTime`); single-string ctor for the `ValueConverter` escape hatch | Create |
| `src/FixPortal.FixAtdl/Model/Controls/Clock_t.cs` | Clock control: local→UTC conversion seam | Rewrite value storage + resolution; swap `TimeProvider` → `IClock`/`IDateTimeZoneProvider`; `InitValue` type → `InitValueClock?` |
| `src/FixPortal.FixAtdl/Xml/SchemaDefinitions.cs` | Reflective attribute map | Point Clock `initValue` at `typeof(InitValueClock)` |
| `src/FixPortal.FixAtdl/Model/Types/Support/UTCDataTimeTypeBase.cs` | UTC value-type wire parse | Retire hardcoded `ConvertFromWireValueFormat`; add `WireParseStyles` override with `AdjustToUniversal` |
| `tests/FixPortal.FixAtdl.Tests/Controls/InitValueClockTests.cs` | **New.** Holder parse tests | Create |
| `tests/FixPortal.FixAtdl.Tests/Controls/ClockTimeZoneTests.cs` | **New.** C1 zone→UTC tests (FakeClock, DST boundary) | Create |
| `tests/FixPortal.FixAtdl.Tests/Controls/ClockTimeProviderTests.cs` | Old `TimeProvider`/mode-1 test | Rewrite to `FakeClock` + zone (the type it tested no longer exists) |
| `tests/FixPortal.FixAtdl.Tests/Model/Controls/ControlValueTests.cs` | `ClockControlTests` block | Update the cases that assigned a `DateTime` `InitValue` (won't compile) and now require `localMktTz` |
| `tests/FixPortal.FixAtdl.Tests/Xml/ClockDeserializationTests.cs` | **New.** End-to-end parse of a Clock+localMktTz XML → correct UTC | Create |
| `tests/FixPortal.FixAtdl.Tests/Types/UtcTimestampWireParseTests.cs` | **New.** M1 parse-Kind test | Create |

**Why `InitValueClock` is the integration point (do not skip this):** `ValueConverter.ConvertTo` (`src/FixPortal.FixAtdl/Xml/Serialization/ValueConverter.cs:116-120`) already has an escape hatch — any target type whose full name starts with `FixPortal.FixAtdl.Model.Controls.InitValue` is returned as the **raw string**, and `ElementFactory.SetPropertyValue` (`Xml/Serialization/ElementFactory.cs:631-640`) then constructs it via a **single-string-arg constructor**. So a class named `InitValueClock` in namespace `FixPortal.FixAtdl.Model.Controls` with a `public InitValueClock(string raw)` ctor is wired in by the existing machinery once the schema points `initValue` at `typeof(InitValueClock)` — **no change to `ValueConverter` or `ElementFactory`**. The name MUST begin with `InitValue` for the prefix match.

---

## Task 1: Add NodaTime, retire TimeProvider.Testing

**Files:**
- Modify: `Directory.Packages.props`
- Modify: `src/FixPortal.FixAtdl/FixPortal.FixAtdl.csproj`
- Modify: `tests/FixPortal.FixAtdl.Tests/FixPortal.FixAtdl.Tests.csproj`

- [ ] **Step 1: Add NodaTime versions to central package management**

In `Directory.Packages.props`, add `NodaTime` to the Runtime group and `NodaTime.Testing` to the Test group; remove `Microsoft.Extensions.TimeProvider.Testing` (its only consumer, `Clock_t.TimeProvider` + `ClockTimeProviderTests`, is being replaced in this phase).

Runtime group becomes:

```xml
  <ItemGroup Label="Runtime">
    <PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />
    <PackageVersion Include="NodaTime" Version="3.3.2" />
  </ItemGroup>
```

In the Test group, delete the line:

```xml
    <PackageVersion Include="Microsoft.Extensions.TimeProvider.Testing" Version="9.10.0" />
```

and add:

```xml
    <PackageVersion Include="NodaTime.Testing" Version="3.3.2" />
```

- [ ] **Step 2: Reference NodaTime from the library**

In `src/FixPortal.FixAtdl/FixPortal.FixAtdl.csproj`, add to the first `<ItemGroup>`:

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
    <PackageReference Include="NodaTime" />
  </ItemGroup>
```

- [ ] **Step 3: Swap the test project's time package**

In `tests/FixPortal.FixAtdl.Tests/FixPortal.FixAtdl.Tests.csproj`, replace the `Microsoft.Extensions.TimeProvider.Testing` reference with `NodaTime.Testing`:

```xml
    <PackageReference Include="NodaTime.Testing" />
```

(Delete the `<PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" />` line.)

- [ ] **Step 4: Restore + confirm the solution still builds**

Run: `dotnet build D:\fix-portal\fixportal-fixatdl\.claude\worktrees\reviewer-passes\FixPortal.FixAtdl.sln`
Expected: **FAILS to compile** — `ClockTimeProviderTests.cs` still uses `Microsoft.Extensions.Time.Testing` / `Clock_t.TimeProvider`, both now gone. This confirms the package removal took effect. (It is fixed in Task 4; do not commit yet.)

> If you prefer a green checkpoint here, temporarily comment out `ClockTimeProviderTests.cs`'s body — but it is cleaner to leave the solution red until Task 4 and commit Tasks 1+3+4 together. **This plan commits Task 1's project files together with Task 4's Clock rewrite** because the type-signature change makes them inseparable for a compiling build. Tasks 2 and 5 commit independently.

---

## Task 2: `InitValueClock` holder (NodaTime parse)

**Files:**
- Create: `src/FixPortal.FixAtdl/Model/Controls/InitValueClock.cs`
- Test: `tests/FixPortal.FixAtdl.Tests/Controls/InitValueClockTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/FixPortal.FixAtdl.Tests/Controls/InitValueClockTests.cs`:

```csharp
using AwesomeAssertions;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Model.Controls;
using NodaTime;

namespace FixPortal.FixAtdl.Tests.Controls;

public class InitValueClockTests
{
    [Theory]
    [InlineData("08:00:00")]
    [InlineData("23:59:59")]
    [InlineData("08:00:00.250")]
    public void Time_only_value_is_parsed_as_time_of_day(string raw)
    {
        var iv = new InitValueClock(raw);

        iv.IsTimeOnly.Should().BeTrue();
        iv.TimeOfDay.Should().NotBeNull();
        iv.DateTime.Should().BeNull();
    }

    [Fact]
    public void Time_only_value_keeps_the_exact_time_of_day()
    {
        var iv = new InitValueClock("08:00:00");

        iv.TimeOfDay.Should().Be(new LocalTime(8, 0, 0));
    }

    [Theory]
    [InlineData("20260601-09:30:00")]
    [InlineData("20260601-09:30:00.500")]
    public void Full_datetime_value_is_parsed_as_local_datetime(string raw)
    {
        var iv = new InitValueClock(raw);

        iv.IsTimeOnly.Should().BeFalse();
        iv.DateTime.Should().NotBeNull();
        iv.TimeOfDay.Should().BeNull();
    }

    [Fact]
    public void Full_datetime_value_keeps_the_exact_local_datetime()
    {
        var iv = new InitValueClock("20260601-09:30:00");

        iv.DateTime.Should().Be(new LocalDateTime(2026, 6, 1, 9, 30, 0));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-time")]
    [InlineData("25:00:00")]
    [InlineData("2026-06-01T09:30:00")]
    public void Unparseable_value_throws(string raw)
    {
        var act = () => new InitValueClock(raw);

        act.Should().Throw<InvalidFieldValueException>();
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test D:\fix-portal\fixportal-fixatdl\.claude\worktrees\reviewer-passes\tests\FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~InitValueClockTests"`
Expected: FAIL — `InitValueClock` does not exist (compile error).

- [ ] **Step 3: Implement `InitValueClock`**

Create `src/FixPortal.FixAtdl/Model/Controls/InitValueClock.cs`:

```csharp
// FP Enhancement: 2026-05-31 — NodaTime-backed holder for Clock_t initValue (batch 5, C1/C2).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Diagnostics;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Resources;
using NodaTime;
using NodaTime.Text;

namespace FixPortal.FixAtdl.Model.Controls;

/// <summary>
/// Parse-layer holder for a <see cref="Clock_t"/> <c>initValue</c>. A clock initValue is either a
/// time-of-day (e.g. <c>08:00:00</c>) or a full local date-and-time (e.g. <c>20260601-09:30:00</c>),
/// expressed in the control's <c>localMktTz</c> zone. Holding it as a NodaTime <see cref="LocalTime"/>
/// or <see cref="LocalDateTime"/> — rather than a BCL <see cref="System.DateTime"/> — avoids injecting a
/// spurious "today" date into a time-only value (the C2 contamination) and keeps the value zone-agnostic
/// until <see cref="Clock_t"/> resolves it against the market zone.
/// </summary>
/// <remarks>
/// The single-<see cref="string"/> constructor is required: the reflective deserializer routes the
/// <c>initValue</c> attribute here via the <c>ValueConverter</c> "InitValue*" escape hatch, which passes
/// the raw XML text to a one-arg-string constructor. The type name MUST begin with <c>InitValue</c>.
/// </remarks>
public sealed class InitValueClock
{
    private const string ExceptionContext = "InitValueClock";

    // Time-only is tried before date+time; the format sets are disjoint (a time-only string never
    // matches the date+time pattern and vice-versa), so order is for clarity only. NodaTime year token
    // is 'uuuu' (absolute year), not BCL 'yyyy'.
    private static readonly LocalTimePattern[] TimePatterns =
    [
        LocalTimePattern.CreateWithInvariantCulture("HH:mm:ss.fff"),
        LocalTimePattern.CreateWithInvariantCulture("HH:mm:ss"),
    ];

    private static readonly LocalDateTimePattern[] DateTimePatterns =
    [
        LocalDateTimePattern.CreateWithInvariantCulture("uuuuMMdd-HH:mm:ss.fff"),
        LocalDateTimePattern.CreateWithInvariantCulture("uuuuMMdd-HH:mm:ss"),
    ];

    /// <summary>
    /// Parses the supplied raw initValue text into a time-of-day or a local date-and-time.
    /// </summary>
    /// <param name="raw">The raw <c>initValue</c> attribute text.</param>
    /// <exception cref="InvalidFieldValueException">The text matches no supported FIX time/date-time format.</exception>
    public InitValueClock(string raw)
    {
        Raw = raw;

        foreach (LocalTimePattern pattern in TimePatterns)
        {
            ParseResult<LocalTime> result = pattern.Parse(raw);
            if (result.Success)
            {
                TimeOfDay = result.Value;
                return;
            }
        }

        foreach (LocalDateTimePattern pattern in DateTimePatterns)
        {
            ParseResult<LocalDateTime> result = pattern.Parse(raw);
            if (result.Success)
            {
                DateTime = result.Value;
                return;
            }
        }

        throw ThrowHelper.New<InvalidFieldValueException>(ExceptionContext, ErrorMessages.InvalidDateOrTimeValue, raw);
    }

    /// <summary>The raw initValue text, retained for diagnostics.</summary>
    public string Raw { get; }

    /// <summary>The time-of-day, when the initValue was supplied time-only; otherwise null.</summary>
    public LocalTime? TimeOfDay { get; }

    /// <summary>The local date-and-time, when the initValue carried a date; otherwise null.</summary>
    public LocalDateTime? DateTime { get; }

    /// <summary>True when the initValue was a bare time-of-day (no date component).</summary>
    public bool IsTimeOnly => TimeOfDay.HasValue;
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `dotnet test D:\fix-portal\fixportal-fixatdl\.claude\worktrees\reviewer-passes\tests\FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~InitValueClockTests"`
Expected: PASS (all theory cases).

- [ ] **Step 5: Commit**

```
git add src/FixPortal.FixAtdl/Model/Controls/InitValueClock.cs tests/FixPortal.FixAtdl.Tests/Controls/InitValueClockTests.cs
```
```
git commit -m "feat(controls): add NodaTime-backed InitValueClock holder for Clock_t initValue"
```

(End-of-message reminder: `git add` and `git commit` are **separate** tool calls.)

---

## Task 3: M1 — UTC value-type wire parse normalises to `Kind=Utc`

> Done before the Clock rewrite because it is self-contained and keeps the build green independently.

**Files:**
- Modify: `src/FixPortal.FixAtdl/Model/Types/Support/UTCDataTimeTypeBase.cs`
- Test: `tests/FixPortal.FixAtdl.Tests/Types/UtcTimestampWireParseTests.cs`

- [ ] **Step 1: Write the failing test**

Create `tests/FixPortal.FixAtdl.Tests/Types/UtcTimestampWireParseTests.cs`:

```csharp
using AwesomeAssertions;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Types;
using NSubstitute;

namespace FixPortal.FixAtdl.Tests.Types;

/// <summary>
/// M1: the UTC value-type family must parse a wire value to a canonical <see cref="DateTimeKind.Utc"/>
/// (AssumeUniversal alone yields Kind=Local — a host-offset-dependent defect). Routing through
/// WireParseStyles with AdjustToUniversal fixes it.
/// </summary>
public class UtcTimestampWireParseTests
{
    [Fact]
    public void Utc_wire_value_parses_to_utc_kind()
    {
        var host = Substitute.For<IParameter>();
        var ts = new UTCTimestamp_t();

        ts.SetWireValue(host, "20260115-08:00:00");
        var native = (DateTime)ts.GetNativeValue(false);

        native.Kind.Should().Be(DateTimeKind.Utc);
        native.Should().Be(new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc));
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test D:\fix-portal\fixportal-fixatdl\.claude\worktrees\reviewer-passes\tests\FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~UtcTimestampWireParseTests"`
Expected: FAIL — `native.Kind` is `Local` (current `AssumeUniversal` without `AdjustToUniversal`).

- [ ] **Step 3: Retire the hardcoded override; route through `WireParseStyles`**

In `src/FixPortal.FixAtdl/Model/Types/Support/UTCDataTimeTypeBase.cs`:

Delete the entire `ConvertFromWireValueFormat` override (the method block currently spanning the `protected override DateTime? ConvertFromWireValueFormat(string value) { ... }` body) and replace it with a `WireParseStyles` override. After editing, the `#region AtdlReferenceType<string> Overrides` section contains `ValidateValue`, the new `WireParseStyles`, and `ConvertToWireValueFormat`:

```csharp
    /// <summary>
    /// Validates the supplied value in terms of the parameters constraints (e.g., MinValue, MaxValue, etc.).
    /// </summary>
    /// <param name="value">Value to validate, may be null in which case no validation is applied.</param>
    /// <param name="isRequired">Set to true to check that this parameter is non-null.</param>
    /// <returns>ValidationResult indicating whether the supplied value is valid.</returns>
    /// <remarks>DateTime.MaxValue (a date and time at the end of the year 9999) is used to indicate an invalid date or time.</remarks>
    protected override ValidationResult ValidateValue(DateTime? value, bool isRequired)
    {
        return base.ValidateValue(GetAdjustedValue(value), isRequired);
    }

    /// <summary>
    /// <see cref="DateTimeStyles"/> applied when parsing a UTC-family wire value. The value is assumed to
    /// be UTC when no offset is present (FIX UTCTimestamp is by definition UTC) and any explicit offset is
    /// normalised to UTC (<see cref="DateTimeStyles.AdjustToUniversal"/>), so the result is canonically
    /// <see cref="DateTimeKind.Utc"/> and host-offset-independent. Replaces the former hardcoded
    /// AssumeUniversal-only parse, which produced a Kind=Local value (M1).
    /// </summary>
    protected override DateTimeStyles WireParseStyles =>
        DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;

    /// <summary>
    /// Converts the supplied value to a string, as might be used on the FIX wire.
    /// </summary>
    /// <param name="value">Value to convert, may be null.</param>
    /// <returns>If input value is not null, returns value converted to a string; null otherwise.</returns>
    protected override string ConvertToWireValueFormat(DateTime? value)
    {
        string format = GetDateTimeFormatStrings()[0];
        DateTime? adjustedValue = GetAdjustedValue(value);

        return adjustedValue != null ? ((DateTime)adjustedValue).ToString(format, CultureInfo.InvariantCulture) : null!;
    }
```

The base `DateTimeTypeBase.ConvertFromWireValueFormat` (which uses `WireParseStyles`) now handles the parse and throws the identical `InvalidCastException` / `ErrorMessages.InvalidDateOrTimeValue` on failure, so no behaviour is lost.

- [ ] **Step 4: Remove now-unused usings**

The deleted override was the only consumer of `ThrowHelper` and `ErrorMessages` in this file. Remove these two `using` lines from the top of `UTCDataTimeTypeBase.cs`:

```csharp
using FixPortal.FixAtdl.Diagnostics;
using FixPortal.FixAtdl.Resources;
```

Keep `using System.Globalization;` (DateTimeStyles) and `using FixPortal.FixAtdl.Validation;` (ValidationResult).

- [ ] **Step 5: Run to verify it passes**

Run: `dotnet test D:\fix-portal\fixportal-fixatdl\.claude\worktrees\reviewer-passes\tests\FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~UtcTimestampWireParseTests"`
Expected: PASS — `Kind` is `Utc`.

- [ ] **Step 6: Confirm no regression in the UTC/TZ type suite + clean build**

Run: `dotnet build D:\fix-portal\fixportal-fixatdl\.claude\worktrees\reviewer-passes\src\FixPortal.FixAtdl\FixPortal.FixAtdl.csproj`
Expected: build succeeds, **no new warnings** (the removed usings would otherwise warn).

- [ ] **Step 7: Commit**

```
git add src/FixPortal.FixAtdl/Model/Types/Support/UTCDataTimeTypeBase.cs tests/FixPortal.FixAtdl.Tests/Types/UtcTimestampWireParseTests.cs
```
```
git commit -m "fix(types): route UTC wire parse through WireParseStyles with AdjustToUniversal (M1)"
```

---

## Task 4: C1 — `Clock_t` applies `localMktTz`, emits UTC

This task changes a public type signature (`Clock_t : InitializableControl<DateTime?>` → `InitializableControl<InitValueClock?>`, `TimeProvider` → `IClock`/`IDateTimeZoneProvider`). That breaks the two existing test sites at compile time, so the new tests, the existing-test updates, the `Clock_t` rewrite, **and** the schema change are one atomic commit (together with Task 1's project edits). Write the tests first (red), then implement.

**Files:**
- Modify: `src/FixPortal.FixAtdl/Model/Controls/Clock_t.cs`
- Modify: `src/FixPortal.FixAtdl/Xml/SchemaDefinitions.cs`
- Create: `tests/FixPortal.FixAtdl.Tests/Controls/ClockTimeZoneTests.cs`
- Modify (rewrite): `tests/FixPortal.FixAtdl.Tests/Controls/ClockTimeProviderTests.cs`
- Modify: `tests/FixPortal.FixAtdl.Tests/Model/Controls/ControlValueTests.cs` (the `ClockControlTests` block)

- [ ] **Step 1: Write the new C1 tests (DST boundary, missing/invalid zone, mode 1)**

Create `tests/FixPortal.FixAtdl.Tests/Controls/ClockTimeZoneTests.cs`:

```csharp
using System.Globalization;
using AwesomeAssertions;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Controls;
using NodaTime;
using NodaTime.Testing;

namespace FixPortal.FixAtdl.Tests.Controls;

/// <summary>
/// C1: a Clock_t initValue expressed in localMktTz must convert to the correct UTC instant on the wire,
/// DST-aware. "Now" is pinned with a NodaTime FakeClock so the market "today" (and the initValueMode==1
/// branch) are deterministic. GetCurrentValue returns the LOCAL market representation; ToDateTime returns
/// the UTC instant the UTCTimestamp_t field will emit.
/// </summary>
public class ClockTimeZoneTests
{
    private static Clock_t BerlinClock(InitValueClock initValue, Instant now, int? mode = 0) => new("clk")
    {
        InitValue = initValue,
        LocalMktTz = "Europe/Berlin",
        InitValueMode = mode,
        Clock = new FakeClock(now),
    };

    [Fact]
    public void Berlin_0800_in_winter_emits_0700_utc()
    {
        // 2026-01-15 — CET (UTC+1). 08:00 Berlin -> 07:00Z.
        var clock = BerlinClock(new InitValueClock("08:00:00"), Instant.FromUtc(2026, 1, 15, 12, 0, 0));

        clock.LoadInitValue(FixFieldValueProvider.Empty);

        clock.ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should().Be(new DateTime(2026, 1, 15, 7, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Berlin_0800_in_summer_emits_0600_utc()
    {
        // 2026-07-15 — CEST (UTC+2). 08:00 Berlin -> 06:00Z.
        var clock = BerlinClock(new InitValueClock("08:00:00"), Instant.FromUtc(2026, 7, 15, 12, 0, 0));

        clock.LoadInitValue(FixFieldValueProvider.Empty);

        clock.ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should().Be(new DateTime(2026, 7, 15, 6, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void GetCurrentValue_returns_local_market_time_for_display()
    {
        var clock = BerlinClock(new InitValueClock("08:00:00"), Instant.FromUtc(2026, 1, 15, 12, 0, 0));

        clock.LoadInitValue(FixFieldValueProvider.Empty);

        // Local display is 08:00 Berlin wall-clock (DateTime equality is Kind-insensitive).
        ((DateTime)clock.GetCurrentValue())
            .Should().Be(new DateTime(2026, 1, 15, 8, 0, 0));
    }

    [Fact]
    public void Missing_localMktTz_with_initValue_throws()
    {
        var clock = new Clock_t("clk")
        {
            InitValue = new InitValueClock("08:00:00"),
            InitValueMode = 0,
            Clock = new FakeClock(Instant.FromUtc(2026, 1, 15, 12, 0, 0)),
        };

        var act = () => clock.LoadInitValue(FixFieldValueProvider.Empty);

        act.Should().Throw<InvalidFieldValueException>();
    }

    [Fact]
    public void Unrecognised_localMktTz_throws()
    {
        var clock = new Clock_t("clk")
        {
            InitValue = new InitValueClock("08:00:00"),
            LocalMktTz = "Mars/Phobos",
            InitValueMode = 0,
            Clock = new FakeClock(Instant.FromUtc(2026, 1, 15, 12, 0, 0)),
        };

        var act = () => clock.LoadInitValue(FixFieldValueProvider.Empty);

        act.Should().Throw<InvalidFieldValueException>();
    }

    [Fact]
    public void Mode1_uses_now_when_initValue_time_has_passed()
    {
        // now = 10:00Z (= 11:00 Berlin winter); init = 08:00 Berlin = 07:00Z < now -> use now (10:00Z).
        var clock = BerlinClock(new InitValueClock("08:00:00"), Instant.FromUtc(2026, 1, 15, 10, 0, 0), mode: 1);

        clock.LoadInitValue(FixFieldValueProvider.Empty);

        clock.ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should().Be(new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Mode1_keeps_initValue_when_it_is_still_in_the_future()
    {
        // now = 05:00Z (= 06:00 Berlin winter); init = 08:00 Berlin = 07:00Z > now -> keep init (07:00Z).
        var clock = BerlinClock(new InitValueClock("08:00:00"), Instant.FromUtc(2026, 1, 15, 5, 0, 0), mode: 1);

        clock.LoadInitValue(FixFieldValueProvider.Empty);

        clock.ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should().Be(new DateTime(2026, 1, 15, 7, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void No_init_value_yields_null()
    {
        var clock = new Clock_t("clk") { Clock = new FakeClock(Instant.FromUtc(2026, 1, 15, 12, 0, 0)) };

        clock.LoadInitValue(FixFieldValueProvider.Empty);

        clock.ToDateTime(null!, CultureInfo.InvariantCulture).Should().BeNull();
        clock.GetCurrentValue().Should().BeNull();
    }
}
```

- [ ] **Step 2: Rewrite `ClockTimeProviderTests.cs` to the new clock seam**

Replace the **entire contents** of `tests/FixPortal.FixAtdl.Tests/Controls/ClockTimeProviderTests.cs` (it used the removed `TimeProvider` / `Microsoft.Extensions.Time.Testing`):

```csharp
using System.Globalization;
using AwesomeAssertions;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Controls;
using NodaTime;
using NodaTime.Testing;

namespace FixPortal.FixAtdl.Tests.Controls;

/// <summary>
/// initValueMode==1 means "use the current time if the initValue time has already passed". "Now" is
/// pinned via an injected NodaTime IClock (FakeClock) and the comparison is made on instants — UTC,
/// zone-correct — replacing the former host-local DateTime comparison.
/// </summary>
public class ClockTimeProviderTests
{
    [Theory]
    [InlineData(13, 13)] // now (13:00Z) is after initValue (12:00Z) -> control takes now
    [InlineData(11, 12)] // now (11:00Z) is before initValue (12:00Z) -> control keeps initValue
    public void InitValueMode1_uses_injected_clock(int nowHourUtc, int expectedHourUtc)
    {
        // localMktTz = Etc/UTC keeps wall-clock == UTC so the hours compare directly.
        var clock = new Clock_t("clk")
        {
            InitValue = new InitValueClock("12:00:00"),
            LocalMktTz = "Etc/UTC",
            InitValueMode = 1,
            Clock = new FakeClock(Instant.FromUtc(2026, 1, 1, nowHourUtc, 0, 0)),
        };

        clock.LoadInitValue(FixFieldValueProvider.Empty);

        clock.ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should().Be(new DateTime(2026, 1, 1, expectedHourUtc, 0, 0, DateTimeKind.Utc));
    }
}
```

- [ ] **Step 3: Update the `ClockControlTests` block in `ControlValueTests.cs`**

In `tests/FixPortal.FixAtdl.Tests/Model/Controls/ControlValueTests.cs`, the `ClockControlTests` class (around lines 719-801) has cases that assign a `DateTime` to `InitValue` (no longer compiles) and call `LoadInitValue` without `localMktTz` (now throws). Replace the **whole `ClockControlTests` class** with:

```csharp
public class ClockControlTests
{
    [Fact]
    public void Clock_no_init_value_yields_null()
    {
        var clock = new Clock_t("clk");
        clock.LoadInitValue(FixFieldValueProvider.Empty);
        clock.GetCurrentValue().Should().BeNull();
    }

    [Fact]
    public void Clock_init_value_mode_0_uses_init_value()
    {
        var clock = new Clock_t("clk")
        {
            InitValue = new InitValueClock("09:00:00"),
            LocalMktTz = "Etc/UTC",
            InitValueMode = 0,
            Clock = new FakeClock(Instant.FromUtc(2026, 6, 1, 0, 0, 0)),
        };
        clock.LoadInitValue(FixFieldValueProvider.Empty);
        // Etc/UTC: local display == 09:00 on the FakeClock's "today" (2026-06-01).
        ((DateTime)clock.GetCurrentValue()).Should().Be(new DateTime(2026, 6, 1, 9, 0, 0));
    }

    [Fact]
    public void Clock_init_value_mode_null_uses_init_value()
    {
        var clock = new Clock_t("clk")
        {
            InitValue = new InitValueClock("09:00:00"),
            LocalMktTz = "Etc/UTC",
            InitValueMode = null,
            Clock = new FakeClock(Instant.FromUtc(2026, 6, 1, 0, 0, 0)),
        };
        clock.LoadInitValue(FixFieldValueProvider.Empty);
        ((DateTime)clock.GetCurrentValue()).Should().Be(new DateTime(2026, 6, 1, 9, 0, 0));
    }

    [Fact]
    public void Clock_set_value_datetime_round_trips()
    {
        var clock = new Clock_t("clk");
        var dt = new DateTime(2026, 6, 1, 10, 30, 0, DateTimeKind.Utc);
        clock.SetValue(dt);
        ((DateTime?)clock.GetCurrentValue()).Should().Be(dt);
    }

    [Fact]
    public void Clock_set_value_null_yields_null()
    {
        var clock = new Clock_t("clk");
        clock.SetValue(new DateTime(2026, 6, 1, 10, 30, 0, DateTimeKind.Utc));
        clock.SetValue((object)null!);
        clock.GetCurrentValue().Should().BeNull();
    }

    [Fact]
    public void Clock_reset_yields_null()
    {
        var clock = new Clock_t("clk")
        {
            InitValue = new InitValueClock("09:00:00"),
            LocalMktTz = "Etc/UTC",
            Clock = new FakeClock(Instant.FromUtc(2026, 6, 1, 0, 0, 0)),
        };
        clock.LoadInitValue(FixFieldValueProvider.Empty);
        clock.Reset();
        clock.GetCurrentValue().Should().BeNull();
    }

    [Fact]
    public void Clock_invalid_init_value_mode_throws()
    {
        var clock = new Clock_t("clk")
        {
            InitValue = new InitValueClock("09:00:00"),
            LocalMktTz = "Etc/UTC",
            InitValueMode = 2,
        };
        var act = () => clock.LoadInitValue(FixFieldValueProvider.Empty);
        act.Should().Throw<InvalidFieldValueException>();
    }

    [Fact]
    public void Clock_has_no_enumerated_state()
    {
        var clock = new Clock_t("clk");
        clock.HasEnumeratedState.Should().BeFalse();
    }

    [Fact]
    public void Clock_local_mkt_tz_property_set_and_get()
    {
        var clock = new Clock_t("clk") { LocalMktTz = "America/New_York" };
        clock.LocalMktTz.Should().Be("America/New_York");
    }
}
```

Add any usings the file does not already have at its top: `using NodaTime;` and `using NodaTime.Testing;`. (`FixPortal.FixAtdl.Model.Controls`, the `InvalidFieldValueException`, and `FixFieldValueProvider` are already used by the existing file.)

- [ ] **Step 4: Run the new + updated tests to verify they fail**

Run: `dotnet test D:\fix-portal\fixportal-fixatdl\.claude\worktrees\reviewer-passes\tests\FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~ClockTimeZoneTests|FullyQualifiedName~ClockTimeProviderTests|FullyQualifiedName~ClockControlTests"`
Expected: FAIL to compile — `Clock_t.InitValue` is still `DateTime?`, `Clock_t.Clock` does not exist.

- [ ] **Step 5: Rewrite `Clock_t`**

Replace the **entire contents** of `src/FixPortal.FixAtdl/Model/Controls/Clock_t.cs`:

```csharp
// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
// FP Enhancement: 2026-05-31 — apply localMktTz via NodaTime; emit UTC at the wire boundary (batch 5, C1).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Globalization;
using FixPortal.FixAtdl.Diagnostics;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Controls.Support;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Resources;
using NodaTime;

namespace FixPortal.FixAtdl.Model.Controls;

/// <summary>
/// Represents the Clock_t control element within FIXatdl.
/// </summary>
/// <remarks>
/// A clock value is expressed in the market-local zone given by <see cref="LocalMktTz"/> and feeds a
/// UTCTimestamp_t FIX field, whose wire value must be UTC. This control is therefore the local→UTC bridge
/// (the only place that knows the market zone). It stores the resolved value as a NodaTime
/// <see cref="Instant"/> (a UTC point-in-time): <see cref="GetCurrentValue"/> returns the local-market
/// representation for display, while <see cref="ToDateTime(IParameter, IFormatProvider)"/> returns the UTC
/// instant for the wire. The BCL↔NodaTime seam is confined to this control and <see cref="InitValueClock"/>.
/// </remarks>
public class Clock_t : InitializableControl<InitValueClock?>
{
    private Instant? _value;

    /// <summary>
    /// Initializes a new instance of <see cref="Clock_t"/> using the supplied ID.
    /// </summary>
    /// <param name="id">ID for this control.</param>
    public Clock_t(string id)
        : base(id)
    {
    }

    /// <summary>The IANA/Olson zone in which initValue is represented. Required when initValue is supplied.
    /// Applicable when xsi:type is Clock_t. Null when not supplied in the ATDL.</summary>
    public string? LocalMktTz { get; set; }

    /// <summary>Defines the treatment of initValue time. 0: use initValue; 1: use current time if initValue time has passed.
    /// The default value is 0.</summary>
    public int? InitValueMode { get; set; }

    /// <summary>
    /// The clock used to read "now" (for <see cref="InitValueMode"/> == 1 and to determine the market's
    /// current date for a time-only initValue). Defaults to the system clock; assign a NodaTime FakeClock
    /// in tests. Set after reflective construction.
    /// </summary>
    public IClock Clock { get; set; } = SystemClock.Instance;

    /// <summary>
    /// The time-zone provider used to resolve <see cref="LocalMktTz"/>. Defaults to the TZDB provider.
    /// </summary>
    public IDateTimeZoneProvider TimeZoneProvider { get; set; } = DateTimeZoneProviders.Tzdb;

    #region InitializableControl<T> Overrides

    /// <summary>
    /// Attempts to load the supplied FIX field value (a UTC timestamp) into this control.
    /// </summary>
    /// <param name="value">Value to set this control to.</param>
    /// <returns>true if the supplied value could set this control; false otherwise.</returns>
    protected override bool LoadDefaultFromFixValue(string value)
    {
        bool parsed = FixDateTime.TryParse(value, CultureInfo.InvariantCulture, out DateTime result);

        _value = parsed ? ToInstant(result) : null;

        return parsed;
    }

    /// <summary>
    /// Loads this control from <see cref="InitializableControl{T}.InitValue"/>, converting the
    /// market-local time to a UTC instant via <see cref="LocalMktTz"/>. If no initValue was supplied the
    /// control value is left null.
    /// </summary>
    protected override void LoadDefaultFromInitValue()
    {
        // Surface an invalid initValueMode (only null/0/1 are defined) before anything else, rather than
        // silently treating anything that is not 1 as 0 (#4).
        if (InitValueMode is not (null or 0 or 1))
        {
            throw ThrowHelper.New<InvalidFieldValueException>(this, ErrorMessages.InitControlValueError,
                Id, string.Format(CultureInfo.InvariantCulture, "initValueMode '{0}' is invalid; expected 0 or 1", InitValueMode));
        }

        if (InitValue == null)
        {
            _value = null;
            return;
        }

        // FIXatdl requires localMktTz whenever initValue is supplied on a Clock_t. Without it the local→UTC
        // conversion is undefined; fail fast rather than emit a wrong instant (C1).
        if (string.IsNullOrEmpty(LocalMktTz))
        {
            throw ThrowHelper.New<InvalidFieldValueException>(this, ErrorMessages.InitControlValueError,
                Id, "localMktTz is required when initValue is supplied on a Clock_t control");
        }

        DateTimeZone? zone = TimeZoneProvider.GetZoneOrNull(LocalMktTz);

        if (zone == null)
        {
            throw ThrowHelper.New<InvalidFieldValueException>(this, ErrorMessages.InitControlValueError,
                Id, string.Format(CultureInfo.InvariantCulture, "localMktTz '{0}' is not a recognised IANA time zone", LocalMktTz));
        }

        Instant nowInstant = Clock.GetCurrentInstant();
        LocalDate marketToday = nowInstant.InZone(zone).Date;

        LocalDateTime localDt = InitValue.IsTimeOnly
            ? marketToday.At(InitValue.TimeOfDay!.Value)
            : InitValue.DateTime!.Value;

        // LenientResolver maps DST gaps forward and overlaps to the earlier offset, so resolution never
        // throws on a spring-forward / fall-back wall-clock time.
        Instant initInstant = zone.ResolveLocal(localDt, Resolvers.LenientResolver).ToInstant();

        // initValueMode 1: use "now" if the initValue instant has already passed. Comparison is on instants.
        _value = (InitValueMode == 1 && nowInstant > initInstant) ? nowInstant : initInstant;
    }

    #endregion

    #region Control_t Overrides

    /// <summary>
    /// Sets the value of this control using the value of the supplied parameter.
    /// </summary>
    /// <param name="parameter">Parameter to set this control's value from.</param>
    public override void SetValueFromParameter(IParameter parameter)
    {
        IControlConvertible value = parameter.GetValueForControl();

        DateTime? dateTime = value.ToDateTime();

        _value = dateTime == null ? null : ToInstant(dateTime.Value);
    }

    /// <summary>
    /// Sets the value of this control; either via a DateTime, or using the FIXatdl '{NULL}' value. This method
    /// is either called indirectly from the user interface, or by a StateRule.
    /// </summary>
    /// <param name="newValue">Either a valid DateTime or null (meaning do not send this value over FIX).
    /// May also contain the FIXatdl '{NULL}' value as a string.</param>
    public override void SetValue(object newValue)
    {
        bool isString = newValue is string;
        bool isDateTime = newValue is DateTime;

        if (isString)
        {
            string? value = newValue as string;

            if (value == Atdl.NullValue)
            {
                _value = null;
            }
            else if (FixDateTime.TryParse(value, CultureInfo.InvariantCulture, out DateTime parsed))
            {
                // Accept a serialized timestamp so the control can round-trip its own ToString output,
                // not just {NULL} (#3).
                _value = ToInstant(parsed);
            }
            else
            {
                throw ThrowHelper.New<InvalidFieldValueException>(this, ErrorMessages.InitControlValueError,
                    Id, string.Format(CultureInfo.InvariantCulture, "'{0}' is not a valid value for this control", value));
            }
        }
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
    }

    /// <summary>
    /// Resets this control to a null value.
    /// </summary>
    public override void Reset()
    {
        _value = null;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent nullable boolean value.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <returns>One of true, false or null which is equivalent to the value of this instance.</returns>
    public override bool? ToBoolean(IParameter targetParameter)
    {
        throw ThrowHelper.New<InvalidCastException>(this, ErrorMessages.UnsupportedControlValueConversion, _value, "Boolean", Id);
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent nullable decimal value using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <param name="provider">An <see cref="IFormatProvider"/> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>A nullable decimal equivalent to the value of this instance.</returns>
    public override decimal? ToDecimal(IParameter targetParameter, IFormatProvider provider)
    {
        throw ThrowHelper.New<InvalidCastException>(this, ErrorMessages.UnsupportedControlValueConversion, _value, "Decimal", Id);
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent 32-bit signed integer using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <param name="provider">An <see cref="IFormatProvider"/> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>A nullable 32-bit signed integer equivalent to the value of this instance.</returns>
    public override int? ToInt32(IParameter targetParameter, IFormatProvider provider)
    {
        throw ThrowHelper.New<InvalidCastException>(this, ErrorMessages.UnsupportedControlValueConversion, _value, "Int32", Id);
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent 32-bit unsigned integer using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <param name="provider">An <see cref="IFormatProvider"/> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>A nullable 32-bit unsigned integer equivalent to the value of this instance.</returns>
    public override uint? ToUInt32(IParameter targetParameter, IFormatProvider provider)
    {
        throw ThrowHelper.New<InvalidCastException>(this, ErrorMessages.UnsupportedControlValueConversion, _value, "UInt32", Id);
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent char value.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <returns>A nullable char value equivalent to the value of this instance. May be null.</returns>
    public override char? ToChar(IParameter targetParameter)
    {
        throw ThrowHelper.New<InvalidCastException>(this, ErrorMessages.UnsupportedControlValueConversion, _value, "Char", Id);
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent string value (the UTC wire representation, YYYYMMDD-HH:MM:SS).
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <returns>A string value equivalent to the value of this instance. May be null.</returns>
    public override string ToString(IParameter targetParameter)
    {
        return _value != null
            ? _value.Value.ToDateTimeUtc().ToString(FixDateTimeFormat.FixDateTime, CultureInfo.InvariantCulture)
            : null!;
    }

    /// <summary>
    /// Converts the value of this instance to the equivalent UTC <see cref="DateTime"/> for the FIX wire.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <param name="provider">An <see cref="IFormatProvider"/> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>The UTC instant (Kind = Utc), or null.</returns>
    public override DateTime? ToDateTime(IParameter targetParameter, IFormatProvider provider)
    {
        return _value?.ToDateTimeUtc();
    }

    /// <summary>
    /// Indicates whether the control has enumerated state.
    /// </summary>
    public override bool HasEnumeratedState => false;

    #endregion

    #region IValueProvider Members

    /// <summary>
    /// Gets the current value of this control (the LOCAL-market representation for display / Edits), for
    /// use in Edits as part of StateRules.
    /// </summary>
    /// <returns>Either a valid DateTime (local-market wall-clock when <see cref="LocalMktTz"/> is set,
    /// otherwise UTC) or null.</returns>
    public override object GetCurrentValue()
    {
        if (_value == null)
        {
            return null!;
        }

        if (!string.IsNullOrEmpty(LocalMktTz))
        {
            DateTimeZone? zone = TimeZoneProvider.GetZoneOrNull(LocalMktTz);

            if (zone != null)
            {
                return _value.Value.InZone(zone).ToDateTimeUnspecified();
            }
        }

        return _value.Value.ToDateTimeUtc();
    }

    #endregion

    /// <summary>
    /// Converts an inbound BCL <see cref="DateTime"/> (from a FIX wire value or a UI/StateRule set) to a
    /// NodaTime <see cref="Instant"/>. These values are UTC by convention; a Local value is converted and an
    /// Unspecified value is taken to be UTC.
    /// </summary>
    private static Instant ToInstant(DateTime dateTime) => dateTime.Kind switch
    {
        DateTimeKind.Utc => Instant.FromDateTimeUtc(dateTime),
        DateTimeKind.Local => Instant.FromDateTimeUtc(dateTime.ToUniversalTime()),
        _ => Instant.FromDateTimeUtc(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)),
    };
}
```

- [ ] **Step 6: Point the schema's Clock `initValue` at `InitValueClock`**

In `src/FixPortal.FixAtdl/Xml/SchemaDefinitions.cs`, the `ClockAttributes` array maps `initValue` to `typeof(DateTime)`. Change that one line:

```csharp
    private static readonly ElementAttribute[] ClockAttributes =
    [
        new ElementAttribute("initValue", "InitValue", typeof(Model.Controls.InitValueClock), Required.Optional),
        new ElementAttribute("initValueMode", "InitValueMode", typeof(int), Required.Optional),
        new ElementAttribute("localMktTz", "LocalMktTz", typeof(string), Required.Optional)
    ];
```

(`ValueConverter` returns the raw string for any `FixPortal.FixAtdl.Model.Controls.InitValue*` type; `SetPropertyValue` then invokes `InitValueClock(string)`. No converter/factory change needed.)

- [ ] **Step 7: Build, then run the full test suite**

Run: `dotnet build D:\fix-portal\fixportal-fixatdl\.claude\worktrees\reviewer-passes\FixPortal.FixAtdl.sln`
Expected: builds with **no new warnings**.

Run: `dotnet test D:\fix-portal\fixportal-fixatdl\.claude\worktrees\reviewer-passes\tests\FixPortal.FixAtdl.Tests`
Expected: PASS — the new `ClockTimeZoneTests`, the rewritten `ClockTimeProviderTests`, the updated `ClockControlTests`, and every pre-existing test green.

- [ ] **Step 8: Commit (this includes Task 1's project edits)**

```
git add Directory.Packages.props src/FixPortal.FixAtdl/FixPortal.FixAtdl.csproj tests/FixPortal.FixAtdl.Tests/FixPortal.FixAtdl.Tests.csproj src/FixPortal.FixAtdl/Model/Controls/Clock_t.cs src/FixPortal.FixAtdl/Xml/SchemaDefinitions.cs tests/FixPortal.FixAtdl.Tests/Controls/ClockTimeZoneTests.cs tests/FixPortal.FixAtdl.Tests/Controls/ClockTimeProviderTests.cs tests/FixPortal.FixAtdl.Tests/Model/Controls/ControlValueTests.cs
```
```
git commit -m "feat(controls): apply localMktTz via NodaTime IClock+TZDB so Clock_t emits correct UTC (C1)"
```

---

## Task 5: End-to-end deserialization of a Clock + localMktTz

Proves the schema wiring (`InitValueClock` ctor via the escape hatch) and the full parse→resolve→UTC path together, mirroring the broker-431 construct.

**Files:**
- Create: `tests/FixPortal.FixAtdl.Tests/Xml/ClockDeserializationTests.cs`

- [ ] **Step 1: Confirm the deserialization entrypoint used by the existing fixture tests**

Read `tests/FixPortal.FixAtdl.Tests/Fixtures` usage in the existing tests (e.g. the suite that loads `twap.xml`/`vwap.xml`) to find the public parse API (the `StrategiesReader` / loader the tests already call). Use that same API in Step 2. (Do not invent an API — match what the green fixture tests use.)

- [ ] **Step 2: Write the failing test**

Create `tests/FixPortal.FixAtdl.Tests/Xml/ClockDeserializationTests.cs`. Replace `LoadStrategies(xml)` below with the exact loader call discovered in Step 1; the assertion target is the deserialized `Clock_t`'s resolved UTC value.

```csharp
using System.Globalization;
using AwesomeAssertions;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Controls;
using NodaTime;
using NodaTime.Testing;

namespace FixPortal.FixAtdl.Tests.Xml;

/// <summary>
/// End-to-end: a Clock_t parsed from ATDL with initValue="08:00:00" localMktTz="Europe/Berlin" must
/// resolve to the correct UTC instant once a FakeClock pins the market date. Mirrors broker-431.
/// </summary>
public class ClockDeserializationTests
{
    [Fact]
    public void Clock_initValue_with_localMktTz_resolves_to_utc()
    {
        // Arrange: locate the deserialized Clock_t control (see Step 1 for the loader API) and inject a
        // FakeClock + TZDB before LoadInitValue so "today" is deterministic.
        Clock_t clock = DeserializeBerlinClockControl(); // implement via the existing loader from Step 1
        clock.Clock = new FakeClock(Instant.FromUtc(2026, 1, 15, 12, 0, 0));

        // Act
        clock.LoadInitValue(FixFieldValueProvider.Empty);

        // Assert — 08:00 Berlin (CET, UTC+1) -> 07:00Z.
        clock.ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should().Be(new DateTime(2026, 1, 15, 7, 0, 0, DateTimeKind.Utc));
    }
}
```

> **Implementer note:** `DeserializeBerlinClockControl()` is the only part to fill from Step 1's findings — build a minimal `<Strategies>` document containing a single `<Control xsi:type="lay:Clock_t" id="clk" initValue="08:00:00" localMktTz="Europe/Berlin" initValueMode="0" />` (matching the namespaces the existing fixtures use), run it through the same loader the green tests use, and return the `Clock_t`. This is **not** a placeholder for the assertion — the assertion is complete; only the loader call is environment-specific and must be copied from the existing passing fixture test rather than guessed.

- [ ] **Step 3: Run to verify it fails, then passes**

Run: `dotnet test D:\fix-portal\fixportal-fixatdl\.claude\worktrees\reviewer-passes\tests\FixPortal.FixAtdl.Tests --filter "FullyQualifiedName~ClockDeserializationTests"`
Expected before wiring the loader: compile error / FAIL. After implementing `DeserializeBerlinClockControl` against the real loader: PASS. The production code needs no further change — Task 4 already did the work; this only proves the parse path.

- [ ] **Step 4: Commit**

```
git add tests/FixPortal.FixAtdl.Tests/Xml/ClockDeserializationTests.cs
```
```
git commit -m "test(xml): pin Clock_t initValue+localMktTz deserialization to correct UTC (C1)"
```

---

## Self-Review

**Spec coverage (against the design doc §1):**
- §1.2 NodaTime adoption confined to the seam → Task 1 + 2 (only `Clock_t` / `InitValueClock` reference NodaTime). ✓
- §1.3 `IClock` + `IDateTimeZoneProvider` replace `TimeProvider` → Task 4. ✓
- §1.4 `initValue` parsed as `LocalTime`/`LocalDateTime`; `InitValue` type changed outright → Task 2 + Task 4 (base generic param). ✓
- §1.5 resolution algorithm (zone, marketToday, ResolveLocal/LenientResolver, instant compare for mode 1; throw on missing tz) → Task 4 `LoadDefaultFromInitValue`. ✓
- §1.6 wire boundary: `ToDateTime` = UTC, `GetCurrentValue` = local → Task 4. ✓
- §1.7 M1 parse styles → Task 3. ✓
- §1.8 C2 time-only **bound** on `UTCTimestamp_t` → **deferred to Phase C** (documented in Scope note); the Clock-initValue half of C2 is covered by Task 2's `LocalTime` parsing. ✓ (deliberate, not a gap)
- Conformance test C1 (DST boundary via FakeClock) → Task 4 `ClockTimeZoneTests` + Task 5 end-to-end. ✓

**Placeholder scan:** The only non-literal element is `DeserializeBerlinClockControl()` in Task 5, which is explicitly flagged to be filled from the existing fixture loader (Step 1) — the assertion itself is complete. Everything else is literal code/commands.

**Type consistency:** `InitValueClock.TimeOfDay : LocalTime?`, `.DateTime : LocalDateTime?`, `.IsTimeOnly : bool`, ctor `(string)` — used consistently in `Clock_t.LoadDefaultFromInitValue` and all tests. `Clock_t.Clock : IClock`, `Clock_t.TimeZoneProvider : IDateTimeZoneProvider`, `_value : Instant?` — consistent. `ToInstant(DateTime) : Instant` private helper used by all inbound paths. `ToDateTime` returns `DateTime?` (Kind=Utc); `GetCurrentValue` returns `object` (DateTime local). Schema target type `typeof(Model.Controls.InitValueClock)` matches the property type `InitValueClock?` (nullable annotation does not affect reflection). ✓

**Risk notes for the implementer:**
- `Instant.FromDateTimeUtc` throws if `Kind != Utc`; the `ToInstant` helper normalises Kind first — do not bypass it.
- `DateTimeZoneProviders.Tzdb` is the bundled TZDB; `Europe/Berlin` and `Etc/UTC` are always present. `GetZoneOrNull` returns null for unknown ids (no exception) — that path throws our domain error.
- If the pre-existing suite has any other site assigning a `DateTime` to `Clock_t.InitValue` beyond the two files listed, the build will flag it; update it the same way (wrap in `new InitValueClock("…")` + add `localMktTz`).
