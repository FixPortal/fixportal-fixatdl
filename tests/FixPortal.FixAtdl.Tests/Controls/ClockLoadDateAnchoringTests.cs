using System.Globalization;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Controls;
using NodaTime;
using NodaTime.Testing;

namespace FixPortal.FixAtdl.Tests.Controls;

/// <summary>
/// Characterization tests for finding D-F1: a time-only <see cref="Clock_t"/> initValue anchors its
/// DATE to the clock's "now" at the moment <see cref="Clock_t.LoadInitValue"/> runs (load time), and
/// the resolved value is NOT re-anchored to a later "send" time. This was left as-is by explicit
/// decision — re-resolving at send would silently mutate a value the operator may already have seen
/// and approved. These tests LOCK that behaviour: if either assertion fails, the load-vs-send time
/// semantics have changed and the D-F1 product decision must be revisited before merging.
/// </summary>
public class ClockLoadDateAnchoringTests
{
    // Etc/UTC keeps wall-clock == UTC so the resolved date/time reads directly.
    private static Clock_t TimeOnlyClock(IClock clock) => new("clk")
    {
        InitValue = new InitValueClock("08:00:00"), // time-only: the DATE is supplied by the clock
        LocalMktTz = "Etc/UTC",
        InitValueMode = 0,
        Clock = clock,
    };

    [Fact]
    public void Time_only_initValue_is_frozen_at_load_date_when_clock_advances_to_send_day()
    {
        var clock = new FakeClock(Instant.FromUtc(2026, 1, 15, 9, 0, 0));
        Clock_t control = TimeOnlyClock(clock);

        // Loaded on the 15th: the time-only 08:00 anchors to 2026-01-15.
        control.LoadInitValue(FixFieldValueProvider.Empty);

        // Time passes — e.g. loaded near midnight, the order is actually sent the next day.
        clock.AdvanceDays(1);

        // D-F1: the value stays anchored to the LOAD date (15th), not the send date (16th).
        control.ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should().Be(new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Explicit_reload_re_anchors_to_the_new_now()
    {
        var clock = new FakeClock(Instant.FromUtc(2026, 1, 15, 9, 0, 0));
        Clock_t control = TimeOnlyClock(clock);

        control.LoadInitValue(FixFieldValueProvider.Empty);
        clock.AdvanceDays(1);

        // Re-loading DOES pick up the new "now" (16th). The engine resolves against the clock at
        // load time; whether a reload should happen at send is the host's call — that is the D-F1
        // decision boundary, deliberately left outside the control.
        control.LoadInitValue(FixFieldValueProvider.Empty);

        control.ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should().Be(new DateTime(2026, 1, 16, 8, 0, 0, DateTimeKind.Utc));
    }
}
