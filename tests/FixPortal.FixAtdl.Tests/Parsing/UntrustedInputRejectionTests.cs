using System.Text;
using System.Xml;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Xml;

namespace FixPortal.FixAtdl.Tests.Parsing;

public class UntrustedInputRejectionTests
{
    private static Strategies_t Load(string xml)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return new StrategiesReader().Load(stream);
    }

    [Fact]
    public void Load_rejects_document_with_internal_dtd()
    {
        const string xml = """
            <!DOCTYPE Strategies [<!ENTITY strategyName "EntityName">]>
            <Strategies xmlns="http://www.fixprotocol.org/FIXatdl-1-1/Core" strategyIdentifierTag="958">
              <Strategy name="&strategyName;" version="1" wireValue="Test" uiRep="Test"
                        providerID="DEMO" lclMktTz="Europe/London" />
            </Strategies>
            """;

        var act = () => Load(xml);

        act.Should().Throw<XmlException>();
    }

    [Fact]
    public void Load_rejects_document_with_external_entity()
    {
        string path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, "resolved");
            string xml = $$"""
                <!DOCTYPE Strategies [<!ENTITY xxe SYSTEM "{{new Uri(path).AbsoluteUri}}">]>
                <Strategies xmlns="http://www.fixprotocol.org/FIXatdl-1-1/Core" strategyIdentifierTag="958">
                  &xxe;
                  <Strategy name="Test" version="1" wireValue="Test" uiRep="Test"
                            providerID="DEMO" lclMktTz="Europe/London" />
                </Strategies>
                """;

            var act = () => Load(xml);

            act.Should().Throw<XmlException>();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("Boolean_t, FixPortal.FixAtdl")]
    [InlineData("Support.MonthYear")]
    public void Load_rejects_parameter_types_outside_the_allow_list(string parameterType)
    {
        var act = () => Load(ParameterDocument(parameterType));

        act.Should().Throw<InvalidFieldValueException>();
    }

    [Theory]
    [InlineData("lay:Clock_t, FixPortal.FixAtdl")]
    [InlineData("lay:InitValueClock")]
    public void Load_rejects_control_types_outside_the_allow_list(string controlType)
    {
        var act = () => Load(ControlDocument(controlType));

        act.Should().Throw<InvalidFieldValueException>();
    }

    private static string ParameterDocument(string parameterType) =>
        $$"""
            <Strategies xmlns="http://www.fixprotocol.org/FIXatdl-1-1/Core"
                        xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                        strategyIdentifierTag="958">
              <Strategy name="Test" version="1" wireValue="Test" uiRep="Test"
                        providerID="DEMO" lclMktTz="Europe/London">
                <Parameter name="Param" xsi:type="{{parameterType}}" fixTag="999" use="optional" />
              </Strategy>
            </Strategies>
            """;

    private static string ControlDocument(string controlType) =>
        $$"""
            <Strategies xmlns="http://www.fixprotocol.org/FIXatdl-1-1/Core"
                        xmlns:lay="http://www.fixprotocol.org/FIXatdl-1-1/Layout"
                        xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                        strategyIdentifierTag="958">
              <Strategy name="Test" version="1" wireValue="Test" uiRep="Test"
                        providerID="DEMO" lclMktTz="Europe/London">
                <lay:StrategyLayout>
                  <lay:StrategyPanel title="Panel">
                    <lay:Control ID="Control" xsi:type="{{controlType}}" />
                  </lay:StrategyPanel>
                </lay:StrategyLayout>
              </Strategy>
            </Strategies>
            """;
}
