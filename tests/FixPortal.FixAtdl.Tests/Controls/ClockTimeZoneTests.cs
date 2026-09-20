using System.Globalization;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Controls;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Types;
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
    private static Clock_t BerlinClock(InitValueClock initValue, Instant now, int? mode = 0) =>
        new("clk")
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

        clock
            .ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should()
            .Be(new DateTime(2026, 1, 15, 7, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Berlin_0800_in_summer_emits_0600_utc()
    {
        // 2026-07-15 — CEST (UTC+2). 08:00 Berlin -> 06:00Z.
        var clock = BerlinClock(new InitValueClock("08:00:00"), Instant.FromUtc(2026, 7, 15, 12, 0, 0));

        clock.LoadInitValue(FixFieldValueProvider.Empty);

        clock
            .ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should()
            .Be(new DateTime(2026, 7, 15, 6, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void GetCurrentValue_returns_local_market_time_for_display()
    {
        var clock = BerlinClock(new InitValueClock("08:00:00"), Instant.FromUtc(2026, 1, 15, 12, 0, 0));

        clock.LoadInitValue(FixFieldValueProvider.Empty);

        // Local display is 08:00 Berlin wall-clock (DateTime equality is Kind-insensitive).
        // GetCurrentValue returns ToDateTimeUnspecified, so match its Kind.
        ((DateTime)clock.GetCurrentValue())
            .Should()
            .Be(new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Unspecified));
    }

    [Fact]
    public void Nanosecond_initValue_is_accepted_and_emitted_truncated_to_milliseconds()
    {
        // CR5: acceptance is deliberately wider than emission — InitValueClock parses up to 9
        // fractional digits, but the FIX 4.4 UTCTimestamp grammar tops out at milliseconds, so the
        // wire form truncates past the third digit rather than rejecting the value.
        var clock = BerlinClock(new InitValueClock("08:00:00.123456789"), Instant.FromUtc(2026, 1, 15, 12, 0, 0));

        clock.LoadInitValue(FixFieldValueProvider.Empty);

        clock.ToString(null!).Should().Be("20260115-07:00:00.123");
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

        clock
            .ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should()
            .Be(new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Mode1_keeps_initValue_when_it_is_still_in_the_future()
    {
        // now = 05:00Z (= 06:00 Berlin winter); init = 08:00 Berlin = 07:00Z > now -> keep init (07:00Z).
        var clock = BerlinClock(new InitValueClock("08:00:00"), Instant.FromUtc(2026, 1, 15, 5, 0, 0), mode: 1);

        clock.LoadInitValue(FixFieldValueProvider.Empty);

        clock
            .ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should()
            .Be(new DateTime(2026, 1, 15, 7, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void No_init_value_yields_null()
    {
        var clock = new Clock_t("clk") { Clock = new FakeClock(Instant.FromUtc(2026, 1, 15, 12, 0, 0)) };

        clock.LoadInitValue(FixFieldValueProvider.Empty);

        clock.ToDateTime(null!, CultureInfo.InvariantCulture).Should().BeNull();
        clock.GetCurrentValue().Should().BeNull();
    }

    [Fact]
    public void Date_bearing_initValue_uses_its_own_date_not_the_clock_today()
    {
        // initValue carries 2026-01-15 (CET, UTC+1) -> 07:00Z, regardless of the FakeClock being in September.
        var clock = BerlinClock(new InitValueClock("20260115-08:00:00"), Instant.FromUtc(2026, 9, 1, 0, 0, 0));

        clock.LoadInitValue(FixFieldValueProvider.Empty);

        clock
            .ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should()
            .Be(new DateTime(2026, 1, 15, 7, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Mode1_with_date_bearing_initValue_uses_now_when_passed()
    {
        // init = 2026-01-15 08:00 Berlin = 07:00Z; now = 2026-06-01 00:00Z is later -> use now.
        var clock = BerlinClock(new InitValueClock("20260115-08:00:00"), Instant.FromUtc(2026, 6, 1, 0, 0, 0), mode: 1);

        clock.LoadInitValue(FixFieldValueProvider.Empty);

        clock
            .ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should()
            .Be(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Market_today_is_derived_in_the_zone_not_in_utc()
    {
        // now = 2026-01-15 20:00Z; in Pacific/Auckland (NZDT, UTC+13) that is 2026-01-16 09:00, so the
        // market "today" is the 16th. initValue 06:00 -> 2026-01-16 06:00 Auckland -> 2026-01-15 17:00Z.
        // (If "today" were taken in UTC (the 15th) the result would be a day earlier — this pins the zone derivation.)
        var clock = new Clock_t("clk")
        {
            InitValue = new InitValueClock("06:00:00"),
            LocalMktTz = "Pacific/Auckland",
            InitValueMode = 0,
            Clock = new FakeClock(Instant.FromUtc(2026, 1, 15, 20, 0, 0)),
        };

        clock.LoadInitValue(FixFieldValueProvider.Empty);

        clock
            .ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should()
            .Be(new DateTime(2026, 1, 15, 17, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Spring_forward_gap_initValue_resolves_leniently_without_throwing()
    {
        // Europe/Berlin spring-forward 2026-03-29: 02:00->03:00 local; 02:30 does not exist.
        // LenientResolver maps the gap time forward; assert the resolved UTC instant (and that it does not throw).
        var clock = BerlinClock(new InitValueClock("02:30:00"), Instant.FromUtc(2026, 3, 29, 12, 0, 0));

        var act = () => clock.LoadInitValue(FixFieldValueProvider.Empty);
        act.Should().NotThrow();

        clock
            .ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should()
            .Be(new DateTime(2026, 3, 29, 1, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void SetValue_with_unspecified_kind_throws_argument_exception()
    {
        var clock = new Clock_t("clk") { Clock = new FakeClock(Instant.FromUtc(2026, 1, 15, 12, 0, 0)) };
        var act = () => clock.SetValue(new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Unspecified));
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage(
                "DateTimeKind.Unspecified is not supported to avoid timezone ambiguity; Kind must be Utc or Local*"
            );
    }

    [Fact]
    public void Offset_bearing_initValue_takes_precedence_over_localMktTz()
    {
        // initValue carries its own explicit UTC offset (India, +05:30) - it must convert directly,
        // ignoring localMktTz's own zone (Berlin), rather than being reinterpreted through it.
        var clock = new Clock_t("clk")
        {
            InitValue = new InitValueClock("13:09:00+05:30"),
            LocalMktTz = "Europe/Berlin",
            InitValueMode = 0,
            Clock = new FakeClock(Instant.FromUtc(2026, 9, 1, 0, 0, 0)),
        };

        clock.LoadInitValue(FixFieldValueProvider.Empty);

        clock
            .ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should()
            .Be(new DateTime(2026, 9, 1, 7, 39, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Offset_bearing_initValue_anchors_today_in_its_own_offset_not_utc()
    {
        // now = 2026-09-01 23:30Z; initValue = 01:00+05:30. UTC's calendar day (Sep 1) differs from the
        // offset's own wall-clock day at this instant (Sep 2, since 23:30Z + 5:30 = 05:00 next day) - if
        // "today" were wrongly anchored in UTC, this would resolve a day early (2026-08-31T19:30Z) instead
        // of the correct 2026-09-01T19:30Z (01:00+05:30 on 2026-09-02's offset-local calendar day).
        var clock = new Clock_t("clk")
        {
            InitValue = new InitValueClock("01:00:00+05:30"),
            LocalMktTz = "Europe/Berlin",
            InitValueMode = 0,
            Clock = new FakeClock(Instant.FromUtc(2026, 9, 1, 23, 30, 0)),
        };

        clock.LoadInitValue(FixFieldValueProvider.Empty);

        clock
            .ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should()
            .Be(new DateTime(2026, 9, 1, 19, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Offset_bearing_initValue_still_requires_localMktTz_attribute()
    {
        // localMktTz stays a required Clock_t attribute whenever initValue is supplied (spec table,
        // unconditional) - even though the offset form does not need it for the conversion itself.
        var clock = new Clock_t("clk")
        {
            InitValue = new InitValueClock("13:09:00+05:30"),
            InitValueMode = 0,
            Clock = new FakeClock(Instant.FromUtc(2026, 9, 1, 0, 0, 0)),
        };

        var act = () => clock.LoadInitValue(FixFieldValueProvider.Empty);

        act.Should().Throw<InvalidFieldValueException>();
    }

    [Fact]
    public void Mode1_with_offset_bearing_initValue_uses_now_when_passed()
    {
        // init = 13:09+05:30 = 07:39Z; now = 2026-09-01 10:00Z is later -> use now.
        var clock = new Clock_t("clk")
        {
            InitValue = new InitValueClock("13:09:00+05:30"),
            LocalMktTz = "Europe/Berlin",
            InitValueMode = 1,
            Clock = new FakeClock(Instant.FromUtc(2026, 9, 1, 10, 0, 0)),
        };

        clock.LoadInitValue(FixFieldValueProvider.Empty);

        clock
            .ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should()
            .Be(new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void GetCurrentValue_with_unresolvable_localMktTz_throws()
    {
        var clock = new Clock_t("clk")
        {
            LocalMktTz = "Mars/Phobos",
            Clock = new FakeClock(Instant.FromUtc(2026, 1, 15, 12, 0, 0)),
        };
        // Set value first using a valid UTC DateTime
        clock.SetValue(new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc));

        Func<object?> act = clock.GetCurrentValue;
        act.Should().Throw<InvalidFieldValueException>();
    }

    [Theory]
    [InlineData(2026, 7, 15)] // EDT (UTC-4)
    [InlineData(2026, 1, 15)] // EST (UTC-5)
    public void SetValueFromParameter_utc_time_only_stays_utc_regardless_of_market_zone(int year, int month, int day)
    {
        // R01: a UTCTimeOnly_t parameter holds a Year-1 Kind=Utc value. The clock must keep the UTC
        // time-of-day, not re-read it as market-local wall clock (which re-emitted 17:30Z/18:30Z here).
        var parameter = new Parameter_t<UTCTimeOnly_t>("P") { WireValue = "13:30:00" };
        var clock = new Clock_t("clk")
        {
            LocalMktTz = "America/New_York",
            Clock = new FakeClock(Instant.FromUtc(year, month, day, 12, 0, 0)),
        };

        clock.SetValueFromParameter(parameter);

        clock
            .ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should()
            .Be(new DateTime(year, month, day, 13, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void SetValueFromParameter_tz_time_only_stays_utc_regardless_of_market_zone()
    {
        // TZTimeOnly_t anchors the same Year-1 Kind=Utc value after normalising the offset to UTC.
        var parameter = new Parameter_t<TZTimeOnly_t>("P") { WireValue = "13:30:00Z" };
        var clock = new Clock_t("clk")
        {
            LocalMktTz = "America/New_York",
            Clock = new FakeClock(Instant.FromUtc(2026, 7, 15, 12, 0, 0)),
        };

        clock.SetValueFromParameter(parameter);

        clock
            .ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should()
            .Be(new DateTime(2026, 7, 15, 13, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void SetValue_with_a_bare_time_of_day_resolves_in_localMktTz_like_initValue()
    {
        // R14: "08:00:00" via SetValue must resolve exactly as the same literal does as an initValue —
        // the market's "today" in localMktTz (CEST, UTC+2 -> 06:00Z), not the host's today at 08:00Z.
        var clock = new Clock_t("clk")
        {
            LocalMktTz = "Europe/Berlin",
            Clock = new FakeClock(Instant.FromUtc(2026, 7, 15, 12, 0, 0)),
        };

        clock.SetValue("08:00:00");

        clock
            .ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should()
            .Be(new DateTime(2026, 7, 15, 6, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void SetValue_with_a_full_timestamp_keeps_the_utc_wire_interpretation()
    {
        // A full date-and-time keeps the UTC wire interpretation so the control round-trips its own
        // serialized output. Same instant as the bare-time-of-day case above, reached via the other path.
        var clock = new Clock_t("clk")
        {
            LocalMktTz = "Europe/Berlin",
            Clock = new FakeClock(Instant.FromUtc(2026, 7, 15, 12, 0, 0)),
        };

        clock.SetValue("20260715-06:00:00");

        clock
            .ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should()
            .Be(new DateTime(2026, 7, 15, 6, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void SetValue_with_an_offset_bearing_time_of_day_resolves_via_its_offset()
    {
        // The explicit offset pins the value to UTC without consulting localMktTz: 08:00 at -05:00 is 13:00Z.
        var clock = new Clock_t("clk")
        {
            LocalMktTz = "Europe/Berlin",
            Clock = new FakeClock(Instant.FromUtc(2026, 7, 15, 12, 0, 0)),
        };

        clock.SetValue("08:00:00-05:00");

        clock
            .ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should()
            .Be(new DateTime(2026, 7, 15, 13, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void SetValue_with_a_bare_time_of_day_and_no_localMktTz_throws()
    {
        // Without localMktTz there is no zone to resolve a date-less time-of-day against.
        var clock = new Clock_t("clk") { Clock = new FakeClock(Instant.FromUtc(2026, 7, 15, 12, 0, 0)) };

        var act = () => clock.SetValue("08:00:00");

        act.Should().Throw<InvalidFieldValueException>();
    }

    [Theory]
    [InlineData("08:00:00.5", 500)]
    [InlineData("08:00:00.25", 250)]
    [InlineData("08:00:00.5000", 500)]
    public void InitValueClock_accepts_fractional_seconds_of_any_precision(string raw, int expectedMilliseconds)
    {
        // Fractional seconds are not pinned to exactly 3 digits: ".5", ".25" and ".5000" all parse.
        new InitValueClock(raw)
            .TimeOfDay!.Value.Millisecond.Should()
            .Be(expectedMilliseconds);
    }

    [Fact]
    public void InitValueClock_accepts_fractional_seconds_on_a_date_bearing_value()
    {
        new InitValueClock("20260601-09:30:00.5").DateTime!.Value.Millisecond.Should().Be(500);
    }

    [Fact]
    public void SetValueFromParameter_local_market_date_round_trips_the_same_calendar_day()
    {
        // Low 17: a date-only parameter bound to a Clock_t carries a calendar date, not an instant.
        // The clock pins it at UTC midnight so the emitted yyyyMMdd is the same day even with a
        // market zone ahead of UTC.
        var parameter = new Parameter_t<LocalMktDate_t>("P") { WireValue = "20260715" };
        var clock = new Clock_t("clk")
        {
            LocalMktTz = "Asia/Tokyo",
            Clock = new FakeClock(Instant.FromUtc(2026, 7, 15, 12, 0, 0)),
        };

        clock.SetValueFromParameter(parameter);

        clock
            .ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should()
            .Be(new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc));
        parameter.SetValueFromControl(clock).IsValid.Should().BeTrue();
        parameter.WireValue.Should().Be("20260715");
    }

    [Fact]
    public void SetValueFromParameter_unspecified_local_market_date_keeps_its_calendar_day()
    {
        // Low 17: an off-label programmatic value can arrive Kind=Unspecified. Resolving that midnight
        // in localMktTz emits the previous date for zones ahead of UTC (Tokyo 2026-07-15 00:00 becomes
        // 2026-07-14 15:00Z); the date must be pinned at UTC midnight instead.
        var parameter = new Parameter_t<LocalMktDate_t>("P");
        parameter.Value.ConstValue = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Unspecified);
        var clock = new Clock_t("clk")
        {
            LocalMktTz = "Asia/Tokyo",
            Clock = new FakeClock(Instant.FromUtc(2026, 7, 15, 12, 0, 0)),
        };

        clock.SetValueFromParameter(parameter);

        clock
            .ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should()
            .Be(new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void SetValueFromParameter_unspecified_utc_date_only_keeps_its_calendar_day()
    {
        // The same date-only anchoring applies to UTCDateOnly_t, the other date-only parameter type.
        var parameter = new Parameter_t<UTCDateOnly_t>("P");
        parameter.Value.ConstValue = new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Unspecified);
        var clock = new Clock_t("clk")
        {
            LocalMktTz = "Asia/Tokyo",
            Clock = new FakeClock(Instant.FromUtc(2026, 7, 15, 12, 0, 0)),
        };

        clock.SetValueFromParameter(parameter);

        clock
            .ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should()
            .Be(new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc));
    }

    [Theory]
    [InlineData(2026, 1, 2, 1, 2026, 1, 1, 15)] // 01:00Z on the 2nd is still the 1st in New York (EST)
    [InlineData(2026, 7, 15, 12, 2026, 7, 15, 14)] // EDT (UTC-4)
    public void SetValue_with_an_unspecified_time_only_anchors_to_the_market_today(
        int nowYear,
        int nowMonth,
        int nowDay,
        int nowHour,
        int expectedYear,
        int expectedMonth,
        int expectedDay,
        int expectedHour
    )
    {
        // A Year-1 Kind=Unspecified value is the time-only sentinel every UI time picker emits. Resolved
        // literally it stays in year 1, where America/New_York still runs on Local Mean Time (-04:56:02):
        // a 10:31 edit came back as 00010101-15:27:02 - wrong date, and seconds of LMT offset in a value
        // entered to the minute. It must anchor to the market's today, like a bare initValue time-of-day.
        var clock = new Clock_t("clk")
        {
            LocalMktTz = "America/New_York",
            Clock = new FakeClock(Instant.FromUtc(nowYear, nowMonth, nowDay, nowHour, 0, 0)),
        };

        clock.SetValue(new DateTime(1, 1, 1, 10, 31, 0, DateTimeKind.Unspecified));

        clock
            .ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should()
            .Be(new DateTime(expectedYear, expectedMonth, expectedDay, expectedHour, 31, 0, DateTimeKind.Utc));
    }
}
