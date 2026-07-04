using System.Globalization;
using System.Text;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Controls;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Xml;
using NodaTime;
using NodaTime.Testing;

namespace FixPortal.FixAtdl.Tests.Xml;

/// <summary>
/// Proves a clock injected into <see cref="StrategiesReader"/> is auto-wired into every parsed
/// <see cref="Clock_t"/>, so the host does not have to walk the control graph and assign
/// <see cref="Clock_t.Clock"/> by hand (which is what every existing Clock_t test has to do).
/// </summary>
public class StrategiesReaderClockInjectionTests
{
    // Minimal ATDL with a single time-only Clock_t: the DATE portion of the resolved instant is
    // taken from the clock, so an injected FakeClock makes the result fully deterministic.
    private const string ClockStrategyXml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Strategies xmlns="http://www.fixprotocol.org/FIXatdl-1-1/Core"
                    xmlns:lay="http://www.fixprotocol.org/FIXatdl-1-1/Layout"
                    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                    strategyIdentifierTag="5001">
          <Strategy name="ClockTest" version="1" wireValue="ClockTest" uiRep="ClockTest" providerID="DEMO">
            <Parameter name="StartTime" xsi:type="UTCTimestamp_t" fixTag="168" use="required"/>
            <lay:StrategyLayout>
              <lay:StrategyPanel title="Clock Panel" orientation="VERTICAL" collapsible="false" border="Line">
                <lay:Control ID="c_StartTime" xsi:type="lay:Clock_t" parameterRef="StartTime"
                             label="Start" initValue="08:00:00" localMktTz="Europe/Berlin" initValueMode="0"/>
              </lay:StrategyPanel>
            </lay:StrategyLayout>
          </Strategy>
        </Strategies>
        """;

    [Fact]
    public void Injected_clock_is_wired_into_every_parsed_Clock_t()
    {
        var clock = new FakeClock(Instant.FromUtc(2026, 1, 15, 12, 0, 0));

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ClockStrategyXml));
        Strategies_t strategies = new StrategiesReader(clock: clock).Load(stream);

        Clock_t control = strategies.Strategies[0].StrategyLayout.StrategyPanel.Controls
            .OfType<Clock_t>()
            .Single();

        // No manual control.Clock assignment — the reader wired the injected clock at load time.
        control.LoadInitValue(FixFieldValueProvider.Empty);

        // 08:00 Berlin in January is CET (UTC+1) => 07:00Z, anchored to the injected clock's date.
        control.ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should().Be(new DateTime(2026, 1, 15, 7, 0, 0, DateTimeKind.Utc));
    }
}
