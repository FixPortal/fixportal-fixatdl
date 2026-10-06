using AwesomeAssertions;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Enumerations;
using Xunit;

namespace FixPortal.FixAtdl.Contracts.Tests;

public class MapEdgeCaseTests
{
    private static Strategy_t MakeStrategy(string name)
    {
        var strategy = new Strategy_t { Name = name };
        var panel = new StrategyPanel_t(strategy)
        {
            Title = name,
            Orientation = Orientation_t.Vertical,
            Collapsible = false,
            Collapsed = false,
        };
        strategy.StrategyLayout = new StrategyLayout_t { StrategyPanel = panel };
        return strategy;
    }

    private static Strategies_t Wrap(params Strategy_t[] items)
    {
        var strategies = new Strategies_t();
        foreach (var item in items)
        {
            strategies.Strategies.Add(item);
        }
        return strategies;
    }

    [Fact]
    public void Empty_document_maps_to_empty_list() =>
        new AtdlDtoMapper().Map(new Strategies_t()).Strategies.Should().BeEmpty();

    [Fact]
    public void Missing_source_xml_maps_to_empty_string_not_null()
    {
        var withoutDictionary = new AtdlDtoMapper().Map(Wrap(MakeStrategy("A")));
        var withOtherName = new AtdlDtoMapper().Map(
            Wrap(MakeStrategy("A")),
            new Dictionary<string, string> { ["B"] = "<Strategy name=\"B\"/>" }
        );

        withoutDictionary.Strategies.Single().SourceXml.Should().Be(string.Empty);
        withOtherName.Strategies.Single().SourceXml.Should().Be(string.Empty);
    }

    [Fact]
    public void Duplicate_strategy_names_are_both_mapped_with_the_same_source()
    {
        // StrategyCollection keys on Name at insert and does not rekey, so two
        // strategies share a name only after they are already in the list.
        var first = MakeStrategy("Dup-a");
        var second = MakeStrategy("Dup-b");
        var strategies = Wrap(first, second);
        first.Name = "Dup";
        second.Name = "Dup";

        var result = new AtdlDtoMapper().Map(
            strategies,
            new Dictionary<string, string> { ["Dup"] = "<Strategy name=\"Dup\"/>" }
        );

        result.Strategies.Should().HaveCount(2);
        result.Strategies.Should().AllSatisfy(s => s.SourceXml.Should().Be("<Strategy name=\"Dup\"/>"));
    }

    [Fact]
    public void Null_strategies_throws_ArgumentNullException() =>
        FluentActions.Invoking(() => new AtdlDtoMapper().Map(null!)).Should().Throw<ArgumentNullException>();
}
