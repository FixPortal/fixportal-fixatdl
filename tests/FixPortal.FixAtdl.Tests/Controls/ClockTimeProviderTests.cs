using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Controls;
using Microsoft.Extensions.Time.Testing;

namespace FixPortal.FixAtdl.Tests.Controls;

/// <summary>
/// initValueMode==1 means "use the current time if the initValue time has already passed".
/// These tests pin "now" via an injected TimeProvider so the branch is deterministic.
/// </summary>
public class ClockTimeProviderTests
{
    [Theory]
    [InlineData(13, 13)] // now (13:00) is after initValue (12:00) -> control takes now
    [InlineData(11, 12)] // now (11:00) is before initValue (12:00) -> control keeps initValue
    public void InitValueMode1_uses_injected_TimeProvider(int nowHour, int expectedHour)
    {
        // FakeTimeProvider's LocalTimeZone defaults to UTC, so GetLocalNow().DateTime returns the
        // start wall-clock time below — i.e. nowHour:00 — letting these hours be compared directly.
        var fake = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, nowHour, 0, 0, TimeSpan.Zero));
        var clock = new Clock_t("clk")
        {
            InitValue = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Unspecified),
            InitValueMode = 1,
            TimeProvider = fake,
        };

        clock.LoadInitValue(FixFieldValueProvider.Empty);

        var value = (DateTime?)clock.GetCurrentValue();
        value.Should().Be(new DateTime(2026, 1, 1, expectedHour, 0, 0, DateTimeKind.Unspecified));
    }
}
