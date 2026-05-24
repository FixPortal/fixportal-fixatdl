using System.Text;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Xml;

namespace FixPortal.FixAtdl.Tests.Parsing;

public class StrategiesParserRejectionTests
{
    private static FixPortal.FixAtdl.Model.Elements.Strategies_t Load(string xml)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return new StrategiesReader().Load(stream);
    }

    [Fact]
    public async Task Parse_malformed_xml_throws_xml_exception()
    {
        var xml = await File.ReadAllTextAsync("Fixtures/malformed.xml", TestContext.Current.CancellationToken);
        var act = () => Load(xml);
        act.Should().Throw<System.Xml.XmlException>();
    }

    [Fact]
    public async Task Parse_schema_invalid_xml_throws_or_records_validation_error()
    {
        var xml = await File.ReadAllTextAsync("Fixtures/invalid-schema.xml", TestContext.Current.CancellationToken);
        var act = () => Load(xml);
        act.Should().Throw<Atdl4netException>();
    }
}

