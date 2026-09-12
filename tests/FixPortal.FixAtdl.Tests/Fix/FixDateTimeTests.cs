using System.Globalization;
using FixPortal.FixAtdl.Fix;

namespace FixPortal.FixAtdl.Tests.Fix;

public class FixDateTimeTests
{
    [Fact]
    public void Offsetless_fix_timestamp_parses_as_utc_kind()
    {
        FixDateTime.TryParse("20260601-08:00:00", CultureInfo.InvariantCulture, out DateTime result).Should().BeTrue();

        result.Kind.Should().Be(DateTimeKind.Utc);
        result.Should().Be(new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Fallback_parsed_value_is_also_utc_kind()
    {
        // An ISO-8601 value is not one of the exact FIX formats, so it falls through to the loose parse.
        // That fallback must still yield a canonical Kind=Utc result (previously it returned Unspecified).
        FixDateTime
            .TryParse("2026-06-01T08:00:00", CultureInfo.InvariantCulture, out DateTime result)
            .Should()
            .BeTrue();

        result.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Leap_second_timestamp_rolls_forward_into_the_next_day()
    {
        // The UTCTimestamp_t spec's own worked example: a declared leap second at year-end rolls into
        // the next day (and, incidentally, the next year).
        FixDateTime
            .TryParse("19981231-23:59:60", CultureInfo.InvariantCulture, out DateTime result)
            .Should()
            .BeTrue();

        result.Should().Be(new DateTime(1999, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        result.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Leap_second_timestamp_with_milliseconds_rolls_forward_preserving_the_fraction()
    {
        FixDateTime
            .TryParse("19981231-23:59:60.250", CultureInfo.InvariantCulture, out DateTime result)
            .Should()
            .BeTrue();

        result.Should().Be(new DateTime(1999, 1, 1, 0, 0, 0, 250, DateTimeKind.Utc));
    }

    [Fact]
    public void Leap_second_time_only_rolls_forward_into_the_next_minute()
    {
        FixDateTime.TryParse("12:00:60", CultureInfo.InvariantCulture, out DateTime result).Should().BeTrue();

        result.TimeOfDay.Should().Be(new TimeSpan(12, 1, 0));
    }

    [Fact]
    public void Minute_field_of_60_is_not_mistaken_for_a_leap_second()
    {
        // "60" only ever denotes a leap second in the SECONDS field. A malformed minutes field of "60"
        // must not be silently normalised - it should fail to parse like any other invalid FIX value.
        FixDateTime.TryParse("20260601-08:60:00", CultureInfo.InvariantCulture, out _).Should().BeFalse();
    }

    [Fact]
    public void Ordinary_second_of_59_is_unaffected()
    {
        FixDateTime.TryParse("20260601-23:59:59", CultureInfo.InvariantCulture, out DateTime result).Should().BeTrue();

        result.Should().Be(new DateTime(2026, 6, 1, 23, 59, 59, DateTimeKind.Utc));
    }
}
