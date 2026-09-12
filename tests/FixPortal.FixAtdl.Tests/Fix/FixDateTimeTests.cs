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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Null_or_empty_input_returns_false_instead_of_throwing(string? value)
    {
        // A null can reach here via a programmatically stored null in FixTagValuesCollection; a Try
        // method must report failure, not throw.
        FixDateTime.TryParse(value!, CultureInfo.InvariantCulture, out _).Should().BeFalse();
    }

    [Fact]
    public void Nanosecond_fraction_is_truncated_to_tick_precision()
    {
        // DateTime resolves fractions to 7 tick digits; a FIX timestamp carrying nanosecond
        // precision beyond that must not be rejected outright.
        FixDateTime
            .TryParse("20260601-09:30:00.123456789", CultureInfo.InvariantCulture, out DateTime result)
            .Should()
            .BeTrue();

        result.Should().Be(new DateTime(2026, 6, 1, 9, 30, 0, DateTimeKind.Utc).AddTicks(1234567));
    }

    [Fact]
    public void Leap_second_beyond_the_last_representable_instant_returns_false_instead_of_throwing()
    {
        // 9999-12-31 23:59:60 normalises to the final representable second; rolling forward one
        // second would pass DateTime.MaxValue, so the Try method reports failure.
        FixDateTime.TryParse("99991231-23:59:60", CultureInfo.InvariantCulture, out _).Should().BeFalse();
    }

    [Fact]
    public void AllFormats_does_not_hand_out_the_mutable_backing_array()
    {
        FixDateTimeFormat.AllFormats.Should().NotBeEmpty();

        Func<object> act = () => (string[])FixDateTimeFormat.AllFormats;

        act.Should().Throw<InvalidCastException>();
    }
}
