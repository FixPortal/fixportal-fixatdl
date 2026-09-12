using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Types;

namespace FixPortal.FixAtdl.Tests.Validation;

/// <summary>
/// Pins C2: a time-only <c>minValue</c>/<c>maxValue</c> on a timestamp parameter constrains the
/// UTC time-of-day of the value regardless of the value's calendar date (a bare <c>HH:mm:ss</c>
/// bound is a time-of-day constraint, not a date+time one), while a full-datetime bound continues
/// to compare as a complete <see cref="DateTime"/>.
/// </summary>
public class TimestampBoundConformanceTests
{
    // FIXatdl 1.1 with Errata 20101221, pp. 32–33: timestamp bounds are daily
    // local-market times, qualified by Parameter/@localMktTz.
    [Theory]
    [InlineData("America/New_York", "20260101-21:00:00", "09:00:00", "16:00:00", true)]
    [InlineData("America/New_York", "20260701-20:00:00", "09:00:00", "16:00:00", true)]
    [InlineData("America/New_York", "20260101-21:00:01", "09:00:00", "16:00:00", false)]
    [InlineData("America/New_York", "20260701-20:00:01", "09:00:00", "16:00:00", false)]
    [InlineData("America/New_York", "20260101-13:59:59", "09:00:00", "16:00:00", false)]
    [InlineData("America/New_York", "20260701-12:59:59", "09:00:00", "16:00:00", false)]
    [InlineData("Pacific/Auckland", "20260101-20:00:00", "09:00:00", "09:00:00", true)]
    [InlineData("America/New_York", "20261101-05:30:00", "01:00:00", "01:45:00", true)]
    [InlineData("America/New_York", "20261101-06:30:00", "01:00:00", "01:45:00", true)]
    public void Timestamp_bounds_use_market_wall_time_on_the_timestamp_date(
        string zone,
        string wire,
        string min,
        string max,
        bool valid
    )
    {
        var parameter = Param(min, max);
        parameter.Value.LocalMktTz = zone;

        var act = () => parameter.WireValue = wire;

        if (valid)
        {
            act.Should().NotThrow();
            parameter.WireValue.Should().Be(wire, "bounds must not change UTC wire serialization");
        }
        else
        {
            act.Should().Throw<InvalidFieldValueException>();
        }
    }

    [Fact]
    public void Timestamp_bound_with_unknown_market_zone_is_rejected()
    {
        var parameter = Param(null, "16:00:00");
        parameter.Value.LocalMktTz = "Mars/Phobos";

        var act = () => parameter.WireValue = "20260101-12:00:00";

        act.Should().Throw<InvalidFieldValueException>();
    }

    private static Parameter_t<UTCTimestamp_t> Param(string? minText, string? maxText)
    {
        var p = new Parameter_t<UTCTimestamp_t>("ts");
        if (minText != null)
        {
            p.Value.MinValueText = minText;
        }
        if (maxText != null)
        {
            p.Value.MaxValueText = maxText;
        }
        return p;
    }

    [Theory]
    [InlineData("20260101-09:00:00")]
    [InlineData("20991231-09:00:00")]
    public void Time_only_min_bound_accepts_value_at_or_after_time_of_day(string wireValue)
    {
        var p = Param(minText: "08:00:00", maxText: null);
        var act = () => p.WireValue = wireValue;
        act.Should().NotThrow();
    }

    [Fact]
    public void Time_only_min_bound_rejects_value_before_time_of_day()
    {
        var p = Param(minText: "08:00:00", maxText: null);
        var act = () => p.WireValue = "20260101-07:00:00";
        act.Should().Throw<InvalidFieldValueException>();
    }

    [Fact]
    public void Full_datetime_max_bound_still_compares_as_datetime()
    {
        var p = Param(minText: null, maxText: "20260601-12:00:00");
        var act = () => p.WireValue = "20260601-13:00:00";
        act.Should().Throw<InvalidFieldValueException>();
    }

    [Fact]
    public void Time_only_max_bound_rejects_value_after_time_of_day()
    {
        var p = Param(minText: null, maxText: "16:00:00");
        var act = () => p.WireValue = "20260101-17:00:00";
        act.Should().Throw<InvalidFieldValueException>();
    }

    [Theory]
    [InlineData("20260101-21:00:00", true)] // 21:00Z == the bound's own UTC anchor - at the boundary.
    [InlineData("20260701-21:00:00", true)] // Same UTC instant in EDT (16:00 EDT local) - offset ignores DST.
    [InlineData("20260101-21:00:01", false)]
    public void Offset_anchored_max_bound_compares_against_the_values_own_utc_time_not_zone_local(
        string wire,
        bool valid
    )
    {
        // maxValue carries its own explicit UTC offset - it must be compared against the value's UTC
        // time-of-day directly, not the parameter's zone-local (America/New_York) time-of-day, or a
        // fixed-offset bound and a DST-aware zone conversion would silently disagree.
        var p = Param(minText: null, maxText: "21:00:00Z");
        p.Value.LocalMktTz = "America/New_York";

        var act = () => p.WireValue = wire;

        if (valid)
        {
            act.Should().NotThrow();
        }
        else
        {
            act.Should().Throw<InvalidFieldValueException>();
        }
    }

    [Theory]
    [InlineData("20260101-21:00:00", true)] // 21:00Z == the bound's own UTC anchor (bare-hour "-00" offset).
    [InlineData("20260101-21:00:01", false)]
    public void Bare_hour_offset_max_bound_is_also_recognised_as_offset_anchored(string wire, bool valid)
    {
        // "-05" (FixTimeOnlyWithHourOffset's "zz" format) is a bare 2-digit hour offset with no minutes -
        // distinct from the 4-digit "+05:30"/"Z" forms covered above, and the specific gap Gitar flagged.
        var p = Param(minText: null, maxText: "16:00:00-05");
        p.Value.LocalMktTz = "America/New_York";

        var act = () => p.WireValue = wire;

        if (valid)
        {
            act.Should().NotThrow();
        }
        else
        {
            act.Should().Throw<InvalidFieldValueException>();
        }
    }

    [Fact]
    public void Min_and_max_bounds_may_independently_carry_or_omit_an_offset()
    {
        // minValue is plain (market-local, per the spec's worked example); maxValue carries its own
        // explicit offset. Each bound is anchored in its own frame on the same parameter.
        var p = Param(minText: "09:00:00", maxText: "21:00:00Z");
        p.Value.LocalMktTz = "America/New_York";

        // 20:00Z on 2026-01-01 is 15:00 EST - within [09:00 EST, 21:00Z] on both frames.
        var act = () => p.WireValue = "20260101-20:00:00";
        act.Should().NotThrow();
    }

    [Fact]
    public void Reassigning_MaxValueText_to_a_plain_bound_clears_the_offset_anchoring_flag()
    {
        var p = Param(minText: null, maxText: "21:00:00Z");
        p.Value.LocalMktTz = "America/New_York";

        // Overwrite the offset-anchored text bound with a plain (market-local) one - the earlier bound's
        // offset-anchoring must not stick around and get applied to this new bound (TY1-G-shaped: paired
        // with the existing _maxTimeOfDay reset).
        p.Value.MaxValueText = "16:00:00";

        // 21:00Z on 2026-01-01 is 16:00 EST - at the plain (zone-local) bound, not the old UTC anchor.
        var act = () => p.WireValue = "20260101-21:00:00";
        act.Should().NotThrow();
    }
}
