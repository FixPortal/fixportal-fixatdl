using System.Globalization;
using System.Text.Json;
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

    [Theory]
    [InlineData("1.5")]
    [InlineData("2.5")]
    [InlineData("10.6")]
    public void Fractional_json_is_rejected_for_integer_types(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var act = () => FixValueFormatter.Format(doc.RootElement, "Int_t", null);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(1.5d)]
    [InlineData(2.5d)]
    [InlineData(10.6d)]
    public void Fractional_double_is_rejected_for_integer_types(double value)
    {
        var act = () => FixValueFormatter.Format(value, "Int_t", null);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Fractional_float_and_decimal_are_rejected_for_integer_types()
    {
        var floatAct = () => FixValueFormatter.Format(1.5f, "Int_t", null);
        var decimalAct = () => FixValueFormatter.Format(1.5m, "Int_t", null);
        floatAct.Should().Throw<ArgumentOutOfRangeException>();
        decimalAct.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Integral_and_string_integer_inputs_keep_their_text()
    {
        using var whole = JsonDocument.Parse("2");
        using var wide = JsonDocument.Parse("9007199254740993");
        FixValueFormatter.Format(whole.RootElement, "Int_t", null).Should().Be("2");
        FixValueFormatter.Format(2d, "Int_t", null).Should().Be("2");
        FixValueFormatter.Format(wide.RootElement, "Int_t", null).Should().Be("9007199254740993");
        FixValueFormatter.Format("1.5", "Int_t", null).Should().Be("1.5");
    }

    [Fact]
    public void Whole_decimal_outside_int64_is_rejected_for_integer_types()
    {
        using var wide = JsonDocument.Parse("9223372036854775808");
        var jsonAct = () => FixValueFormatter.Format(wide.RootElement, "Int_t", null);
        var decimalAct = () => FixValueFormatter.Format(9223372036854775808m, "Int_t", null);
        jsonAct.Should().Throw<ArgumentOutOfRangeException>();
        decimalAct.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Json_numbers_keep_decimal_digits_on_float_types()
    {
        using var fraction = JsonDocument.Parse("1.5");
        using var precise = JsonDocument.Parse("123456789.123456789");
        FixValueFormatter.Format(fraction.RootElement, "Float_t", null).Should().Be("1.5");
        FixValueFormatter.Format(1.5d, "Float_t", null).Should().Be("1.5");
        FixValueFormatter.Format(precise.RootElement, "Float_t", null).Should().Be("123456789.123456789");
    }

    [Fact]
    public void Enum_id_match_uses_invariant_culture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            FixValueFormatter.Format(1.5m, "String_t", [new AtdlEnumPairDto("1.5", "WIRE")]).Should().Be("WIRE");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
