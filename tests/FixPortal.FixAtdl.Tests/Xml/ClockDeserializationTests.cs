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
/// End-to-end deserialization tests that prove a <see cref="Clock_t"/> parsed from real ATDL XML
/// (initValue + localMktTz) resolves to the correct UTC instant via the production parse path.
/// </summary>
public class ClockDeserializationTests
{
    // Minimal valid ATDL: one Strategy, one Parameter, one lay:Clock_t control carrying
    // initValue + localMktTz + initValueMode. Mirrors the namespace/shape of the twap.xml fixture.
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

    // Same fixture as above but initValueMode="1" — "use now if the initValue time has already passed".
    private const string ClockStrategyXmlMode1 = """
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
                             label="Start" initValue="08:00:00" localMktTz="Europe/Berlin" initValueMode="1"/>
              </lay:StrategyPanel>
            </lay:StrategyLayout>
          </Strategy>
        </Strategies>
        """;

    private static Strategies_t Load(string xml)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return new StrategiesReader().Load(stream);
    }

    [Fact]
    public void Clock_t_deserialized_with_initValue_and_localMktTz_resolves_to_correct_utc()
    {
        var strategies = Load(ClockStrategyXml);

        var clock = strategies.Strategies[0].StrategyLayout.StrategyPanel.Controls
            .OfType<Clock_t>()
            .Single();

        // Inject a fixed clock so a time-only initValue is anchored to a deterministic market date.
        // TimeZoneProvider defaults to TZDB, which resolves Europe/Berlin.
        clock.Clock = new FakeClock(Instant.FromUtc(2026, 1, 15, 12, 0, 0));

        // LoadInitValue recomputes _value from InitValue on each call (reading Clock/TimeZoneProvider
        // live), so invoking it after injecting the FakeClock is deterministic. The parser does not
        // call it itself.
        clock.LoadInitValue(FixFieldValueProvider.Empty);

        // 08:00 Berlin in January is CET (UTC+1) => 07:00Z.
        clock.ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should().Be(new DateTime(2026, 1, 15, 7, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Clock_t_deserialized_with_initValueMode_1_uses_now_when_initValue_has_passed()
    {
        var strategies = Load(ClockStrategyXmlMode1);

        var clock = strategies.Strategies[0].StrategyLayout.StrategyPanel.Controls
            .OfType<Clock_t>()
            .Single();

        // "now" = 09:30Z (10:30 Berlin/CET), which is after initValue 08:00 Berlin (07:00Z).
        // With initValueMode=1 the control resolves to "now" and emits it as the UTC instant.
        clock.Clock = new FakeClock(Instant.FromUtc(2026, 1, 15, 9, 30, 0));

        clock.LoadInitValue(FixFieldValueProvider.Empty);

        clock.ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should().Be(new DateTime(2026, 1, 15, 9, 30, 0, DateTimeKind.Utc));
    }
}
