using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Controls;
using FixPortal.FixAtdl.Model.Controls.Support;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Model.Types;

namespace FixPortal.FixAtdl.Tests.Model.Controls;

/// <summary>
/// Round-trip symmetry between controls and their bound parameters: a value the library emits in
/// one direction must be accepted in the other, and a parameter refresh must not orphan state a
/// host already holds (R07, R12, R13, R15 of the 2026-09-12 adversarial review).
/// </summary>
public class ControlParameterRoundTripTests
{
    // R07 - a Boolean_t parameter writes its wire spelling into a text control; the control must
    // accept the same spelling back.
    [Theory]
    [InlineData("Y")]
    [InlineData("N")]
    public void Text_control_round_trips_the_default_boolean_wire_spelling(string wire)
    {
        var parameter = new Parameter_t<Boolean_t>("Flag") { WireValue = wire };
        var control = new TextField_t("tf");

        control.SetValueFromParameter(parameter);
        control.GetCurrentValue().Should().Be(wire);

        parameter.SetValueFromControl(control).IsValid.Should().BeTrue();
        parameter.WireValue.Should().Be(wire);
    }

    [Fact]
    public void Text_control_round_trips_a_declared_boolean_wire_mapping()
    {
        var parameter = new Parameter_t<Boolean_t>("Flag");
        parameter.Value.TrueWireValue = "1";
        parameter.Value.FalseWireValue = "0";
        parameter.WireValue = "1";
        var control = new TextField_t("tf");

        control.SetValueFromParameter(parameter);
        control.GetCurrentValue().Should().Be("1");

        parameter.SetValueFromControl(control).IsValid.Should().BeTrue();
        parameter.WireValue.Should().Be("1");
    }

    [Theory]
    [InlineData("Y", true)]
    [InlineData("N", false)]
    public void ToBoolean_accepts_fix_spellings_for_a_non_boolean_target(string text, bool expected)
    {
        var control = new TextField_t("tf");
        control.SetValue(text);

        control.ToBoolean(Substitute.For<IParameter>()).Should().Be(expected);
    }

    // R12 - FIX initialisation must honour a Boolean_t parameter's declared wire mapping, not just
    // the Y/N defaults.
    [Theory]
    [InlineData("1", true)]
    [InlineData("0", false)]
    public void Fix_init_decodes_a_declared_boolean_wire_mapping(string wire, bool expected)
    {
        var parameter = new Parameter_t<Boolean_t>("Flag");
        parameter.Value.TrueWireValue = "1";
        parameter.Value.FalseWireValue = "0";
        var control = new CheckBox_t("cb")
        {
            ParameterRef = parameter.Name,
            InitPolicy = InitPolicy_t.UseFixField,
            InitFixField = "FIX_Side",
        };
        var initial = Substitute.For<IInitialFixValueProvider>();
        initial.InputFixValues.Returns(new FixTagValuesCollection { { 54, wire } });

        control.LoadInitValue(new FixFieldValueProvider(initial, [parameter]));

        control.GetCurrentValue().Should().Be(expected);
        parameter.SetValueFromControl(control).IsValid.Should().BeTrue();
        parameter.WireValue.Should().Be(wire);
    }

    [Theory]
    [InlineData("Y", true)]
    [InlineData("N", false)]
    public void Fix_init_still_decodes_the_default_boolean_wire_spelling(string wire, bool expected)
    {
        var parameter = new Parameter_t<Boolean_t>("Flag");
        var control = new CheckBox_t("cb")
        {
            ParameterRef = parameter.Name,
            InitPolicy = InitPolicy_t.UseFixField,
            InitFixField = "FIX_Side",
        };
        var initial = Substitute.For<IInitialFixValueProvider>();
        initial.InputFixValues.Returns(new FixTagValuesCollection { { 54, wire } });

        control.LoadInitValue(new FixFieldValueProvider(initial, [parameter]));

        control.GetCurrentValue().Should().Be(expected);
    }

    [Fact]
    public void Fix_init_falls_back_to_initvalue_for_an_unrecognised_boolean_wire_value()
    {
        var parameter = new Parameter_t<Boolean_t>("Flag");
        parameter.Value.TrueWireValue = "1";
        parameter.Value.FalseWireValue = "0";
        var control = new CheckBox_t("cb")
        {
            ParameterRef = parameter.Name,
            InitPolicy = InitPolicy_t.UseFixField,
            InitFixField = "FIX_Side",
            InitValue = true,
        };
        var initial = Substitute.For<IInitialFixValueProvider>();
        initial.InputFixValues.Returns(new FixTagValuesCollection { { 54, "X" } });

        control.LoadInitValue(new FixFieldValueProvider(initial, [parameter]));

        control.GetCurrentValue().Should().Be(true);
    }

    // R13 - an EditableDropDownList_t emits free text; a parameter refresh must reload it as the
    // non-enum value rather than failing the refresh.
    [Fact]
    public void Editable_drop_down_reloads_the_free_text_it_emitted()
    {
        var (parameter, control) = CreateEditable();
        control.SetValue(new EnumState(["e1"]) { NonEnumValue = "FreeText" });
        parameter.SetValueFromControl(control).IsValid.Should().BeTrue();
        parameter.WireValue.Should().Be("FreeText");

        var act = () => control.SetValueFromParameter(parameter);

        act.Should().NotThrow();
        ((EnumState)control.GetCurrentValue()).NonEnumValue.Should().Be("FreeText");
    }

    [Fact]
    public void Editable_drop_down_still_maps_a_recognised_wire_value_to_the_enum()
    {
        var (parameter, control) = CreateEditable();
        parameter.WireValue = "W1";

        control.SetValueFromParameter(parameter);

        var state = (EnumState)control.GetCurrentValue();
        state["e1"].Should().BeTrue();
        state.NonEnumValue.Should().BeNull();
    }

    // R15 - a parameter refresh updates the control's own EnumState in place, so a host holding the
    // instance across the refresh is not orphaned.
    [Fact]
    public void Parameter_refresh_updates_the_existing_enumstate_instance_in_place()
    {
        var parameter = new Parameter_t<MultipleStringValue_t>("Options");
        parameter.EnumPairs.Add(new EnumPair_t { EnumId = "a", WireValue = "aa" });
        parameter.EnumPairs.Add(new EnumPair_t { EnumId = "b", WireValue = "bb" });
        var control = new MultiSelectList_t("list");
        control.ListItems.Add(new ListItem_t { EnumId = "a", UiRep = "a" });
        control.ListItems.Add(new ListItem_t { EnumId = "b", UiRep = "b" });
        control.LoadInitValue(FixFieldValueProvider.Empty);
        var instance = (EnumState)control.GetCurrentValue();

        parameter.WireValue = "aa";
        control.SetValueFromParameter(parameter);

        ((EnumState)control.GetCurrentValue()).Should().BeSameAs(instance);
        instance["a"].Should().BeTrue();
        instance["b"].Should().BeFalse();
    }

    private static (Parameter_t<String_t> Parameter, EditableDropDownList_t Control) CreateEditable()
    {
        var parameter = new Parameter_t<String_t>("TextParam");
        parameter.EnumPairs.Add(new EnumPair_t { EnumId = "e1", WireValue = "W1" });
        var control = new EditableDropDownList_t("edl");
        control.ListItems.Add(new ListItem_t { EnumId = "e1", UiRep = "One" });
        control.LoadInitValue(FixFieldValueProvider.Empty);
        return (parameter, control);
    }
}
