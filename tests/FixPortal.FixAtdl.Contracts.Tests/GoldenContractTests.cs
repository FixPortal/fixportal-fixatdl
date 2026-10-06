using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using AwesomeAssertions;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Xml;
using Xunit;

namespace FixPortal.FixAtdl.Contracts.Tests;

/// <summary>
/// Holds the package to the wire JSON captured at commit 7a287c2, before the mapper moved
/// here. A failure means the package changed the contract consumed by
/// @fix-portal/fixatdl-react. Fix the code; never regenerate the golden.
/// </summary>
public class GoldenContractTests
{
    public static TheoryData<string, string> Cases() =>
        new()
        {
            { "twap", "Fixtures/twap.atdl.xml" },
            { "multi-strategy", "Fixtures/multi-strategy.atdl.xml" },
            { "editref-staterule", "Fixtures/editref-staterule.atdl.xml" },
            { "pov", "CoreFixtures/pov.xml" },
            { "tz-clock", "CoreFixtures/tz-clock.xml" },
            { "regions-enums", "CoreFixtures/regions-enums.xml" },
        };

    [Theory, MemberData(nameof(Cases))]
    public void Map_matches_simulator_golden(string golden, string fixture)
    {
        var xml = File.ReadAllText(Path.Join(AppContext.BaseDirectory, fixture));
        var (strategies, sourceXml) = ParseLikeTheSimulator(xml);

        var json = JsonSerializer.Serialize(new AtdlDtoMapper().Map(strategies, sourceXml), AtdlContractJson.Options);

        var expected = File.ReadAllText(Path.Join(AppContext.BaseDirectory, "Golden", $"{golden}.strategies.json"));
        json.Should().Be(expected.TrimEnd());
    }

    // Loads the document the way the goldens were captured at 7a287c2: once with whitespace
    // preserved, then feeds the re-serialised document to StrategiesReader and slices each
    // Strategy element's XML out of the same XDocument. The golden sourceXml values were
    // produced this way. XElement.ToString writes Environment.NewLine, so the slice is
    // normalised to "\n": the simulator serves from a Linux container, and that is the wire.
    private static (Strategies_t Strategies, IReadOnlyDictionary<string, string> SourceXml) ParseLikeTheSimulator(
        string xml
    )
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersFromEntities = 0,
        };
        XDocument doc;
        using (var reader = XmlReader.Create(new StringReader(xml), settings))
        {
            doc = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
        }

        using var stream = new MemoryStream();
        using (
            var writer = XmlWriter.Create(
                stream,
                new XmlWriterSettings { Encoding = Encoding.UTF8, CloseOutput = false }
            )
        )
        {
            doc.WriteTo(writer);
        }
        stream.Position = 0;
        var strategies = new StrategiesReader().Load(stream);

        var core = XNamespace.Get("http://www.fixprotocol.org/FIXatdl-1-1/Core");
        var elements = doc.Root!.Elements(core + "Strategy")
            .GroupBy(e => (string?)e.Attribute("name") ?? string.Empty)
            .ToDictionary(g => g.Key, g => g.First());
        var slices = new Dictionary<string, string>();
        foreach (var strategy in strategies)
        {
            slices[strategy.Name] = elements.TryGetValue(strategy.Name, out var element)
                ? element.ToString(SaveOptions.None).ReplaceLineEndings("\n")
                : string.Empty;
        }

        return (strategies, slices);
    }
}
