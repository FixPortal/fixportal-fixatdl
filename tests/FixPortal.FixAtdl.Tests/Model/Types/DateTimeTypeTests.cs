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
        // NOTE: The second format is accepted on input but output always uses format[0] (no ms).
        // A millisecond input is accepted and stored, then serialised without ms precision
        // unless the wire was originally ms-formatted.
        // Actually: ConvertToWireValueFormat uses format[0] = "yyyyMMdd-HH:mm:ss" always,
        // so ms precision is LOST on round-trip through WireValue getter.
        // Pinning actual behaviour: ms input → no-ms output.
        var p = new Parameter_t<UTCTimestamp_t>("Ts") { WireValue = "20260101-09:30:00.123" };
        // NOTE: ms precision is lost on output — format[0] is "yyyyMMdd-HH:mm:ss" (no ms).
        p.WireValue.Should().Be("20260101-09:30:00");
    }

    [Fact]
    public void UTCTimestamp_t_rejects_invalid_wire_value()
    {
        var p = new Parameter_t<UTCTimestamp_t>("Ts");
        var act = () => p.WireValue = "notadate";
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void UTCTimestamp_t_GetCurrentValue_returns_non_null_DateTime()
    {
        // NOTE: DateTime.TryParseExact with AssumeUniversal (without AdjustToUniversal) can return
        // Kind=Local on some platforms/TZ configurations. GetAdjustedValue then calls ToUniversalTime().
        // We pin the round-trip (WireValue) rather than the internal Kind, which is host-dependent.
        var p = new Parameter_t<UTCTimestamp_t>("Ts") { WireValue = "20260101-09:30:00" };
        var value = (DateTime?)p.GetCurrentValue();
        value.Should().NotBeNull();
        // The round-trip wire value is stable regardless of intermediate Kind.
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
        act.Should().Throw<InvalidCastException>();
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
        act.Should().Throw<InvalidCastException>();
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
    public void TZTimeOnly_t_rejects_truly_invalid_wire_value()
    {
        // NOTE: The 'K' specifier in "HH:mm:ssK" is OPTIONAL — "09:30:00" without a
        // timezone suffix is accepted by TryParseExact (treated as local/no-offset).
        // Only a completely unparseable string triggers rejection.
        var p = new Parameter_t<TZTimeOnly_t>("T");
        var act = () => p.WireValue = "notatime";
        act.Should().Throw<InvalidCastException>();
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
    public void TZTimestamp_t_rejects_truly_invalid_wire_value()
    {
        // NOTE: The 'K' specifier in "yyyyMMdd-HH:mm:ssK" is OPTIONAL — a value without
        // a timezone suffix is accepted by TryParseExact (treated as local/no-offset).
        // Only a completely unparseable string triggers rejection.
        var p = new Parameter_t<TZTimestamp_t>("Ts");
        var act = () => p.WireValue = "notadatetime";
        act.Should().Throw<InvalidCastException>();
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
        act.Should().Throw<InvalidCastException>();
    }
}
