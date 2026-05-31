using System.Globalization;
using AwesomeAssertions;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Controls;
using NodaTime;
using NodaTime.Testing;

namespace FixPortal.FixAtdl.Tests.Controls;

/// <summary>
/// initValueMode==1 means "use the current time if the initValue time has already passed". "Now" is
/// pinned via an injected NodaTime IClock (FakeClock) and the comparison is made on instants — UTC,
/// zone-correct — replacing the former host-local DateTime comparison.
/// </summary>
public class ClockTimeProviderTests
{
    [Theory]
    [InlineData(13, 13)] // now (13:00Z) is after initValue (12:00Z) -> control takes now
    [InlineData(11, 12)] // now (11:00Z) is before initValue (12:00Z) -> control keeps initValue
    public void InitValueMode1_uses_injected_clock(int nowHourUtc, int expectedHourUtc)
    {
        // localMktTz = Etc/UTC keeps wall-clock == UTC so the hours compare directly.
        var clock = new Clock_t("clk")
        {
            InitValue = new InitValueClock("12:00:00"),
            LocalMktTz = "Etc/UTC",
            InitValueMode = 1,
            Clock = new FakeClock(Instant.FromUtc(2026, 1, 1, nowHourUtc, 0, 0)),
        };

        clock.LoadInitValue(FixFieldValueProvider.Empty);

        clock.ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should().Be(new DateTime(2026, 1, 1, expectedHourUtc, 0, 0, DateTimeKind.Utc));
    }
}
