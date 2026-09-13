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
        // R08: the zone is validated eagerly at assignment (the deserialization boundary), so a bad
        // zone can no longer lie dormant until the first bound check bricks every read/write of the
        // parameter - the setter itself is now the rejection point.
        var parameter = Param(null, "16:00:00");

        Action act = () => parameter.Value.LocalMktTz = "Mars/Phobos";

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
    public void Leap_second_bound_classifies_as_time_of_day_not_date_bearing()
    {
        // The classifier applies the parse path's leap-second normalisation: "08:00:60" rolls to
        // 08:01:00 and must become a recurring daily window, not a one-off date-time bound (the
        // far-future value dates pin date-independence).
        var p = Param(minText: "08:00:60", maxText: null);

        var before = () => p.WireValue = "20990101-08:00:30";
        before.Should().Throw<InvalidFieldValueException>();

        var at = () => p.WireValue = "20990101-08:01:00";
        at.Should().NotThrow();
    }

    [Fact]
    public void High_precision_fraction_bound_classifies_as_time_of_day_not_date_bearing()
    {
        // Excess fraction digits truncate to tick precision (the parse path's rule) and the bound
        // stays a recurring daily window; previously this was misclassified as date-bearing.
        var p = Param(minText: "08:00:00.123456789", maxText: null);

        var before = () => p.WireValue = "20990101-08:00:00.123";
        before.Should().Throw<InvalidFieldValueException>();

        var after = () => p.WireValue = "20990101-08:00:00.124";
        after.Should().NotThrow();
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

    [Theory]
    [InlineData("20260101-21:00:00", true)]
    [InlineData("20260101-21:00:01", false)]
    public void Offset_anchored_bound_with_trailing_whitespace_is_still_recognised(string wire, bool valid)
    {
        // The bound parse tolerates surrounding whitespace (AllowWhiteSpaces); the offset-anchoring
        // classification must trim before matching too, or this bound is misread as market-local and
        // 21:00:01Z passes against the 16:00 EST wall clock.
        var p = Param(minText: null, maxText: "21:00:00Z ");
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

    // ──────────────────────────────────────────────────────────────────────────
    // Date-bearing bound classification (R04): a bound carrying a date is classified
    // from the parse result (FixDateTime.IsTimeOnlyText), not the previous "first
    // eight characters are digits" heuristic, which degraded ISO-8601, date-only and
    // leading-space bounds to a recurring daily time-of-day window.
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("2026-06-01T12:00:00Z")] // ISO-8601
    [InlineData(" 20260601-12:00:00")] // leading-space FIX timestamp
    public void Date_bearing_max_bound_is_not_degraded_to_a_daily_window(string maxText)
    {
        var p = Param(minText: null, maxText: maxText);

        // A year before the bound date is within the bound even though 23:59:59 is after the
        // bound's time-of-day: the date component must rule, not a recurring daily window.
        var earlyAct = () => p.WireValue = "20250531-23:59:59";
        earlyAct.Should().NotThrow();

        // After the bound instant: rejected.
        var lateAct = () => p.WireValue = "20260601-13:00:00";
        lateAct.Should().Throw<InvalidFieldValueException>();
    }

    [Fact]
    public void Date_only_max_bound_is_not_erased()
    {
        var p = Param(minText: null, maxText: "2026-06-01");

        // Before the bound date: within.
        var earlyAct = () => p.WireValue = "20250531-23:59:59";
        earlyAct.Should().NotThrow();

        // After the bound date: rejected (the bound previously became a 00:00:00 daily window).
        var lateAct = () => p.WireValue = "20260602-00:00:01";
        lateAct.Should().Throw<InvalidFieldValueException>();
    }
}
