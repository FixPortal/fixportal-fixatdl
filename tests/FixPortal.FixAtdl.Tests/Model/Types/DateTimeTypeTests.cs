using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Types;

namespace FixPortal.FixAtdl.Tests.Model.Types;

/// <summary>
/// Characterizes the wire-format round-trips for all FIXatdl date/time types.
/// Wire format constants are from FixDateTimeFormat: "yyyyMMdd-HH:mm:ss", "HH:mm:ss",
/// "yyyyMMdd", "HH:mm:ssK", "yyyyMMdd-HH:mm:ssK".
/// </summary>
public class DateTimeTypeTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // UTCTimestamp_t  format: yyyyMMdd-HH:mm:ss  (primary)
    //                         yyyyMMdd-HH:mm:ss.fff  (milliseconds)
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("20260101-09:30:00")]
    [InlineData("20261231-23:59:59")]
    public void UTCTimestamp_t_round_trips_whole_second_wire_value(string wire)
    {
        var p = new Parameter_t<UTCTimestamp_t>("Ts") { WireValue = wire };
        p.WireValue.Should().Be(wire);
    }

    [Fact]
    public void UTCTimestamp_t_round_trips_millisecond_wire_value()
    {
        // MS-aware round-tripping: milliseconds are preserved when present.
        var p = new Parameter_t<UTCTimestamp_t>("Ts") { WireValue = "20260101-09:30:00.123" };
        p.WireValue.Should().Be("20260101-09:30:00.123");
    }

    [Fact]
    public void UTCTimeOnly_t_round_trips_millisecond_wire_value()
    {
        // MS-aware round-tripping: milliseconds are preserved when present.
        var p = new Parameter_t<UTCTimeOnly_t>("T") { WireValue = "09:30:00.123" };
        p.WireValue.Should().Be("09:30:00.123");
    }

    [Fact]
    public void UTCTimestamp_t_rejects_invalid_wire_value()
    {
        var p = new Parameter_t<UTCTimestamp_t>("Ts");
        var act = () => p.WireValue = "notadate";
        act.Should().Throw<FixPortal.FixAtdl.Diagnostics.Exceptions.InvalidFieldValueException>();
    }

    [Fact]
    public void UTCTimestamp_t_GetCurrentValue_returns_non_null_DateTime()
    {
        // NOTE: UTC wire parse now uses AssumeUniversal | AdjustToUniversal, so the parsed
        // DateTime always carries Kind=Utc — no longer host-dependent.
        // This test pins both the wire round-trip and the canonical Kind=Utc contract.
        var p = new Parameter_t<UTCTimestamp_t>("Ts") { WireValue = "20260101-09:30:00" };
        var value = p.GetCurrentValue();
        value.Should().BeOfType<DateTime>().Which.Kind.Should().Be(DateTimeKind.Utc);
        p.WireValue.Should().Be("20260101-09:30:00");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UTCTimeOnly_t  format: HH:mm:ss
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("09:30:00")]
    [InlineData("23:59:59")]
    [InlineData("00:00:00")]
    public void UTCTimeOnly_t_round_trips_valid_wire_value(string wire)
    {
        var p = new Parameter_t<UTCTimeOnly_t>("T") { WireValue = wire };
        p.WireValue.Should().Be(wire);
    }

    [Fact]
    public void UTCTimeOnly_t_rejects_invalid_wire_value()
    {
        var p = new Parameter_t<UTCTimeOnly_t>("T");
        var act = () => p.WireValue = "notatime";
        act.Should().Throw<FixPortal.FixAtdl.Diagnostics.Exceptions.InvalidFieldValueException>();
    }

    [Fact]
    public void UTCTimeOnly_t_normalises_time_only_values_to_a_stable_date()
    {
        var p = new Parameter_t<UTCTimeOnly_t>("T") { WireValue = "09:30:00" };

        var value = p.GetCurrentValue();

        value.Should().BeOfType<DateTime>().Which.Date.Should().Be(new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UTCDateOnly_t  format: yyyyMMdd
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("20260101")]
    [InlineData("20261231")]
    public void UTCDateOnly_t_round_trips_valid_wire_value(string wire)
    {
        var p = new Parameter_t<UTCDateOnly_t>("D") { WireValue = wire };
        p.WireValue.Should().Be(wire);
    }

    [Fact]
    public void UTCDateOnly_t_rejects_invalid_wire_value()
    {
        var p = new Parameter_t<UTCDateOnly_t>("D");
        var act = () => p.WireValue = "2026-01-01";
        act.Should().Throw<FixPortal.FixAtdl.Diagnostics.Exceptions.InvalidFieldValueException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // TZTimeOnly_t  format: HH:mm:ssK
    // NOTE: WireParseStyles = AssumeUniversal | AdjustToUniversal.
    // The UTC offset is normalised to UTC on parse; the emitted form uses 'K'
    // which renders as "Z" for a UTC DateTime, so a non-Z input (e.g. "-05:00")
    // is stored as UTC and emitted as "Z". Only a Z input round-trips unchanged.
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TZTimeOnly_t_round_trips_UTC_Z_suffix()
    {
        var p = new Parameter_t<TZTimeOnly_t>("T") { WireValue = "09:30:00Z" };
        p.WireValue.Should().Be("09:30:00Z");
    }

    [Fact]
    public void TZTimeOnly_t_normalises_offset_to_UTC_Z()
    {
        // NOTE: "02:39:00-05:00" (ET) is normalised to UTC on parse: 07:39:00Z.
        // The original offset "-05:00" is NOT preserved — only the UTC instant is.
        var p = new Parameter_t<TZTimeOnly_t>("T") { WireValue = "02:39:00-05:00" };
        p.WireValue.Should().Be("07:39:00Z");
    }

    [Fact]
    public void TZTimeOnly_t_accepts_documented_optional_seconds_and_bare_hour_offsets()
    {
        var p = new Parameter_t<TZTimeOnly_t>("T") { WireValue = "15:39+08" };

        p.WireValue.Should().Be("07:39:00Z");
        ((DateTime?)p.GetCurrentValue()).Value.Date.Should().Be(new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void TZTimeOnly_t_accepts_fractional_seconds_with_offsets()
    {
        var p = new Parameter_t<TZTimeOnly_t>("T") { WireValue = "13:09:00.123456+05:30" };

        p.WireValue.Should().Be("07:39:00.123456Z");
    }

    [Fact]
    public void TZTimeOnly_t_accepts_whole_second_bare_hour_offsets()
    {
        var p = new Parameter_t<TZTimeOnly_t>("T") { WireValue = "02:39:00-05" };

        p.WireValue.Should().Be("07:39:00Z");
    }

    [Fact]
    public void TZTimeOnly_t_rejects_truly_invalid_wire_value()
    {
        // NOTE: The 'K' specifier in "HH:mm:ssK" is OPTIONAL — "09:30:00" without a
        // timezone suffix is accepted by TryParseExact (treated as local/no-offset).
        // Only a completely unparseable string triggers rejection.
        var p = new Parameter_t<TZTimeOnly_t>("T");
        var act = () => p.WireValue = "notatime";
        act.Should().Throw<FixPortal.FixAtdl.Diagnostics.Exceptions.InvalidFieldValueException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // TZTimestamp_t  format: yyyyMMdd-HH:mm:ssK
    // NOTE: same UTC-normalisation as TZTimeOnly_t.
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TZTimestamp_t_round_trips_UTC_Z_suffix()
    {
        var p = new Parameter_t<TZTimestamp_t>("Ts") { WireValue = "20060901-07:39:00Z" };
        p.WireValue.Should().Be("20060901-07:39:00Z");
    }

    [Fact]
    public void TZTimestamp_t_normalises_offset_to_UTC_Z()
    {
        // NOTE: "20060901-02:39:00-05:00" normalised: 02:39 + 5h = 07:39 UTC on same date.
        var p = new Parameter_t<TZTimestamp_t>("Ts") { WireValue = "20060901-02:39:00-05:00" };
        p.WireValue.Should().Be("20060901-07:39:00Z");
    }

    [Fact]
    public void TZTimestamp_t_accepts_documented_bare_hour_offsets()
    {
        var p = new Parameter_t<TZTimestamp_t>("Ts") { WireValue = "20060901-15:39+08" };

        p.WireValue.Should().Be("20060901-07:39:00Z");
    }

    [Fact]
    public void TZTimestamp_t_accepts_fractional_seconds_with_offsets()
    {
        var p = new Parameter_t<TZTimestamp_t>("Ts") { WireValue = "20060901-13:09:00.123456+05:30" };

        p.WireValue.Should().Be("20060901-07:39:00.123456Z");
    }

    [Fact]
    public void TZTimestamp_t_accepts_whole_second_bare_hour_offsets()
    {
        var p = new Parameter_t<TZTimestamp_t>("Ts") { WireValue = "20060901-02:39:00-05" };

        p.WireValue.Should().Be("20060901-07:39:00Z");
    }

    [Fact]
    public void TZTimestamp_t_rejects_truly_invalid_wire_value()
    {
        // NOTE: The 'K' specifier in "yyyyMMdd-HH:mm:ssK" is OPTIONAL — a value without
        // a timezone suffix is accepted by TryParseExact (treated as local/no-offset).
        // Only a completely unparseable string triggers rejection.
        var p = new Parameter_t<TZTimestamp_t>("Ts");
        var act = () => p.WireValue = "notadatetime";
        act.Should().Throw<FixPortal.FixAtdl.Diagnostics.Exceptions.InvalidFieldValueException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // LocalMktDate_t  format: yyyyMMdd (no UTC adjustment)
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("20260101")]
    [InlineData("20261231")]
    public void LocalMktDate_t_round_trips_valid_wire_value(string wire)
    {
        var p = new Parameter_t<LocalMktDate_t>("D") { WireValue = wire };
        p.WireValue.Should().Be(wire);
    }

    [Fact]
    public void LocalMktDate_t_rejects_invalid_wire_value()
    {
        var p = new Parameter_t<LocalMktDate_t>("D");
        var act = () => p.WireValue = "baddate";
        act.Should().Throw<FixPortal.FixAtdl.Diagnostics.Exceptions.InvalidFieldValueException>();
    }

    [Fact]
    public void DateTimeTypeBase_SetBound_clears_alternative_slots()
    {
        var param = new Parameter_t<UTCTimestamp_t>("Ts");

        // 1. Set time-only bound
        param.Value.MaxValueText = "12:00:00";
        param.Value.MaxValue.Should().BeNull();

        // 2. Set datetime bound
        param.Value.MaxValueText = "20260602-15:00:00";
        param.Value.MaxValue.Should().NotBeNull();

        // Check validation of 13:00:00 on that date (valid because 13:00:00 < 15:00:00, and 12:00:00 time-only is cleared)
        var actValid = () => param.WireValue = "20260602-13:00:00";
        actValid.Should().NotThrow();

        // 3. Set time-only bound again, check that datetime bound is cleared
        param.Value.MaxValueText = "12:00:00";
        param.Value.MaxValue.Should().BeNull();

        // Now 13:00:00 should fail because 13:00:00 > 12:00:00 (time-only bound)
        var actInvalid = () => param.WireValue = "20260602-13:00:00";
        actInvalid.Should().Throw<FixPortal.FixAtdl.Diagnostics.Exceptions.InvalidFieldValueException>();
    }

    [Fact]
    public void DateTimeTypeBase_SetBound_handles_offset_bearing_and_minute_only_bounds()
    {
        var param = new Parameter_t<UTCTimeOnly_t>("T");

        // 1. Set offset-bearing bound "12:00:00+01:00" -> which is 11:00:00 UTC.
        param.Value.MaxValueText = "12:00:00+01:00";
        
        // 10:30:00 UTC should succeed (10:30:00 < 11:00:00)
        var act1 = () => param.WireValue = "10:30:00";
        act1.Should().NotThrow();

        // 11:30:00 UTC should throw (11:30:00 > 11:00:00)
        var act2 = () => param.WireValue = "11:30:00";
        act2.Should().Throw<FixPortal.FixAtdl.Diagnostics.Exceptions.InvalidFieldValueException>();

        // 2. Set minute-only bound "12:30"
        param.Value.MaxValueText = "12:30";

        // 12:00:00 UTC should succeed
        var act3 = () => param.WireValue = "12:00:00";
        act3.Should().NotThrow();

        // 13:00:00 UTC should throw
        var act4 = () => param.WireValue = "13:00:00";
        act4.Should().Throw<FixPortal.FixAtdl.Diagnostics.Exceptions.InvalidFieldValueException>();
    }
}
