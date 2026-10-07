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
    public void Explicit_offset_fix_timestamp_converts_to_the_equivalent_utc_instant()
    {
        // "yyyyMMdd-HH:mm:sszz" is one of the exact FIX formats and carries a literal
        // hour offset; the parser must convert it to the equivalent UTC instant (not
        // just relabel the local wall-clock value as UTC).
        FixDateTime
            .TryParse("20260601-10:00:00+02:00", CultureInfo.InvariantCulture, out DateTime result)
            .Should()
            .BeTrue();

        result.Kind.Should().Be(DateTimeKind.Utc);
        result.Should().Be(new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Explicit_offset_fallback_timestamp_converts_to_the_equivalent_utc_instant()
    {
        // An ISO-8601 value with an explicit offset is not one of the exact FIX formats,
        // so it falls through to the loose parse. That fallback must convert the offset
        // to the equivalent UTC instant too, not just stamp Kind=Utc onto the local value.
        FixDateTime
            .TryParse("2026-06-01T10:00:00+02:00", CultureInfo.InvariantCulture, out DateTime result)
            .Should()
            .BeTrue();

        result.Kind.Should().Be(DateTimeKind.Utc);
        result.Should().Be(new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc));
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

    [Theory]
    [InlineData("12:00:60")]
    [InlineData("20260601-12:00:60")]
    public void Leap_second_at_any_minute_other_than_23_59_is_rejected(string value)
    {
        // UTCTimestamp_t permits a declared leap second only as the 60th second of 23:59 UTC. A ":60"
        // at any other minute is invalid and must fail outright, not be silently normalised and
        // shifted forward into the next minute.
        FixDateTime.TryParse(value, CultureInfo.InvariantCulture, out _).Should().BeFalse();
    }

    [Fact]
    public void Parse_throws_FormatException_for_a_60_seconds_field_outside_23_59()
    {
        var act = () => FixDateTime.Parse("12:00:60", CultureInfo.InvariantCulture);

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Leap_second_time_only_rolls_forward_then_reanchors_to_0001_01_01()
    {
        // Roll the leap second first, then re-anchor. Pinning first landed midnight on 0001-01-02,
        // which no longer matched a wire UTCTimeOnly of 00:00:00.
        FixDateTime.TryParse("23:59:60", CultureInfo.InvariantCulture, out DateTime result).Should().BeTrue();

        result.Should().Be(new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Leap_second_time_only_with_milliseconds_reanchors_after_the_roll()
    {
        FixDateTime.TryParse("23:59:60.250", CultureInfo.InvariantCulture, out DateTime result).Should().BeTrue();

        result.Should().Be(new DateTime(1, 1, 1, 0, 0, 0, 250, DateTimeKind.Utc));
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
        FixDateTime.TryParse(value, CultureInfo.InvariantCulture, out _).Should().BeFalse();
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

    [Theory]
    [InlineData("20260601-09:30:00.1234", 1234000L)]
    [InlineData("20260601-09:30:00.1234567", 1234567L)]
    public void Offsetless_fractional_timestamp_with_four_to_seven_digits_parses_exactly(string value, long ticks)
    {
        FixDateTime.TryParse(value, CultureInfo.InvariantCulture, out DateTime result).Should().BeTrue();

        result.Kind.Should().Be(DateTimeKind.Utc);
        result.Should().Be(new DateTime(2026, 6, 1, 9, 30, 0, DateTimeKind.Utc).AddTicks(ticks));
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

    [Fact]
    public void Parse_throws_FormatException_for_unparseable_input()
    {
        // FormatException is the conventional parse-failure type, so callers catching it around date
        // parsing catch this too (previously an InvalidCastException escaped such handlers).
        var act = () => FixDateTime.Parse("not-a-date", CultureInfo.InvariantCulture);

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Exact_fix_timestamp_parses_identically_under_a_non_colon_time_separator_culture()
    {
        // fi-FI's TimeSeparator is '.'; if the exact-format parse resolved the ':' in the FIX format
        // strings against the caller's culture, a valid FIX timestamp would fail under it. The FIX wire
        // format is culture-fixed, so the exact parse must not consult the caller's culture.
        CultureInfo finnish = CultureInfo.GetCultureInfo("fi-FI");

        FixDateTime.TryParse("20260601-08:00:00", finnish, out DateTime finnishResult).Should().BeTrue();
        FixDateTime
            .TryParse("20260601-08:00:00", CultureInfo.InvariantCulture, out DateTime invariantResult)
            .Should()
            .BeTrue();

        finnishResult.Should().Be(invariantResult);
        finnishResult.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Time_only_value_is_anchored_to_a_fixed_date_with_utc_kind()
    {
        // A date-less FIX value must not pick up the host's current date (which would make the result
        // vary by day and, via AdjustToUniversal, by host timezone); it is pinned to 0001-01-01 Utc.
        FixDateTime.TryParse("12:00:00", CultureInfo.InvariantCulture, out DateTime result).Should().BeTrue();

        result.Should().Be(new DateTime(1, 1, 1, 12, 0, 0, DateTimeKind.Utc));
    }
}
