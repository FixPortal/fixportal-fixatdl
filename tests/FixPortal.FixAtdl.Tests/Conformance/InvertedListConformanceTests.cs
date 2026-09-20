using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Controls;
using FixPortal.FixAtdl.Model.Controls.Support;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Model.Types;
using NSubstitute;

namespace FixPortal.FixAtdl.Tests.Conformance;

public class InvertedListConformanceTests
{
    private static (IParameter Parameter, MultiSelectList_t Control) Create(bool chars, bool invert = true)
    {
        var text = new Parameter_t<MultipleStringValue_t>("Options");
        text.Value.InvertOnWire = invert;
        var character = new Parameter_t<MultipleCharValue_t>("Options");
        character.Value.InvertOnWire = invert;
        IParameter parameter = chars ? character : text;
        var control = new MultiSelectList_t("OptionsControl");
        foreach (var id in new[] { "a", "b", "c" })
        {
            parameter.EnumPairs.Add(
                new EnumPair_t { EnumId = id, WireValue = chars ? id.ToUpperInvariant() : id + id }
            );
            control.ListItems.Add(new ListItem_t { EnumId = id, UiRep = id });
        }
        control.LoadInitValue(FixFieldValueProvider.Empty);
        return (parameter, control);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Uninitialized_inverted_list_suppresses_wire_until_explicitly_edited(bool chars)
    {
        var (parameter, control) = Create(chars);

        parameter.SetValueFromControl(control).IsValid.Should().BeTrue();

        parameter.WireValue.Should().BeNull();
        control.SetValue(new EnumState(["a", "b", "c"]));
        parameter.SetValueFromControl(control).IsValid.Should().BeTrue();
        parameter.WireValue.Should().Be(chars ? "A B C" : "aa bb cc");
    }

    [Theory]
    [InlineData(false, "bb cc")]
    [InlineData(true, "B C")]
    public void Inverted_selection_round_trips_between_control_and_wire(bool chars, string expected)
    {
        var (parameter, control) = Create(chars);
        control.SetValue(new EnumState(["a", "b", "c"]) { ["a"] = true });

        parameter.SetValueFromControl(control).IsValid.Should().BeTrue();
        parameter.WireValue.Should().Be(expected);
        var (_, restored) = Create(chars);
        restored.SetValueFromParameter(parameter);

        var selected = (EnumState)restored.GetCurrentValue();
        selected["a"].Should().BeTrue();
        selected["b"].Should().BeFalse();
        selected["c"].Should().BeFalse();
    }

    [Theory]
    [InlineData(false, "aa bb cc")]
    [InlineData(true, "A B C")]
    public void Empty_selection_inverts_but_explicit_null_still_suppresses_wire(bool chars, string expected)
    {
        var (parameter, control) = Create(chars);
        control.SetValue(new EnumState(["a", "b", "c"]));

        parameter.SetValueFromControl(control).IsValid.Should().BeTrue();
        parameter.WireValue.Should().Be(expected);
        control.SetValue(Atdl.NullValue);
        parameter.SetValueFromControl(control).IsValid.Should().BeTrue();
        parameter.WireValue.Should().BeNull();
        control.SetValue(new EnumState(["a", "b", "c"]));
        parameter.SetValueFromControl(control).IsValid.Should().BeTrue();
        parameter.WireValue.Should().Be(expected);
    }

    [Theory]
    [InlineData(false, "bb cc", true, false)]
    [InlineData(true, "B C", true, false)]
    [InlineData(false, "aa bb cc", false, false)]
    [InlineData(true, "A B C", false, false)]
    [InlineData(false, "unknown", false, true)]
    public void Init_fix_field_decodes_multiple_wire_values_and_inversion(
        bool chars,
        string wire,
        bool selectedA,
        bool selectedB
    )
    {
        var (parameter, control) = Create(chars);
        control.ParameterRef = parameter.Name;
        control.InitPolicy = InitPolicy_t.UseFixField;
        control.InitFixField = "FIX_MsgType";
        control.InitValue = "b";
        var initial = Substitute.For<IInitialFixValueProvider>();
        initial.InputFixValues.Returns(new FixTagValuesCollection { { 35, wire } });
        var parameters = new ParameterCollection { parameter };

        control.LoadInitValue(new FixFieldValueProvider(initial, parameters));

        var selected = (EnumState)control.GetCurrentValue();
        selected["a"].Should().Be(selectedA);
        selected["b"].Should().Be(selectedB);
        parameter.SetValueFromControl(control).IsValid.Should().BeTrue();
        if (wire != "unknown")
        {
            parameter.WireValue.Should().Be(wire);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Explicit_null_survives_snapshot_copy_and_reset(bool chars)
    {
        var (parameter, control) = Create(chars);
        control.Reset();
        var copy = ((EnumState)control.GetCurrentValue()).Copy();
        control.SetValue(new EnumState(["a", "b", "c"]) { ["a"] = true });
        control.SetValue(copy);

        parameter.SetValueFromControl(control).IsValid.Should().BeTrue();

        parameter.WireValue.Should().BeNull();
    }

    [Theory]
    [InlineData(false, "aa")]
    [InlineData(true, "A")]
    public void Non_inverted_lists_keep_selected_wire_values(bool chars, string expected)
    {
        var (parameter, control) = Create(chars, invert: false);
        control.SetValue(new EnumState(["a", "b", "c"]) { ["a"] = true });

        parameter.SetValueFromControl(control).IsValid.Should().BeTrue();

        parameter.WireValue.Should().Be(expected);
    }
}
