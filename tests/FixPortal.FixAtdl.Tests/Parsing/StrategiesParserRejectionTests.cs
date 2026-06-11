using System.Text;
using System.Xml;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Xml;

namespace FixPortal.FixAtdl.Tests.Parsing;

public class StrategiesParserRejectionTests
{
    private static Strategies_t Load(string xml)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return new StrategiesReader().Load(stream);
    }

    [Fact]
    public async Task Parse_malformed_xml_throws_xml_exception()
    {
        var xml = await FixtureFiles.ReadAllTextAsync("Fixtures/malformed.xml.txt", TestContext.Current.CancellationToken);
        var act = () => Load(xml);
        act.Should().Throw<XmlException>();
    }

    [Fact]
    public async Task Parse_schema_invalid_xml_throws_or_records_validation_error()
    {
        var xml = await FixtureFiles.ReadAllTextAsync("Fixtures/invalid-schema.xml", TestContext.Current.CancellationToken);
        var act = () => Load(xml);
        act.Should().Throw<FixAtdlException>();
    }

    [Fact]
    public void Parse_document_with_repeating_group_throws_fix_atdl_exception()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <Strategies xmlns="http://www.fixprotocol.org/FIXatdl-1-1/Core"
                        xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                        xsi:schemaLocation="http://www.fixprotocol.org/FIXatdl-1-1/Core"
                        strategyIdentifierTag="958">
                <Strategy name="TestRepeating" lclMktTz="Europe/London">
                    <RepeatingGroup name="TestGroup" parameterRef="TestParam">
                        <Parameter name="TestParam" xsi:type="Boolean_t" fixTag="999" />
                    </RepeatingGroup>
                </Strategy>
            </Strategies>
            """;
        var act = () => Load(xml);
        act.Should().Throw<FixAtdlException>().WithMessage("RepeatingGroup elements are not supported.");
    }

    [Fact]
    public void Parse_document_with_undefined_enum_value_throws_invalid_field_value_exception()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <Strategies xmlns="http://www.fixprotocol.org/FIXatdl-1-1/Core"
                        xmlns:lay="http://www.fixprotocol.org/FIXatdl-1-1/Layout"
                        xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                        strategyIdentifierTag="958">
                <Strategy name="TestEnum" version="1" wireValue="TestEnum" uiRep="TestEnum" providerID="DEMO" lclMktTz="Europe/London">
                    <lay:StrategyLayout>
                        <lay:StrategyPanel title="P" orientation="999" collapsible="false" border="Line" />
                    </lay:StrategyLayout>
                </Strategy>
            </Strategies>
            """;
        var act = () => Load(xml);
        act.Should().Throw<InvalidFieldValueException>();
    }

    [Fact]
    public void Parse_deeply_nested_xml_throws_FixAtdlException()
    {
        // Construct a deeply nested XML document
        var sb = new StringBuilder();
        sb.AppendLine("""
            <?xml version="1.0" encoding="utf-8"?>
            <Strategies xmlns="http://www.fixprotocol.org/FIXatdl-1-1/Core"
                        xmlns:lay="http://www.fixprotocol.org/FIXatdl-1-1/Layout"
                        xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                        strategyIdentifierTag="958">
                <Strategy name="TestRecursion" version="1" wireValue="TestRecursion" uiRep="TestRecursion" providerID="DEMO" lclMktTz="Europe/London">
                    <lay:StrategyLayout>
            """);

        const int depth = 130;
        for (int i = 0; i < depth; i++)
        {
            sb.AppendLine("        <lay:StrategyPanel title=\"P\">");
        }
        for (int i = 0; i < depth; i++)
        {
            sb.AppendLine("        </lay:StrategyPanel>");
        }

        sb.AppendLine("""
                    </lay:StrategyLayout>
                </Strategy>
            </Strategies>
            """);

        var act = () => Load(sb.ToString());
        act.Should().Throw<FixAtdlException>().WithMessage("*exceeded maximum depth limit*");
    }
}
