using System.Globalization;
using System.Text;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Controls;
using FixPortal.FixAtdl.Xml;
using NodaTime;
using NodaTime.Testing;

namespace FixPortal.FixAtdl.Tests.Xml;

public class StrategiesReaderBoundaryTests
{
    private const string StrategiesXml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Strategies xmlns="http://www.fixprotocol.org/FIXatdl-1-1/Core"
                    xmlns:lay="http://www.fixprotocol.org/FIXatdl-1-1/Layout"
                    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                    strategyIdentifierTag="5001">
          <Strategy name="Alpha" version="1" wireValue="A" uiRep="Alpha" providerID="DEMO">
            <Parameter name="Start" xsi:type="UTCTimestamp_t" fixTag="168" use="required"/>
            <Parameter name="End" xsi:type="UTCTimestamp_t" fixTag="126" use="required"/>
            <lay:StrategyLayout>
              <lay:StrategyPanel title="Alpha clocks" orientation="VERTICAL" collapsible="false" border="Line">
                <lay:Control ID="AStart" xsi:type="lay:Clock_t" parameterRef="Start" label="Start" initValue="08:00:00" localMktTz="Europe/Berlin" initValueMode="0"/>
                <lay:Control ID="AEnd" xsi:type="lay:Clock_t" parameterRef="End" label="End" initValue="09:15:00" localMktTz="Europe/Berlin" initValueMode="0"/>
              </lay:StrategyPanel>
            </lay:StrategyLayout>
          </Strategy>
          <Strategy name="Beta" version="1" wireValue="B" uiRep="Beta" providerID="DEMO">
            <Parameter name="Start" xsi:type="UTCTimestamp_t" fixTag="168" use="required"/>
            <Parameter name="End" xsi:type="UTCTimestamp_t" fixTag="126" use="required"/>
            <lay:StrategyLayout>
              <lay:StrategyPanel title="Beta clocks" orientation="VERTICAL" collapsible="false" border="Line">
                <lay:Control ID="BStart" xsi:type="lay:Clock_t" parameterRef="Start" label="Start" initValue="10:30:00" localMktTz="Europe/Berlin" initValueMode="0"/>
                <lay:Control ID="BEnd" xsi:type="lay:Clock_t" parameterRef="End" label="End" initValue="11:45:00" localMktTz="Europe/Berlin" initValueMode="0"/>
              </lay:StrategyPanel>
            </lay:StrategyLayout>
          </Strategy>
        </Strategies>
        """;

    [Fact]
    public void Load_leaves_the_callers_stream_open_and_readable()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(StrategiesXml));

        var strategies = new StrategiesReader().Load(stream);

        strategies.Select(strategy => strategy.Name).Should().Equal("Alpha", "Beta");
        stream.CanRead.Should().BeTrue();
        stream.Position = 0;
        stream.ReadByte().Should().Be(0x3C);
    }

    [Fact]
    public void Load_injects_the_clock_into_all_controls_in_all_strategies()
    {
        var clock = new FakeClock(Instant.FromUtc(2026, 1, 15, 12, 0, 0));
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(StrategiesXml));

        var strategies = new StrategiesReader(clock: clock).Load(stream);

        strategies.Select(strategy => strategy.Name).Should().Equal("Alpha", "Beta");
        strategies.Strategies[0].Controls.Select(control => control.Id).Should().Equal("AStart", "AEnd");
        strategies.Strategies[1].Controls.Select(control => control.Id).Should().Equal("BStart", "BEnd");
        var controls = strategies.SelectMany(strategy => strategy.Controls).ToArray();
        controls.Should().HaveCount(4);
        DateTime[] expected =
        [
            new(2026, 1, 15, 7, 0, 0, DateTimeKind.Utc),
            new(2026, 1, 15, 8, 15, 0, DateTimeKind.Utc),
            new(2026, 1, 15, 9, 30, 0, DateTimeKind.Utc),
            new(2026, 1, 15, 10, 45, 0, DateTimeKind.Utc),
        ];
        for (var index = 0; index < controls.Length; index++)
        {
            var control = controls[index].Should().BeOfType<Clock_t>().Which;
            control.Clock.Should().BeSameAs(clock, $"{control.Id} must receive the reader's injected clock");
            control.LoadInitValue(FixFieldValueProvider.Empty);
            control.ToDateTime(null!, CultureInfo.InvariantCulture).Should().Be(expected[index]);
        }
    }
}
