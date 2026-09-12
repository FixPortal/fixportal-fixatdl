using System.Text;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Controls;
using FixPortal.FixAtdl.Model.Controls.Support;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Utility;
using FixPortal.FixAtdl.Xml;

namespace FixPortal.FixAtdl.Tests.Conformance;

public class AdditionalConformanceTests
{
    private static Strategy_t LoadControls()
    {
        using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(FixtureFiles.ReadAllText("Fixtures/Conformance/consumer-controls.xml"))
        );
        return new StrategiesReader().Load(stream).Strategies[0];
    }

    [Fact]
    public void Numeric_slider_initializes_and_round_trips_numeric_parameter()
    {
        var strategy = LoadControls();
        var slider = strategy.Controls["NumericSlider"];

        slider.LoadInitValue(FixFieldValueProvider.Empty);

        slider.GetCurrentValue().Should().Be(12.5m);
        ((Slider_t)slider).Increment.Should().Be(0.25m);
        slider.HasEnumeratedState.Should().BeFalse();
        strategy.Parameters["Number"].SetValueFromControl(slider).IsValid.Should().BeTrue();
        strategy.Parameters["Number"].WireValue.Should().Be("12.5");
        strategy.Parameters["Number"].WireValue = "20.75";
        slider.SetValueFromParameter(strategy.Parameters["Number"]);
        slider.GetCurrentValue().Should().Be(20.75m);
        slider.Reset();
        strategy.Parameters["Number"].SetValueFromControl(slider).IsValid.Should().BeTrue();
        strategy.Parameters["Number"].WireValue.Should().BeNull();
    }

    [Theory]
    [InlineData(true, "e_A", true)]
    [InlineData(true, "e_B", false)]
    [InlineData(false, "e_A", false)]
    [InlineData(false, "e_B", true)]
    public void Enumerated_binary_state_rules_compare_enum_ids(bool selected, string literal, bool expected)
    {
        var strategy = LoadControls();
        strategy.Controls["BinaryControl"].SetValue(selected);
        var edit = new Edit_t<Control_t>
        {
            Field = "BinaryControl",
            Operator = Operator_t.Equal,
            Value = literal,
        };
        ((IResolvable<Strategy_t, Control_t>)edit).Resolve(strategy, strategy.Controls);

        edit.Evaluate();

        edit.CurrentState.Should().Be(expected);
        strategy.Controls["BinaryControl"].GetCurrentValue().Should().Be(selected);
    }

    [Theory]
    [InlineData("ChoiceA", "A")]
    [InlineData("ChoiceB", "B")]
    public void Selected_radio_value_is_not_overwritten_by_unselected_group_member(string selectedId, string wire)
    {
        var strategy = LoadControls();
        strategy.Controls.LoadDefaults(FixFieldValueProvider.Empty);
        strategy.Controls["ChoiceA"].SetValue(selectedId == "ChoiceA");
        strategy.Controls["ChoiceB"].SetValue(selectedId == "ChoiceB");

        strategy.Controls.TryUpdateParameterValues(strategy.Parameters, false, out _).Should().BeTrue();

        strategy.Parameters["Choice"].WireValue.Should().Be(wire);
    }

    [Fact]
    public void Radio_group_without_selection_still_enforces_required_parameter()
    {
        var strategy = LoadControls();
        strategy.Controls.LoadDefaults(FixFieldValueProvider.Empty);
        strategy.Parameters["Choice"].Use = Use_t.Required;
        strategy.Parameters["Choice"].WireValue = "A";
        strategy.Controls["ChoiceA"].SetValue(false);
        strategy.Controls["ChoiceB"].SetValue(false);

        strategy.Controls.TryUpdateParameterValues(strategy.Parameters, false, out var failures).Should().BeFalse();

        failures.Should().OnlyContain(failure => failure.IsMissing);
        strategy.Parameters["Choice"].IsSet.Should().BeFalse();
    }

    [Fact]
    public void Enumerated_binary_field2_comparison_uses_enum_ids_on_both_sides()
    {
        var strategy = LoadControls();
        strategy.Controls["BinaryControl"].SetValue(true);
        strategy.Controls["ChoiceA"].SetValue(true);
        var edit = new Edit_t<Control_t>
        {
            Field = "BinaryControl",
            Operator = Operator_t.Equal,
            Field2 = "ChoiceA",
        };
        ((IResolvable<Strategy_t, Control_t>)edit).Resolve(strategy, strategy.Controls);

        edit.Evaluate();

        edit.CurrentState.Should().BeTrue();
    }

    [Fact]
    public void List_field2_equality_compares_selected_ids_independently_of_declaration_order()
    {
        var strategy = LoadControls();
        strategy.Controls.LoadDefaults(FixFieldValueProvider.Empty);
        var edit = new Edit_t<Control_t>
        {
            Field = "ListForward",
            Operator = Operator_t.Equal,
            Field2 = "ListReverse",
        };
        ((IResolvable<Strategy_t, Control_t>)edit).Resolve(strategy, strategy.Controls);

        edit.Evaluate();

        edit.CurrentState.Should().BeTrue();
        strategy
            .Controls["ListForward"]
            .GetCurrentValue()
            .GetHashCode()
            .Should()
            .Be(strategy.Controls["ListReverse"].GetCurrentValue().GetHashCode());
        strategy.Controls["ListReverse"].SetValue(new EnumState(["e_C", "e_B", "e_A"]) { ["e_B"] = true });
        edit.Evaluate();
        edit.CurrentState.Should().BeFalse();
    }

    [Fact]
    public void Enum_selection_equality_ignores_unselected_options_and_compares_ids_not_positions()
    {
        var left = new EnumState(["A", "B"]) { ["A"] = true };
        var sameSelection = new EnumState(["C", "B", "A"]) { ["A"] = true };
        var differentSelection = new EnumState(["B", "A"]) { ["B"] = true };

        left.Equals(sameSelection).Should().BeTrue();
        sameSelection.Equals(left).Should().BeTrue();
        left.GetHashCode().Should().Be(sameSelection.GetHashCode());
        left.Equals(differentSelection).Should().BeFalse();
    }

    [Fact]
    public void Referenced_state_rule_comparison_preserves_its_literal()
    {
        using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(FixtureFiles.ReadAllText("Fixtures/Conformance/editref-staterule.xml"))
        );

        var strategy = new StrategiesReader().Load(stream).Strategies[0];

        strategy.Controls.LoadDefaults(FixFieldValueProvider.Empty);
        var control = strategy.Controls["c_Duration"];
        control.SetValue("0");
        var rule = control.StateRules[0];
        rule.Evaluate();
        rule.CurrentState.Should().BeTrue();
    }
}
