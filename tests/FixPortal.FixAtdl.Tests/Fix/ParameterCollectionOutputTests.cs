using System.Text;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Xml;

namespace FixPortal.FixAtdl.Tests.Fix;

public class ParameterCollectionOutputTests
{
    private static Strategies_t Load(string xml)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return new StrategiesReader().Load(stream);
    }

    [Fact]
    public async Task Filled_twap_strategy_emits_expected_fix_tags()
    {
        var xml = await FixtureFiles.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        var strategies = Load(xml);
        var twap = strategies.Strategies[0];

        // WireValue is the raw FIX wire format string.
        // UTCTimestamp parameters use WireValue directly.
        // Percentage WireValue is the decimal wire form (e.g. "0.1" for 10%).
        twap.Parameters["StartTime"].WireValue = "20260101-09:30:00";
        twap.Parameters["EndTime"].WireValue = "20260101-16:00:00";
        twap.Parameters["Participation"].WireValue = "0.1";

        var fixValues = twap.Parameters.GetOutputValues();

        // FixTagValuesCollection enumerates KeyValuePair<FixField, string>.
        // FixField is an int-backed enum; cast to int to get the tag number.
        var dict = fixValues.ToDictionary(kv => (int)kv.Key, kv => kv.Value);

        dict.Should().ContainKey(168).WhoseValue.Should().Be("20260101-09:30:00");
        dict.Should().ContainKey(126).WhoseValue.Should().Be("20260101-16:00:00");
        dict.Should().ContainKey(7700).WhoseValue.Should().Be("0.1");
    }
}
