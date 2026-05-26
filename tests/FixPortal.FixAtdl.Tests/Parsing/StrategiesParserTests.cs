using System.Text;
using FixPortal.FixAtdl.Xml;

namespace FixPortal.FixAtdl.Tests.Parsing;

public class StrategiesParserTests
{
    // StrategiesReader.Load() accepts a Stream, not a StringReader.
    private static FixPortal.FixAtdl.Model.Elements.Strategies_t Load(string xml)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return new StrategiesReader().Load(stream);
    }

    [Fact]
    public async Task Parse_twap_fixture_yields_one_strategy_named_TWAP()
    {
        var xml = await File.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);

        var strategies = Load(xml);

        strategies.Count.Should().Be(1);
        strategies.Strategies[0].Name.Should().Be("TWAP");
    }

    [Fact]
    public async Task Parse_twap_extracts_three_parameters_with_correct_fix_tags()
    {
        var xml = await File.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        var strategies = Load(xml);

        var twap = strategies.Strategies[0];
        twap.Parameters.Should().HaveCount(3);
        twap.Parameters.Select(p => p.Name).Should().BeEquivalentTo("StartTime", "EndTime", "Participation");
        ((int)twap.Parameters["StartTime"].FixTag!.Value).Should().Be(168);
        ((int)twap.Parameters["EndTime"].FixTag!.Value).Should().Be(126);
        ((int)twap.Parameters["Participation"].FixTag!.Value).Should().Be(7700);
    }

    [Fact]
    public async Task Parse_twap_extracts_strategy_layout_with_three_controls()
    {
        var xml = await File.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        var strategies = Load(xml);

        var layout = strategies.Strategies[0].StrategyLayout;
        layout.Should().NotBeNull();
        var panel = layout!.StrategyPanel;
        panel.Controls.Should().HaveCount(3);
        panel.Controls.Select(c => c.Id).Should().BeEquivalentTo("c_StartTime", "c_EndTime", "c_Part");
    }

    [Fact]
    public async Task Parse_pov_extracts_state_rule_on_target_percentage()
    {
        var xml = await File.ReadAllTextAsync("Fixtures/pov.xml", TestContext.Current.CancellationToken);
        var strategies = Load(xml);

        var pov = strategies.Strategies[0];
        var targetCtrl = pov.StrategyLayout!.StrategyPanel.Controls
            .Single(c => c.ParameterRef == "TargetPercentage");
        targetCtrl.StateRules.Should().HaveCount(1);
    }
}

