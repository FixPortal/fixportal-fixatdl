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
}
