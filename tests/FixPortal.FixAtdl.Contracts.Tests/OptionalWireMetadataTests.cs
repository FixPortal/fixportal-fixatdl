using System.Text.Json;
using AwesomeAssertions;
using FixPortal.FixAtdl.Model.Controls;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Model.Types;
using FixPortal.FixAtdl.Model.Types.Support;
using Xunit;

namespace FixPortal.FixAtdl.Contracts.Tests;

public sealed class OptionalWireMetadataTests
{
    [Fact]
    public void Mapped_optional_control_and_parameter_fields_use_the_exact_consumer_wire_names()
    {
        var (strategy, panel) = NewStrategy();
        var text = new Parameter_t<String_t>("Text")
        {
            FixTag = 7001,
            Use = Use_t.Required,
            MutableOnCxlRpl = false,
        };
        text.Value.MinLength = 1;
        text.Value.MaxLength = 12;
        text.EnumPairs.Add(new EnumPair_t { EnumId = "A", WireValue = "ALPHA" });
        var price = new Parameter_t<Float_t>("Price");
        price.Value.MinValue = 1.25m;
        price.Value.MaxValue = 10.5m;
        price.Value.Precision = 2;
        var flag = new Parameter_t<Boolean_t>("Flag");
        flag.Value.TrueWireValue = "T";
        flag.Value.FalseWireValue = "F";
        flag.Value.ConstValue = false;
        var selection = new Parameter_t<MultipleStringValue_t>("Selection");
        selection.Value.InvertOnWire = false;
        var percent = new Parameter_t<Percentage_t>("Percent");
        percent.Value.MultiplyBy100 = false;
        var time = new Parameter_t<UTCTimestamp_t>("Time");
        time.Value.LocalMktTz = "Europe/London";
        foreach (var parameter in new IParameter[] { text, price, flag, selection, percent, time })
        {
            strategy.Parameters.Add(parameter);
        }
        panel.Controls.Add(
            new TextField_t("text")
            {
                Label = "Text label",
                ParameterRef = "Text",
                ToolTip = "Help",
                InitValue = "ALPHA",
            }
        );
        var list = new DropDownList_t("list") { ParameterRef = "Text", InitValue = "A" };
        list.ListItems.Add(new ListItem_t { EnumId = "A", UiRep = "Alpha label" });
        panel.Controls.Add(list);
        panel.Controls.Add(
            new SingleSpinner_t("single")
            {
                Increment = 0.25m,
                InitValue = 2.5m,
                InitPolicy = InitPolicy_t.UseFixField,
                InitFixField = "44",
            }
        );
        panel.Controls.Add(
            new RadioButton_t("radio")
            {
                CheckedEnumRef = "yes",
                UncheckedEnumRef = "no",
                RadioGroup = "group",
                InitValue = false,
            }
        );
        panel.Controls.Add(
            new Clock_t("clock")
            {
                InitValue = new InitValueClock("09:30:00"),
                InitValueMode = 1,
                LocalMktTz = "Europe/London",
            }
        );
        panel.Controls.Add(new DoubleSpinner_t("double") { InnerIncrement = 0m, OuterIncrement = 1m });
        using var json = Serialize(strategy);
        var mapped = json.RootElement.GetProperty("strategies")[0];
        var controls = mapped.GetProperty("panel").GetProperty("children");
        Check(
            controls[0],
            ("kind", "\"control\""),
            ("label", "\"Text label\""),
            ("parameterRef", "\"Text\""),
            ("tooltip", "\"Help\""),
            ("initValue", "\"ALPHA\"")
        );
        Check(controls[0].GetProperty("parameter"), ("name", "\"Text\""), ("minLength", "1"), ("maxLength", "12"));
        Check(controls[1].GetProperty("listItems")[0], ("enumId", "\"A\""), ("uiRep", "\"Alpha label\""));
        Check(
            controls[2],
            ("increment", "0.25"),
            ("initValue", "2.5"),
            ("initPolicy", "\"UseFixField\""),
            ("initFixField", "44")
        );
        Check(
            controls[3],
            ("checkedEnumRef", "\"yes\""),
            ("uncheckedEnumRef", "\"no\""),
            ("radioGroup", "\"group\""),
            ("initValue", "false")
        );
        Check(controls[4], ("initValue", "\"09:30:00\""), ("initValueMode", "1"), ("localMktTz", "\"Europe/London\""));
        Check(controls[5], ("innerIncrement", "0"), ("outerIncrement", "1"));
        var parameters = mapped.GetProperty("parameters");
        Check(
            parameters[0],
            ("fixTag", "7001"),
            ("mutableOnCxlRpl", "false"),
            ("useValue", "\"required\""),
            ("minLength", "1"),
            ("maxLength", "12")
        );
        Check(parameters[0].GetProperty("enumValues")[0], ("enumId", "\"A\""), ("wireValue", "\"ALPHA\""));
        Check(parameters[1], ("min", "1.25"), ("max", "10.5"), ("precision", "2"));
        Check(parameters[2], ("trueWireValue", "\"T\""), ("falseWireValue", "\"F\""), ("constValue", "false"));
        Check(parameters[3], ("invertOnWire", "false"));
        Check(parameters[4], ("multiplyBy100", "false"));
        Check(parameters[5], ("localMktTz", "\"Europe/London\""));
        foreach (var parameter in parameters.EnumerateArray())
        {
            parameter.TryGetProperty("defaultValue", out _).Should().BeFalse("mapper defaults belong to controls");
        }
    }

    [Fact]
    public void Unpopulated_optional_members_are_absent_in_actual_mapped_control_and_parameter_json()
    {
        var (strategy, panel) = NewStrategy();
        strategy.Parameters.Add(new Parameter_t<String_t>("Empty"));
        panel.Controls.Add(new TextField_t("empty"));
        using var json = Serialize(strategy);
        var mapped = json.RootElement.GetProperty("strategies")[0];
        mapped
            .GetProperty("parameters")[0]
            .EnumerateObject()
            .Select(p => p.Name)
            .Should()
            .BeEquivalentTo("name", "type", "mutableOnCxlRpl", "useValue");
        mapped
            .GetProperty("panel")
            .GetProperty("children")[0]
            .EnumerateObject()
            .Select(p => p.Name)
            .Should()
            .BeEquivalentTo("kind", "id", "type", "stateRules");
    }

    [Theory]
    [InlineData("Int", true)]
    [InlineData("Int", false)]
    [InlineData("MonthYear", true)]
    [InlineData("MonthYear", false)]
    [InlineData("Tenor", true)]
    [InlineData("Tenor", false)]
    public void Bounds_are_read_from_each_actual_model_type_and_reach_wire_json(string type, bool populated)
    {
        var (strategy, _) = NewStrategy();
        var (parameter, minimum, maximum) = CreateBounds(type, populated);
        strategy.Parameters.Add(parameter);
        var mapped = Map(strategy).Strategies.Single().Parameters.Single();
        mapped.Type.Should().Be(type + "_t");
        mapped.Min.Should().Be(minimum);
        mapped.Max.Should().Be(maximum);
        using var json = Serialize(strategy);
        var wire = json.RootElement.GetProperty("strategies")[0].GetProperty("parameters")[0];
        wire.TryGetProperty("min", out var min).Should().Be(populated);
        wire.TryGetProperty("max", out var max).Should().Be(populated);
        if (populated)
        {
            if (type == "Int")
            {
                min.GetInt32().Should().Be(-2);
                max.GetInt32().Should().Be(7);
            }
            else
            {
                min.GetString().Should().Be((string)minimum!);
                max.GetString().Should().Be((string)maximum!);
            }
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Data_lengths_survive_actual_model_reflection_and_serialization(bool populated)
    {
        var (strategy, _) = NewStrategy();
        var data = new Parameter_t<Data_t>("Data");
        data.Value.MinLength = populated ? 2 : null;
        data.Value.MaxLength = populated ? 9 : null;
        strategy.Parameters.Add(data);
        var mapped = Map(strategy).Strategies.Single().Parameters.Single();
        mapped.MinLength.Should().Be(populated ? 2 : null);
        mapped.MaxLength.Should().Be(populated ? 9 : null);
        using var json = Serialize(strategy);
        var wire = json.RootElement.GetProperty("strategies")[0].GetProperty("parameters")[0];
        wire.TryGetProperty("minLength", out var min).Should().Be(populated);
        wire.TryGetProperty("maxLength", out var max).Should().Be(populated);
        if (populated)
        {
            min.GetInt32().Should().Be(2);
            max.GetInt32().Should().Be(9);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData(false)]
    [InlineData(true)]
    public void MultipleChar_invert_preserves_unset_false_and_true(bool? invert)
    {
        var (strategy, _) = NewStrategy();
        var value = new Parameter_t<MultipleCharValue_t>("Selection");
        value.Value.InvertOnWire = invert;
        strategy.Parameters.Add(value);
        Map(strategy).Strategies.Single().Parameters.Single().InvertOnWire.Should().Be(invert);
        using var json = Serialize(strategy);
        var wire = json.RootElement.GetProperty("strategies")[0].GetProperty("parameters")[0];
        wire.TryGetProperty("invertOnWire", out var property).Should().Be(invert.HasValue);
        if (invert.HasValue)
        {
            property.GetBoolean().Should().Be(invert.Value);
        }
    }

    private static (IParameter Parameter, object? Minimum, object? Maximum) CreateBounds(string type, bool populated) =>
        type switch
        {
            "Int" => IntBounds(populated),
            "MonthYear" => MonthBounds(populated),
            "Tenor" => TenorBounds(populated),
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };

    private static (IParameter, object?, object?) IntBounds(bool populated)
    {
        var value = new Parameter_t<Int_t>("Bounds");
        value.Value.MinValue = populated ? -2 : null;
        value.Value.MaxValue = populated ? 7 : null;
        return (value, populated ? -2 : null, populated ? 7 : null);
    }

    private static (IParameter, object?, object?) MonthBounds(bool populated)
    {
        var value = new Parameter_t<MonthYear_t>("Bounds");
        value.Value.MinValue = populated ? MonthYear.Parse("202601") : null;
        value.Value.MaxValue = populated ? MonthYear.Parse("202612") : null;
        return (value, populated ? "202601" : null, populated ? "202612" : null);
    }

    private static (IParameter, object?, object?) TenorBounds(bool populated)
    {
        var value = new Parameter_t<Tenor_t>("Bounds");
        value.Value.MinValue = populated ? Tenor.Parse("D1") : null;
        value.Value.MaxValue = populated ? Tenor.Parse("Y2") : null;
        return (value, populated ? "D1" : null, populated ? "Y2" : null);
    }

    private static void Check(JsonElement value, params (string Name, string Json)[] expected)
    {
        foreach (var (name, json) in expected)
        {
            value
                .TryGetProperty(name, out var property)
                .Should()
                .BeTrue($"the literal consumer key {name} must be emitted");
            property.GetRawText().Should().Be(json);
        }
    }

    private static (Strategy_t Strategy, StrategyPanel_t Panel) NewStrategy()
    {
        var strategy = new Strategy_t { Name = "Metadata" };
        var panel = new StrategyPanel_t(strategy);
        strategy.StrategyLayout = new StrategyLayout_t { StrategyPanel = panel };
        return (strategy, panel);
    }

    private static AtdlStrategiesDto Map(Strategy_t strategy)
    {
        var document = new Strategies_t();
        document.Strategies.Add(strategy);
        return new AtdlDtoMapper().Map(document);
    }

    private static JsonDocument Serialize(Strategy_t strategy) =>
        JsonDocument.Parse(JsonSerializer.Serialize(Map(strategy), AtdlContractJson.Options));
}
