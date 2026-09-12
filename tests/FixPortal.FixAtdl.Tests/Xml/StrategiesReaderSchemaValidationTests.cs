using System.Text;
using System.Xml;
using System.Xml.Schema;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Xml;

namespace FixPortal.FixAtdl.Tests.Xml;

/// <summary>
/// Proves <see cref="StrategiesReader"/> validates against a caller-supplied <see cref="XmlSchemaSet"/>
/// when one is given, and is unaffected when one is not. The library does not ship a FIXatdl 1.1 XSD
/// (FIX Protocol Limited's schema is licensed for internal use only, with no redistribution or
/// derivative-works rights, so it cannot be vendored into this public repo) - the schema below is a
/// small, independently-authored one for exercising the validation plumbing, not FPL's schema.
/// </summary>
public class StrategiesReaderSchemaValidationTests
{
    private const string MinimalStrategyXml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Strategies xmlns="http://www.fixprotocol.org/FIXatdl-1-1/Core"
                    xmlns:lay="http://www.fixprotocol.org/FIXatdl-1-1/Layout"
                    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                    strategyIdentifierTag="5001">
          <Strategy name="Test" version="1" wireValue="Test" uiRep="Test" providerID="DEMO">
            <Parameter name="Qty" xsi:type="Int_t" fixTag="38" use="required"/>
            <lay:StrategyLayout>
              <lay:StrategyPanel title="Panel" orientation="VERTICAL" collapsible="false" border="Line" />
            </lay:StrategyLayout>
          </Strategy>
        </Strategies>
        """;

    // Requires strategyIdentifierTag on the root Strategies element - deliberately narrower than the
    // real ATDL grammar, sufficient to prove the reader actually enforces whatever schema it is given.
    private const string MinimalXsd = """
        <?xml version="1.0" encoding="UTF-8"?>
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
                   xmlns="http://www.fixprotocol.org/FIXatdl-1-1/Core"
                   targetNamespace="http://www.fixprotocol.org/FIXatdl-1-1/Core"
                   elementFormDefault="qualified">
          <xs:element name="Strategies">
            <xs:complexType>
              <xs:sequence>
                <xs:any minOccurs="0" maxOccurs="unbounded" processContents="skip" />
              </xs:sequence>
              <xs:attribute name="strategyIdentifierTag" type="xs:string" use="required" />
            </xs:complexType>
          </xs:element>
        </xs:schema>
        """;

    private static XmlSchemaSet LoadMinimalSchemaSet()
    {
        XmlSchemaSet schemaSet = new();
        using var reader = XmlReader.Create(new StringReader(MinimalXsd));
        schemaSet.Add(null, reader);
        return schemaSet;
    }

    private static MemoryStream StreamOf(string xml) => new(Encoding.UTF8.GetBytes(xml));

    [Fact]
    public void Load_with_no_schema_set_does_not_validate()
    {
        using var stream = StreamOf(MinimalStrategyXml);

        Action act = () => new StrategiesReader().Load(stream);

        act.Should().NotThrow();
    }

    [Fact]
    public void Load_with_schema_set_accepts_a_conformant_document()
    {
        using var stream = StreamOf(MinimalStrategyXml);

        Action act = () => new StrategiesReader(schemaSet: LoadMinimalSchemaSet()).Load(stream);

        act.Should().NotThrow();
    }

    [Fact]
    public void Load_with_schema_set_rejects_a_document_missing_a_required_attribute()
    {
        string xml = MinimalStrategyXml.Replace(" strategyIdentifierTag=\"5001\"", "");
        using var stream = StreamOf(xml);

        Action act = () => new StrategiesReader(schemaSet: LoadMinimalSchemaSet()).Load(stream);

        act.Should()
            .Throw<SchemaValidationException>()
            .Which.Errors.Should()
            .ContainSingle(e => e.Contains("strategyIdentifierTag", StringComparison.Ordinal));
    }
}
