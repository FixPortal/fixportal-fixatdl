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
