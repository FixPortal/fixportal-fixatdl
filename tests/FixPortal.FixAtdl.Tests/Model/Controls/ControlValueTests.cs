using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Controls;
using FixPortal.FixAtdl.Model.Controls.Support;
using FixPortal.FixAtdl.Model.Elements;

namespace FixPortal.FixAtdl.Tests.Model.Controls;

// ============================================================
// TEXT controls — TextControlBase (TextField_t, Label_t, HiddenField_t)
// ============================================================
public class TextControlTests
{
    [Fact]
    public void TextField_init_value_round_trips()
    {
        var control = new TextField_t("c1") { InitValue = "hello" };
        control.LoadInitValue(FixFieldValueProvider.Empty);
        control.GetCurrentValue().Should().Be("hello");
    }

    [Fact]
    public void TextField_set_value_replaces_content()
    {
        var control = new TextField_t("c1") { InitValue = "hello" };
        control.LoadInitValue(FixFieldValueProvider.Empty);

        control.SetValue("world");
        control.GetCurrentValue().Should().Be("world");
    }

    [Fact]
    public void TextField_reset_yields_null()
    {
        // NOTE: Reset() sets _value = null; GetCurrentValue() returns _value! so null is returned.
        var control = new TextField_t("c1") { InitValue = "hello" };
        control.LoadInitValue(FixFieldValueProvider.Empty);

        control.Reset();
        control.GetCurrentValue().Should().BeNull();
    }

    [Fact]
    public void TextField_no_init_value_yields_null_after_load()
    {
        var control = new TextField_t("c2");
        control.LoadInitValue(FixFieldValueProvider.Empty);
        control.GetCurrentValue().Should().BeNull();
    }

    [Fact]
    public void TextField_has_no_enumerated_state()
    {
        var control = new TextField_t("c3");
        control.HasEnumeratedState.Should().BeFalse();
    }

    [Fact]
    public void Label_set_and_get_round_trips()
    {
        var control = new Label_t("lbl") { InitValue = "Buy" };
        control.LoadInitValue(FixFieldValueProvider.Empty);
        control.GetCurrentValue().Should().Be("Buy");
    }

    [Fact]
    public void HiddenField_set_and_get_round_trips()
    {
        var control = new HiddenField_t("hf") { InitValue = "42" };
        control.LoadInitValue(FixFieldValueProvider.Empty);
        control.GetCurrentValue().Should().Be("42");
    }

    [Fact]
    public void HiddenField_reset_yields_null()
    {
        var control = new HiddenField_t("hf") { InitValue = "42" };
        control.LoadInitValue(FixFieldValueProvider.Empty);
        control.Reset();
        control.GetCurrentValue().Should().BeNull();
    }
}

// ============================================================
// BINARY controls — BinaryControlBase (CheckBox_t, RadioButton_t)
// ============================================================
public class BinaryControlTests
{
    [Fact]
    public void CheckBox_init_value_true_loads()
    {
        var control = new CheckBox_t("chk") { InitValue = true };
        control.LoadInitValue(FixFieldValueProvider.Empty);
        control.GetCurrentValue().Should().Be(true);
    }

    [Fact]
    public void CheckBox_init_value_false_loads()
    {
        var control = new CheckBox_t("chk") { InitValue = false };
        control.LoadInitValue(FixFieldValueProvider.Empty);
        control.GetCurrentValue().Should().Be(false);
    }

    [Fact]
    public void CheckBox_no_init_value_defaults_to_false()
    {
        // NOTE: BinaryControlBase.LoadDefaultFromInitValue() sets _value = false when InitValue is null.
        var control = new CheckBox_t("chk");
        control.LoadInitValue(FixFieldValueProvider.Empty);
        control.GetCurrentValue().Should().Be(false);
    }

    [Fact]
    public void CheckBox_set_value_bool_round_trips()
    {
        var control = new CheckBox_t("chk");
        control.LoadInitValue(FixFieldValueProvider.Empty);

        control.SetValue(true);
        control.GetCurrentValue().Should().Be(true);

        control.SetValue(false);
        control.GetCurrentValue().Should().Be(false);
    }

    [Fact]
    public void CheckBox_reset_yields_null()
    {
        // NOTE: BinaryControlBase.Reset() sets _value = null (unlike no-init which defaults to false).
        var control = new CheckBox_t("chk") { InitValue = true };
        control.LoadInitValue(FixFieldValueProvider.Empty);
        control.Reset();
        control.GetCurrentValue().Should().BeNull();
    }

    [Fact]
    public void CheckBox_set_value_null_sets_null()
    {
        var control = new CheckBox_t("chk") { InitValue = true };
        control.LoadInitValue(FixFieldValueProvider.Empty);
        control.SetValue((object)null!);
        control.GetCurrentValue().Should().BeNull();
    }

    [Fact]
    public void CheckBox_set_value_wire_Y_sets_true()
    {
        // NOTE: TryParseBooleanWireValue accepts "Y" as true when HasEnumeratedState is false.
        var control = new CheckBox_t("chk");
        control.LoadInitValue(FixFieldValueProvider.Empty);
        control.SetValue("Y");
        control.GetCurrentValue().Should().Be(true);
    }

    [Fact]
    public void CheckBox_set_value_wire_N_sets_false()
    {
        var control = new CheckBox_t("chk");
        control.LoadInitValue(FixFieldValueProvider.Empty);
        control.SetValue("N");
        control.GetCurrentValue().Should().Be(false);
    }

    [Fact]
    public void CheckBox_no_enumerated_state_without_enum_refs()
    {
        var control = new CheckBox_t("chk");
        control.HasEnumeratedState.Should().BeFalse();
    }

    [Fact]
    public void CheckBox_has_enumerated_state_when_enum_refs_set()
    {
        var control = new CheckBox_t("chk")
        {
            CheckedEnumRef = "Y",
            UncheckedEnumRef = "N"
        };
        control.HasEnumeratedState.Should().BeTrue();
    }

    [Fact]
    public void RadioButton_init_value_true_loads()
    {
        var control = new RadioButton_t("rb") { InitValue = true };
        control.LoadInitValue(FixFieldValueProvider.Empty);
        control.GetCurrentValue().Should().Be(true);
    }

    [Fact]
    public void RadioButton_no_init_value_defaults_to_false()
    {
        var control = new RadioButton_t("rb");
        control.LoadInitValue(FixFieldValueProvider.Empty);
        control.GetCurrentValue().Should().Be(false);
    }

    [Fact]
    public void RadioButton_reset_yields_null()
    {
        var control = new RadioButton_t("rb") { InitValue = true };
        control.LoadInitValue(FixFieldValueProvider.Empty);
        control.Reset();
        control.GetCurrentValue().Should().BeNull();
    }
}

// ============================================================
// NUMERIC controls — NumericControlBase (SingleSpinner_t, DoubleSpinner_t, Slider_t)
// ============================================================
public class NumericControlTests
{
    [Fact]
    public void SingleSpinner_init_value_loads()
    {
        var control = new SingleSpinner_t("ss") { InitValue = 3.5m };
        control.LoadInitValue(FixFieldValueProvider.Empty);
        control.GetCurrentValue().Should().Be(3.5m);
    }

    [Fact]
    public void SingleSpinner_set_value_decimal_round_trips()
    {
        var control = new SingleSpinner_t("ss") { InitValue = 1m };
        control.LoadInitValue(FixFieldValueProvider.Empty);

        control.SetValue(9.99m);
        control.GetCurrentValue().Should().Be(9.99m);
    }

    [Fact]
    public void SingleSpinner_set_value_string_numeric_round_trips()
    {
        var control = new SingleSpinner_t("ss");
        control.LoadInitValue(FixFieldValueProvider.Empty);

        control.SetValue("12.5");
        control.GetCurrentValue().Should().Be(12.5m);
    }

    [Fact]
    public void SingleSpinner_reset_yields_null()
    {
        var control = new SingleSpinner_t("ss") { InitValue = 7m };
        control.LoadInitValue(FixFieldValueProvider.Empty);
        control.Reset();
        control.GetCurrentValue().Should().BeNull();
    }

    [Fact]
    public void SingleSpinner_no_init_value_yields_null()
    {
        var control = new SingleSpinner_t("ss");
        control.LoadInitValue(FixFieldValueProvider.Empty);
        control.GetCurrentValue().Should().BeNull();
    }

    [Fact]
    public void SingleSpinner_set_null_yields_null()
    {
        var control = new SingleSpinner_t("ss") { InitValue = 5m };
        control.LoadInitValue(FixFieldValueProvider.Empty);
        control.SetValue((object)null!);
        control.GetCurrentValue().Should().BeNull();
    }

    [Fact]
    public void SingleSpinner_has_no_enumerated_state()
    {
        var control = new SingleSpinner_t("ss");
        control.HasEnumeratedState.Should().BeFalse();
    }

    [Fact]
    public void DoubleSpinner_init_value_loads()
    {
        var control = new DoubleSpinner_t("ds") { InitValue = 2.5m };
        control.LoadInitValue(FixFieldValueProvider.Empty);
        control.GetCurrentValue().Should().Be(2.5m);
    }

    [Fact]
    public void DoubleSpinner_reset_yields_null()
    {
        var control = new DoubleSpinner_t("ds") { InitValue = 2.5m };
        control.LoadInitValue(FixFieldValueProvider.Empty);
        control.Reset();
        control.GetCurrentValue().Should().BeNull();
    }

    // NOTE: Slider_t is actually a ListControlBase (not NumericControlBase) — it selects from
    // a list of options, not a free numeric value. InitValue is string (an EnumId), not decimal.
    [Fact]
    public void Slider_init_value_loads()
    {
        var control = new Slider_t("sl");
        control.ListItems.Add(new ListItem_t { EnumId = "LOW", UiRep = "Low" });
        control.ListItems.Add(new ListItem_t { EnumId = "HIGH", UiRep = "High" });
        control.InitValue = "LOW";
        control.LoadInitValue(FixFieldValueProvider.Empty);
        var state = (EnumState)control.GetCurrentValue();
        state["LOW"].Should().BeTrue();
    }

    [Fact]
    public void Slider_reset_yields_all_false()
    {
        var control = new Slider_t("sl");
        control.ListItems.Add(new ListItem_t { EnumId = "LOW", UiRep = "Low" });
        control.InitValue = "LOW";
        control.LoadInitValue(FixFieldValueProvider.Empty);
        control.Reset();
        var state = (EnumState)control.GetCurrentValue();
        state["LOW"].Should().BeFalse();
    }
}

// ============================================================
// LIST controls — ListControlBase (DropDownList, SingleSelect, MultiSelect, RadioButtonList, CheckBoxList)
// ============================================================
public class ListControlTests
{
    private static DropDownList_t MakeDropDown(string initValue = null!)
    {
        var ctrl = new DropDownList_t("ddl");
        ctrl.ListItems.Add(new ListItem_t { EnumId = "A", UiRep = "Alpha" });
        ctrl.ListItems.Add(new ListItem_t { EnumId = "B", UiRep = "Beta" });
        ctrl.ListItems.Add(new ListItem_t { EnumId = "C", UiRep = "Gamma" });
        if (initValue != null)
        {
            ctrl.InitValue = initValue;
        }
        return ctrl;
    }

    [Fact]
    public void DropDown_load_init_value_sets_enum_state()
    {
        var ctrl = MakeDropDown("B");
        ctrl.LoadInitValue(FixFieldValueProvider.Empty);

        var state = (EnumState)ctrl.GetCurrentValue();
        state["A"].Should().BeFalse();
        state["B"].Should().BeTrue();
        state["C"].Should().BeFalse();
    }

    [Fact]
    public void DropDown_no_init_value_all_unselected()
    {
        var ctrl = MakeDropDown();
        ctrl.LoadInitValue(FixFieldValueProvider.Empty);

        var state = (EnumState)ctrl.GetCurrentValue();
        state["A"].Should().BeFalse();
        state["B"].Should().BeFalse();
        state["C"].Should().BeFalse();
    }

    [Fact]
    public void DropDown_reset_clears_all_selections()
    {
        var ctrl = MakeDropDown("A");
        ctrl.LoadInitValue(FixFieldValueProvider.Empty);

        ctrl.Reset();

        var state = (EnumState)ctrl.GetCurrentValue();
        state["A"].Should().BeFalse();
    }

    [Fact]
    public void DropDown_has_enumerated_state_after_load()
    {
        var ctrl = MakeDropDown();
        ctrl.LoadInitValue(FixFieldValueProvider.Empty);
        ctrl.HasEnumeratedState.Should().BeTrue();
    }

    [Fact]
    public void DropDown_has_list_items()
    {
        var ctrl = MakeDropDown();
        ctrl.HasListItems.Should().BeTrue();
    }

    [Fact]
    public void SingleSelectList_init_value_sets_single_selection()
    {
        var ctrl = new SingleSelectList_t("ssl");
        ctrl.ListItems.Add(new ListItem_t { EnumId = "X", UiRep = "X" });
        ctrl.ListItems.Add(new ListItem_t { EnumId = "Y", UiRep = "Y" });
        ctrl.InitValue = "X";
        ctrl.LoadInitValue(FixFieldValueProvider.Empty);

        var state = (EnumState)ctrl.GetCurrentValue();
        state["X"].Should().BeTrue();
        state["Y"].Should().BeFalse();
    }

    [Fact]
    public void MultiSelectList_init_value_sets_multiple_selections()
    {
        var ctrl = new MultiSelectList_t("msl");
        ctrl.ListItems.Add(new ListItem_t { EnumId = "P", UiRep = "P" });
        ctrl.ListItems.Add(new ListItem_t { EnumId = "Q", UiRep = "Q" });
        ctrl.ListItems.Add(new ListItem_t { EnumId = "R", UiRep = "R" });
        ctrl.InitValue = "P Q";
        ctrl.LoadInitValue(FixFieldValueProvider.Empty);

        var state = (EnumState)ctrl.GetCurrentValue();
        state["P"].Should().BeTrue();
        state["Q"].Should().BeTrue();
        state["R"].Should().BeFalse();
    }

    [Fact]
    public void RadioButtonList_loads_and_resets()
    {
        var ctrl = new RadioButtonList_t("rbl");
        ctrl.ListItems.Add(new ListItem_t { EnumId = "M", UiRep = "M" });
        ctrl.ListItems.Add(new ListItem_t { EnumId = "N", UiRep = "N" });
        ctrl.InitValue = "N";
        ctrl.LoadInitValue(FixFieldValueProvider.Empty);

        ((EnumState)ctrl.GetCurrentValue())["N"].Should().BeTrue();

        ctrl.Reset();
        ((EnumState)ctrl.GetCurrentValue())["N"].Should().BeFalse();
    }

    [Fact]
    public void CheckBoxList_loads_and_resets()
    {
        var ctrl = new CheckBoxList_t("cbl");
        ctrl.ListItems.Add(new ListItem_t { EnumId = "E1", UiRep = "E1" });
        ctrl.ListItems.Add(new ListItem_t { EnumId = "E2", UiRep = "E2" });
        ctrl.InitValue = "E1";
        ctrl.LoadInitValue(FixFieldValueProvider.Empty);

        ((EnumState)ctrl.GetCurrentValue())["E1"].Should().BeTrue();
        ((EnumState)ctrl.GetCurrentValue())["E2"].Should().BeFalse();

        ctrl.Reset();
        ((EnumState)ctrl.GetCurrentValue())["E1"].Should().BeFalse();
    }

    [Fact]
    public void DropDown_set_value_enumstate_updates_selection()
    {
        var ctrl = MakeDropDown("A");
        ctrl.LoadInitValue(FixFieldValueProvider.Empty);

        var newState = new EnumState(["A", "B", "C"]);
        newState["C"] = true;
        ctrl.SetValue(newState);

        var state = (EnumState)ctrl.GetCurrentValue();
        state["A"].Should().BeFalse();
        state["C"].Should().BeTrue();
    }

    [Fact]
    public void EditableDropDown_allows_non_enum_value()
    {
        var ctrl = new EditableDropDownList_t("edl");
        ctrl.ListItems.Add(new ListItem_t { EnumId = "Z1", UiRep = "Z1" });
        ctrl.InitValue = "FreeText";
        ctrl.LoadInitValue(FixFieldValueProvider.Empty);

        // NOTE: EditableDropDownList_t overrides IsNonEnumValueAllowed to true, so the free-text
        // value is accepted and stored as the NonEnumValue on the EnumState.
        var state = (EnumState)ctrl.GetCurrentValue();
        state.NonEnumValue.Should().Be("FreeText");
    }
}

// ============================================================
// ENUM STATE — EnumState directly
// ============================================================
public class EnumStateTests
{
    [Fact]
    public void EnumState_ctor_initialises_all_false()
    {
        var state = new EnumState(["A", "B", "C"]);
        state["A"].Should().BeFalse();
        state["B"].Should().BeFalse();
        state["C"].Should().BeFalse();
    }

    [Fact]
    public void EnumState_count_matches_ids()
    {
        var state = new EnumState(["X", "Y"]);
        state.Count.Should().Be(2);
    }

    [Fact]
    public void EnumState_indexer_set_and_get()
    {
        var state = new EnumState(["A", "B"]);
        state["A"] = true;
        state["A"].Should().BeTrue();
        state["B"].Should().BeFalse();
    }

    [Fact]
    public void EnumState_copy_ctor_deep_clones()
    {
        var original = new EnumState(["A", "B"]);
        original["A"] = true;

        var copy = new EnumState(original);
        copy["A"].Should().BeTrue();

        // mutating copy does not affect original
        copy["A"] = false;
        original["A"].Should().BeTrue();
    }

    [Fact]
    public void EnumState_copy_ctor_null_throws_argument_exception()
    {
        var act = () => new EnumState((EnumState)null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EnumState_null_array_ctor_throws()
    {
        var act = () => new EnumState((string[])null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EnumState_clear_all_resets_bits()
    {
        var state = new EnumState(["A", "B"]);
        state["A"] = true;
        state.ClearAll();
        state["A"].Should().BeFalse();
    }

    [Fact]
    public void EnumState_is_valid_enum_id()
    {
        var state = new EnumState(["A", "B"]);
        state.IsValidEnumId("A").Should().BeTrue();
        state.IsValidEnumId("Z").Should().BeFalse();
    }

    [Fact]
    public void EnumState_invalid_indexer_get_throws()
    {
        var state = new EnumState(["A"]);
        var act = () => { _ = state["INVALID"]; };
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void EnumState_invalid_indexer_set_throws()
    {
        var state = new EnumState(["A"]);
        var act = () => { state["INVALID"] = true; };
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void EnumState_get_first_selected_enum_id_returns_empty_when_none()
    {
        var state = new EnumState(["A", "B"]);
        state.GetFirstSelectedEnumId().Should().Be(string.Empty);
    }

    [Fact]
    public void EnumState_get_first_selected_enum_id_returns_id()
    {
        var state = new EnumState(["A", "B", "C"]);
        state["B"] = true;
        state.GetFirstSelectedEnumId().Should().Be("B");
    }

    [Fact]
    public void EnumState_get_index_of_enum_id_returns_index()
    {
        var state = new EnumState(["A", "B", "C"]);
        state.GetIndexOfEnumId("B").Should().Be(1);
        state.GetIndexOfEnumId("Z").Should().Be(-1);
    }

    [Fact]
    public void EnumState_non_enum_value_clears_bits()
    {
        var state = new EnumState(["A", "B"]);
        state["A"] = true;
        state.NonEnumValue = "custom";
        state["A"].Should().BeFalse();
        state.NonEnumValue.Should().Be("custom");
    }

    [Fact]
    public void EnumState_equals_same_state()
    {
        var a = new EnumState(["X", "Y"]);
        a["X"] = true;
        var b = new EnumState(["X", "Y"]);
        b["X"] = true;
        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void EnumState_not_equals_different_state()
    {
        var a = new EnumState(["X", "Y"]);
        a["X"] = true;
        var b = new EnumState(["X", "Y"]);
        b["Y"] = true;
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void EnumState_equals_non_enum_state_returns_false()
    {
        var state = new EnumState(["A"]);
        state.Equals("A string").Should().BeFalse();
    }

    [Fact]
    public void EnumState_get_hash_code_equal_states_same_hash()
    {
        var a = new EnumState(["X", "Y"]);
        a["X"] = true;
        var b = new EnumState(["X", "Y"]);
        b["X"] = true;
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void EnumState_load_init_value_sets_bits()
    {
        var state = new EnumState(["A", "B", "C"]);
        state.LoadInitValue("A C", false);
        state["A"].Should().BeTrue();
        state["B"].Should().BeFalse();
        state["C"].Should().BeTrue();
    }

    [Fact]
    public void EnumState_load_init_value_invalid_no_allow_throws()
    {
        var state = new EnumState(["A", "B"]);
        var act = () => state.LoadInitValue("INVALID", false);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void EnumState_load_init_value_non_enum_allowed_stores_as_non_enum()
    {
        var state = new EnumState(["A", "B"]);
        state.LoadInitValue("freetext", true);
        state.NonEnumValue.Should().Be("freetext");
    }

    [Fact]
    public void EnumState_copy_method_deep_clones()
    {
        var original = new EnumState(["A", "B"]);
        original["B"] = true;
        var copy = original.Copy();
        copy["B"].Should().BeTrue();
        copy["A"] = true;
        original["A"].Should().BeFalse();
    }

    [Fact]
    public void EnumState_update_from_copies_state()
    {
        var target = new EnumState(["A", "B"]);
        var source = new EnumState(["A", "B"]);
        source["A"] = true;

        target.UpdateFrom(source);
        target["A"].Should().BeTrue();
        target["B"].Should().BeFalse();
    }

    [Fact]
    public void EnumState_to_string_formats_correctly()
    {
        var state = new EnumState(["A", "B"]);
        state["A"] = true;
        var s = state.ToString();
        s.Should().Contain("A=true");
        s.Should().Contain("B=false");
    }

    [Fact]
    public void EnumState_get_first_selected_index_returns_neg1_when_empty()
    {
        var state = new EnumState(["A", "B"]);
        state.GetFirstSelectedEnumIdIndex().Should().Be(-1);
    }

    [Fact]
    public void EnumState_get_first_selected_index_returns_correct_index()
    {
        var state = new EnumState(["A", "B", "C"]);
        state["C"] = true;
        state.GetFirstSelectedEnumIdIndex().Should().Be(2);
    }
}

// ============================================================
// CLOCK_T — extra paths beyond ClockTimeProviderTests.cs
// ============================================================
public class ClockControlTests
{
    [Fact]
    public void Clock_no_init_value_yields_null()
    {
        var clock = new Clock_t("clk");
        clock.LoadInitValue(FixFieldValueProvider.Empty);
        clock.GetCurrentValue().Should().BeNull();
    }

    [Fact]
    public void Clock_init_value_mode_0_uses_init_value()
    {
        var dt = new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Unspecified);
        var clock = new Clock_t("clk") { InitValue = dt, InitValueMode = 0 };
        clock.LoadInitValue(FixFieldValueProvider.Empty);
        clock.GetCurrentValue().Should().Be(dt);
    }

    [Fact]
    public void Clock_init_value_mode_null_uses_init_value()
    {
        var dt = new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Unspecified);
        var clock = new Clock_t("clk") { InitValue = dt, InitValueMode = null };
        clock.LoadInitValue(FixFieldValueProvider.Empty);
        clock.GetCurrentValue().Should().Be(dt);
    }

    [Fact]
    public void Clock_set_value_datetime_round_trips()
    {
        var clock = new Clock_t("clk");
        var dt = new DateTime(2026, 6, 1, 10, 30, 0, DateTimeKind.Unspecified);
        clock.SetValue(dt);
        clock.GetCurrentValue().Should().Be(dt);
    }

    [Fact]
    public void Clock_set_value_null_yields_null()
    {
        var clock = new Clock_t("clk");
        var dt = new DateTime(2026, 6, 1, 10, 30, 0, DateTimeKind.Unspecified);
        clock.SetValue(dt);
        clock.SetValue((object)null!);
        clock.GetCurrentValue().Should().BeNull();
    }

    [Fact]
    public void Clock_reset_yields_null()
    {
        var dt = new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Unspecified);
        var clock = new Clock_t("clk") { InitValue = dt };
        clock.LoadInitValue(FixFieldValueProvider.Empty);
        clock.Reset();
        clock.GetCurrentValue().Should().BeNull();
    }

    [Fact]
    public void Clock_invalid_init_value_mode_throws()
    {
        var clock = new Clock_t("clk")
        {
            InitValue = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Unspecified),
            InitValueMode = 2
        };
        var act = () => clock.LoadInitValue(FixFieldValueProvider.Empty);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Clock_has_no_enumerated_state()
    {
        var clock = new Clock_t("clk");
        clock.HasEnumeratedState.Should().BeFalse();
    }

    [Fact]
    public void Clock_local_mkt_tz_property_set_and_get()
    {
        var clock = new Clock_t("clk") { LocalMktTz = "America/New_York" };
        clock.LocalMktTz.Should().Be("America/New_York");
    }
}
