using System.Text;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Xml;

// AwesomeAssertions, Xunit are global usings (see GlobalUsings.cs).

namespace FixPortal.FixAtdl.Tests.Xml;

/// <summary>
/// End-to-end deserialization tests that prove an <see cref="EnumPair_t.Index"/> ordering hint is
/// captured from real ATDL XML when present, defaults to null when absent, and does not perturb the
/// FIX wire value.
/// </summary>
public class EnumPairDeserializationTests
{
    private const string EnumPairStrategyXml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Strategies xmlns="http://www.fixprotocol.org/FIXatdl-1-1/Core"
                    xmlns:lay="http://www.fixprotocol.org/FIXatdl-1-1/Layout"
                    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                    strategyIdentifierTag="5001">
          <Strategy name="EnumTest" version="1" wireValue="EnumTest" uiRep="EnumTest" providerID="DEMO">
            <Parameter name="Side" xsi:type="Char_t" fixTag="54" use="required">
              <EnumPair enumID="o1" index="0" wireValue="1" />
              <EnumPair enumID="o2" index="1" wireValue="2" />
              <EnumPair enumID="o3" wireValue="3" />
            </Parameter>
            <lay:StrategyLayout>
              <lay:StrategyPanel title="P" orientation="VERTICAL" collapsible="false" border="Line" />
            </lay:StrategyLayout>
          </Strategy>
        </Strategies>
        """;

    private const string InvalidCurrencyConstValueXml = """
        <Strategies xmlns="http://www.fixprotocol.org/FIXatdl-1-1/Core"
                    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                    strategyIdentifierTag="5001">
          <Strategy name="EnumTest" version="1" wireValue="EnumTest" uiRep="EnumTest" providerID="DEMO">
            <Parameter name="Currency" xsi:type="Currency_t" fixTag="15" constValue="AED,AFN" />
          </Strategy>
        </Strategies>
        """;

    private const string UnsignedConstValueStrategyXml = """
        <Strategies xmlns="http://www.fixprotocol.org/FIXatdl-1-1/Core"
                    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                    strategyIdentifierTag="5001">
          <Strategy name="Unsigned" version="1" wireValue="Unsigned" uiRep="Unsigned" providerID="DEMO">
            <Parameter name="Length" xsi:type="Length_t" constValue="1" />
            <Parameter name="Group" xsi:type="NumInGroup_t" constValue="1" />
            <Parameter name="Sequence" xsi:type="SeqNum_t" constValue="1" />
            <Parameter name="Tag" xsi:type="TagNum_t" constValue="1" />
          </Strategy>
        </Strategies>
        """;

    private static Strategies_t Load(string xml)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return new StrategiesReader().Load(stream);
    }

    [Fact]
    public void EnumPair_index_is_captured_when_present()
    {
        var strategies = Load(EnumPairStrategyXml);
        var enumPairs = strategies.Strategies[0].Parameters[0].EnumPairs;

        enumPairs["o1"].Index.Should().Be(0);
        enumPairs["o2"].Index.Should().Be(1);
    }

    [Fact]
    public void EnumPair_index_is_null_when_absent()
    {
        var strategies = Load(EnumPairStrategyXml);
        var enumPairs = strategies.Strategies[0].Parameters[0].EnumPairs;

        enumPairs["o3"].Index.Should().BeNull();
    }

    [Fact]
    public void EnumPair_index_does_not_affect_wire_value()
    {
        var strategies = Load(EnumPairStrategyXml);
        var enumPairs = strategies.Strategies[0].Parameters[0].EnumPairs;

        enumPairs["o1"].WireValue.Should().Be("1");
    }

    [Fact]
    public void Currency_constValue_rejects_comma_separated_non_flags()
    {
        var act = () => Load(InvalidCurrencyConstValueXml);

        act.Should().Throw<FixPortal.FixAtdl.Diagnostics.Exceptions.InvalidFieldValueException>();
    }

    [Fact]
    public void Unsigned_parameter_constValues_deserialize_from_xml()
    {
        var strategy = Load(UnsignedConstValueStrategyXml).Strategies[0];

        strategy.Parameters["Length"].WireValue.Should().Be("1");
        strategy.Parameters["Group"].WireValue.Should().Be("1");
        strategy.Parameters["Sequence"].WireValue.Should().Be("1");
        strategy.Parameters["Tag"].WireValue.Should().Be("1");
    }
}
