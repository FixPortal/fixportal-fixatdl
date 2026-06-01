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
}
