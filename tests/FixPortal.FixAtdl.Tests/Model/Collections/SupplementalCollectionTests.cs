using System.Collections.Specialized;
using System.Text;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Controls;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Model.Types;
using FixPortal.FixAtdl.Utility;
using FixPortal.FixAtdl.Validation;
using FixPortal.FixAtdl.Xml;

namespace FixPortal.FixAtdl.Tests.Model.Collections;

/// <summary>
/// Supplemental coverage for ListItemCollection, EditRefCollection, StrategyEditCollection,
/// and ReadOnlyControlCollection (constructor / Contains / indexer paths).
/// </summary>
public class SupplementalCollectionTests
{
    // -----------------------------------------------------------------------
    // Fixture loader
    // -----------------------------------------------------------------------

    private static Strategy_t LoadTwap()
    {
        string xml = FixtureFiles.ReadAllText("Fixtures/twap.xml");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return new StrategiesReader().Load(stream).Strategies[0];
    }

    // -----------------------------------------------------------------------
    // ListItemCollection
    // -----------------------------------------------------------------------

    [Fact]
    public void ListItemCollection_add_and_retrieve_by_enum_id()
    {
        var items = new ListItemCollection
        {
            new ListItem_t { EnumId = "BUY", UiRep = "Buy" },
            new ListItem_t { EnumId = "SELL", UiRep = "Sell" },
        };

        items.Count.Should().Be(2);
        items["BUY"].UiRep.Should().Be("Buy");
        items.Contains("SELL").Should().BeTrue();
        items.Contains("NOPE").Should().BeFalse();
    }

    [Fact]
    public void ListItemCollection_HasItems_and_EnumIds()
    {
        var empty = new ListItemCollection();
        empty.HasItems.Should().BeFalse();

        var items = new ListItemCollection
        {
            new ListItem_t { EnumId = "A", UiRep = "Alpha" },
            new ListItem_t { EnumId = "B", UiRep = "Beta" },
        };

        items.HasItems.Should().BeTrue();
        items.EnumIds.Should().BeEquivalentTo("A", "B");
    }

    [Fact]
    public void ListItemCollection_CopyFrom_copies_all_items()
    {
        var source = new List<ListItem_t>
        {
            new() { EnumId = "X", UiRep = "Ex" },
            new() { EnumId = "Y", UiRep = "Why" },
        };

        var items = new ListItemCollection();
        items.CopyFrom(source);

        items.Count.Should().Be(2);
        items["X"].UiRep.Should().Be("Ex");
    }

    [Fact]
    public void ListItemCollection_duplicate_key_throws_DuplicateKeyException()
    {
        // ReSharper disable once CollectionNeverQueried.Local
        var items = new ListItemCollection
        {
            new ListItem_t { EnumId = "DUP", UiRep = "First" },
        };

        var act = () => items.Add(new ListItem_t { EnumId = "DUP", UiRep = "Second" });
        act.Should().Throw<DuplicateKeyException>().WithMessage("*'DUP'*ListItems*");
    }

    // -----------------------------------------------------------------------
    // EditRefCollection
    // -----------------------------------------------------------------------

    [Fact]
    public void EditRefCollection_default_ctor_creates_empty_collection()
    {
        var refs = new EditRefCollection<IParameter>();
        refs.Count.Should().Be(0);
        refs.HasEditRef("any").Should().BeFalse();
    }

    [Fact]
    public void EditRefCollection_HasEditRef_after_add()
    {
        EditRefCollection<IParameter> refs = [new EditRef_t<IParameter>("editA")];

        refs.HasEditRef("editA").Should().BeTrue();
        refs.HasEditRef("editB").Should().BeFalse();
    }

    [Fact]
    public void EditRefCollection_registers_added_items_with_evaluating_collection()
    {
        var evaluating = new EditEvaluatingCollection<IParameter> { LogicOperator = LogicOperator_t.Or };
        EditRefCollection<IParameter> refs = new(evaluating);
        var reference = new EditRef_t<IParameter>("editA");

        refs.Add(reference);

        refs.Should().Contain(reference);
        evaluating.Should().Contain(reference);
    }

    // -----------------------------------------------------------------------
    // StrategyEditCollection — empty and EvaluateAll paths
    // -----------------------------------------------------------------------

    [Fact]
    public void StrategyEditCollection_EvaluateAll_empty_collection_returns_true()
    {
        var col = new StrategyEditCollection();
        col.EvaluateAll(FixFieldValueProvider.Empty, shortCircuit: false).Should().BeTrue();
    }

    [Fact]
    public void StrategyEditCollection_EvaluateAll_with_a_resolved_edit_returns_true_when_the_edit_passes()
    {
        // Load and resolve from the TWAP fixture so StrategyEdit_t.Evaluate() works.
        var twap = LoadTwap();
        twap.Parameters["Participation"].WireValue = "50";

        // twap.xml carries no StrategyEdits, so add one in code (matching the pattern of the
        // failing-path tests below) and let ResolveAll wire it to the Participation parameter.
        var edit = new Edit_t<IParameter>
        {
            Field = "Participation",
            Operator = Operator_t.Equal,
            Value = "50", // Participation is "50", so this edit passes
        };
        var strategyEdit = new StrategyEdit_t { Edit = edit, ErrorMessage = "err" };
        twap.StrategyEdits.Add(strategyEdit);

        twap.StrategyEdits.ResolveAll(twap);

        twap.StrategyEdits.EvaluateAll(FixFieldValueProvider.Empty, shortCircuit: false).Should().BeTrue();
        strategyEdit.CurrentState.Should().BeTrue();
    }

    [Fact]
    public void StrategyEditCollection_EvaluateAll_returns_false_when_edit_fails()
    {
        var twap = LoadTwap();
        twap.Parameters["Participation"].WireValue = "30";

        var edit = new Edit_t<IParameter>
        {
            Field = "Participation",
            Operator = Operator_t.Equal,
            Value = "50", // will evaluate to false
        };
        ((IResolvable<Strategy_t, IParameter>)edit).Resolve(twap, twap.Parameters);

        var strategyEdit = new StrategyEdit_t { Edit = edit, ErrorMessage = "err" };
        var col = new StrategyEditCollection { strategyEdit };

        col.EvaluateAll(FixFieldValueProvider.Empty, shortCircuit: false).Should().BeFalse();
        col.EvaluateAll(FixFieldValueProvider.Empty, shortCircuit: true).Should().BeFalse();
    }

    [Fact]
    public void StrategyEditCollection_EvaluateAll_shortCircuit_true_breaks_early()
    {
        var twap = LoadTwap();
        twap.Parameters["Participation"].WireValue = "30";

        var edit1 = new Edit_t<IParameter>
        {
            Field = "Participation",
            Operator = Operator_t.Equal,
            Value = "50", // will be false
        };
        ((IResolvable<Strategy_t, IParameter>)edit1).Resolve(twap, twap.Parameters);

        var strategyEdit1 = new StrategyEdit_t { Edit = edit1, ErrorMessage = "Failed" };
        var strategyEdit2 = new StrategyEdit_t { Edit = new Edit_t<IParameter>(), ErrorMessage = "Throws" };

        var collection = new StrategyEditCollection { strategyEdit1, strategyEdit2 };

        // With shortCircuit: true, it should return false and not throw.
        collection.EvaluateAll(FixFieldValueProvider.Empty, shortCircuit: true).Should().BeFalse();

        // With shortCircuit: false, it should evaluate all and throw.
        var act = () => collection.EvaluateAll(FixFieldValueProvider.Empty, shortCircuit: false);
        act.Should().Throw<InvalidOperationException>();
    }

    // -----------------------------------------------------------------------
    // ReadOnlyControlCollection — construction, Contains, indexer
    // -----------------------------------------------------------------------

    [Fact]
    public void ReadOnlyControlCollection_loaded_strategy_contains_expected_controls()
    {
        var twap = LoadTwap();
        // Control IDs from twap.xml: c_StartTime, c_EndTime, c_Part
        twap.Controls.Contains("c_StartTime").Should().BeTrue();
        twap.Controls.Contains("c_EndTime").Should().BeTrue();
        twap.Controls.Contains("c_Part").Should().BeTrue();
        twap.Controls.Contains("doesNotExist").Should().BeFalse();
    }

    [Fact]
    public void ReadOnlyControlCollection_indexer_throws_for_missing_key()
    {
        var twap = LoadTwap();
        var act = () => twap.Controls["doesNotExist"];
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void ReadOnlyControlCollection_indexer_returns_control_for_known_key()
    {
        var twap = LoadTwap();
        // Known control IDs from twap.xml
        var control = twap.Controls["c_StartTime"];
        control.Should().NotBeNull();
        control.Id.Should().Be("c_StartTime");
    }

    [Fact]
    public void ReadOnlyControlCollection_replace_with_duplicate_id_throws_DuplicateKeyException()
    {
        var strategy = new Strategy_t();
        var panel = new StrategyPanel_t(strategy);
        var ctrl1 = new TextField_t("c_One");
        var ctrl2 = new TextField_t("c_Two");
        panel.Controls.Add(ctrl1);
        panel.Controls.Add(ctrl2);

        // ctrlDup has same ID as ctrl2
        var ctrlDup = new TextField_t("c_Two");
        var act = () => panel.Controls[0] = ctrlDup; // set ctrl1 to ctrlDup, triggering Replace
        act.Should().Throw<DuplicateKeyException>().WithMessage("*'c_Two'*Controls*");
    }

    // -----------------------------------------------------------------------
    // ReadOnlyControlCollection — ungrouped radio value source (R22)
    // -----------------------------------------------------------------------

    [Fact]
    public void GetParameterValueSource_ungrouped_radios_resolve_the_selected_panel_sibling()
    {
        // R22: radioGroup is optional; ungrouped radios sharing a ParameterRef act as an implicit group
        // with their panel siblings, so the selected sibling is the parameter's value source.
        var strategy = new Strategy_t();
        var panel = new StrategyPanel_t(strategy);
        var radioA = new RadioButton_t("r_A") { ParameterRef = "P" };
        var radioB = new RadioButton_t("r_B") { ParameterRef = "P" };
        panel.Controls.Add(radioA);
        panel.Controls.Add(radioB);

        radioB.SetValue(true);

        strategy.Controls.GetParameterValueSource(radioA).Should().BeSameAs(radioB);
    }

    [Fact]
    public void GetParameterValueSource_ungrouped_radios_fall_back_to_the_control_when_no_sibling_is_selected()
    {
        var strategy = new Strategy_t();
        var panel = new StrategyPanel_t(strategy);
        var radioA = new RadioButton_t("r_A") { ParameterRef = "P" };
        var radioB = new RadioButton_t("r_B") { ParameterRef = "P" };
        panel.Controls.Add(radioA);
        panel.Controls.Add(radioB);

        strategy.Controls.GetParameterValueSource(radioA).Should().BeSameAs(radioA);
    }

    [Fact]
    public void GetParameterValueSource_grouped_radios_still_resolve_across_the_whole_strategy()
    {
        // A named RadioGroup spans panels: the selected member may live on a different panel.
        var strategy = new Strategy_t();
        var panelA = new StrategyPanel_t(strategy);
        var panelB = new StrategyPanel_t(strategy);
        var radioA = new RadioButton_t("r_A") { ParameterRef = "P", RadioGroup = "g" };
        var radioB = new RadioButton_t("r_B") { ParameterRef = "P", RadioGroup = "g" };
        panelA.Controls.Add(radioA);
        panelB.Controls.Add(radioB);

        radioB.SetValue(true);

        strategy.Controls.GetParameterValueSource(radioA).Should().BeSameAs(radioB);
    }

    [Fact]
    public void GetParameterValueSource_ungrouped_lookup_ignores_radios_that_have_a_radio_group()
    {
        var strategy = new Strategy_t();
        var panel = new StrategyPanel_t(strategy);
        var radioA = new RadioButton_t("r_A") { ParameterRef = "P" };
        var grouped = new RadioButton_t("r_G") { ParameterRef = "P", RadioGroup = "g" };
        var radioB = new RadioButton_t("r_B") { ParameterRef = "P" };
        panel.Controls.Add(radioA);
        panel.Controls.Add(grouped);
        panel.Controls.Add(radioB);

        grouped.SetValue(true);

        strategy.Controls.GetParameterValueSource(radioA).Should().BeSameAs(radioA);

        radioB.SetValue(true);

        strategy.Controls.GetParameterValueSource(radioA).Should().BeSameAs(radioB);
    }

    [Fact]
    public void Helper_radio_toggle_uses_only_ungrouped_siblings_for_the_same_parameter()
    {
        var strategy = new Strategy_t();
        var panel = new StrategyPanel_t(strategy);
        // Unbound: a source ParameterRef is left at its inbound value and is not inferred.
        var source = new RadioButton_t("source") { RadioGroup = "" };
        var companion = new RadioButton_t("companion") { RadioGroup = "" };
        // Grouped: excluded by the ungrouped-sibling filter.
        var groupedSameParam = new RadioButton_t("groupedSameParam") { RadioGroup = "g" };
        // Ungrouped but a different parameter: excluded by the same-parameter filter.
        var ungroupedOtherParam = new RadioButton_t("ungroupedOtherParam") { ParameterRef = "Q", RadioGroup = "" };
        var helper = new CheckBox_t("helper") { ParameterRef = "H" };
        helper.StateRules.Add(ControlCollectionGuardTests.SourceEqualsTrue());
        // Radios start off so only the helper pass, selecting the sole ungrouped sibling with the
        // same (unset) parameter, can flip the companion.
        strategy.Parameters.Add(new Parameter_t<Boolean_t>("Q") { WireValue = "N" });
        strategy.Parameters.Add(new Parameter_t<Boolean_t>("H") { WireValue = "Y" });
        panel.Controls.Add(source);
        panel.Controls.Add(companion);
        panel.Controls.Add(groupedSameParam);
        panel.Controls.Add(ungroupedOtherParam);
        panel.Controls.Add(helper);
        source.SetValue(false);
        companion.SetValue(false);
        groupedSameParam.SetValue(false);

        strategy.Controls.UpdateValuesFromParameters(strategy.Parameters);

        companion.GetCurrentValue().Should().Be(true);
        source.GetCurrentValue().Should().Be(false);
        groupedSameParam.GetCurrentValue().Should().Be(false);
        ungroupedOtherParam.GetCurrentValue().Should().Be(false);
    }

    // -----------------------------------------------------------------------
    // ReadOnlyControlCollection — Reset removes only the sender's controls (R23)
    // -----------------------------------------------------------------------

    [Fact]
    public void Reset_from_one_panel_collection_removes_only_that_panels_controls()
    {
        // R23: Clear() on one panel's Controls must not drop controls belonging to another panel,
        // including panels not reachable from the strategy's layout root.
        var strategy = new Strategy_t();
        var attachedPanel = new StrategyPanel_t(strategy);
        var detachedPanel = new StrategyPanel_t(strategy);
        attachedPanel.Controls.Add(new TextField_t("c_Att"));
        detachedPanel.Controls.Add(new TextField_t("c_Det"));

        attachedPanel.Controls.Clear();

        strategy.Controls.Contains("c_Att").Should().BeFalse();
        strategy.Controls.Contains("c_Det").Should().BeTrue();
    }

    [Fact]
    public void Reset_then_readd_keeps_the_duplicate_id_guard_intact()
    {
        // After a Clear, re-adding the same Id must succeed (it was removed), while a genuinely
        // duplicated Id from another panel must still be rejected.
        var strategy = new Strategy_t();
        var panel = new StrategyPanel_t(strategy);
        var otherPanel = new StrategyPanel_t(strategy);
        panel.Controls.Add(new TextField_t("c_One"));
        otherPanel.Controls.Add(new TextField_t("c_Two"));

        panel.Controls.Clear();
        panel.Controls.Add(new TextField_t("c_One"));

        var act = () => panel.Controls.Add(new TextField_t("c_Two"));
        act.Should().Throw<DuplicateKeyException>();
    }

    // -----------------------------------------------------------------------
    // ControlCollection — Move/Remove bookkeeping (Low 25, Low 27)
    // -----------------------------------------------------------------------

    [Fact]
    public void Move_refreshes_layout_indexes()
    {
        var strategy = new Strategy_t();
        var panel = new StrategyPanel_t(strategy);
        var first = new TextField_t("c_First");
        var second = new TextField_t("c_Second");
        panel.Controls.Add(first);
        panel.Controls.Add(second);

        panel.Controls.Move(0, 1);

        first.Index.Should().Be(1);
        second.Index.Should().Be(0);
    }

    [Fact]
    public void Remove_detaches_the_control_from_its_panel()
    {
        var strategy = new Strategy_t();
        var panel = new StrategyPanel_t(strategy);
        var control = new TextField_t("c_One");
        panel.Controls.Add(control);

        panel.Controls.Remove(control);

        control.OwningStrategyPanel.Should().BeNull();
    }

    [Fact]
    public void Remove_finalizes_detachment_and_indexes_before_the_notification()
    {
        // CR13: ObservableCollection raises CollectionChanged from inside its mutation methods, so the
        // detach and index refresh must already have run when handlers observe the event.
        var strategy = new Strategy_t();
        var panel = new StrategyPanel_t(strategy);
        var first = new TextField_t("c_First");
        var second = new TextField_t("c_Second");
        panel.Controls.Add(first);
        panel.Controls.Add(second);

        bool? detachedWhenNotified = null;
        int? siblingIndexWhenNotified = null;
        panel.Controls.CollectionChanged += (_, args) =>
        {
            if (args.Action == NotifyCollectionChangedAction.Remove)
            {
                detachedWhenNotified = first.OwningStrategyPanel == null;
                siblingIndexWhenNotified = second.Index;
            }
        };

        panel.Controls.Remove(first);

        detachedWhenNotified.Should().BeTrue();
        siblingIndexWhenNotified.Should().Be(0);
    }

    [Fact]
    public void Move_refreshes_indexes_before_the_notification()
    {
        var strategy = new Strategy_t();
        var panel = new StrategyPanel_t(strategy);
        var first = new TextField_t("c_First");
        var second = new TextField_t("c_Second");
        panel.Controls.Add(first);
        panel.Controls.Add(second);

        int? firstIndexWhenNotified = null;
        panel.Controls.CollectionChanged += (_, args) =>
        {
            if (args.Action == NotifyCollectionChangedAction.Move)
            {
                firstIndexWhenNotified = first.Index;
            }
        };

        panel.Controls.Move(0, 1);

        firstIndexWhenNotified.Should().Be(1);
    }

    [Fact]
    public void Replace_detaches_the_old_control_but_keeps_a_same_instance_parented()
    {
        var strategy = new Strategy_t();
        var panel = new StrategyPanel_t(strategy);
        var original = new TextField_t("c_One");
        panel.Controls.Add(original);

        var replacement = new TextField_t("c_Two");
        panel.Controls[0] = replacement;

        original.OwningStrategyPanel.Should().BeNull();
        replacement.OwningStrategyPanel.Should().BeSameAs(panel);

        panel.Controls[0] = replacement;

        replacement.OwningStrategyPanel.Should().BeSameAs(panel);
    }

    [Fact]
    public void Clear_detaches_every_control_before_the_reset_notification()
    {
        // Clear raises a Reset notification with no OldItems, so detaching must happen in ClearItems
        // before handlers observe the event.
        var strategy = new Strategy_t();
        var panel = new StrategyPanel_t(strategy);
        var first = new TextField_t("c_First");
        var second = new TextField_t("c_Second");
        panel.Controls.Add(first);
        panel.Controls.Add(second);

        bool? detachedWhenNotified = null;
        panel.Controls.CollectionChanged += (_, args) =>
        {
            if (args.Action == NotifyCollectionChangedAction.Reset)
            {
                detachedWhenNotified = first.OwningStrategyPanel == null && second.OwningStrategyPanel == null;
            }
        };

        panel.Controls.Clear();

        panel.Controls.Should().BeEmpty();
        detachedWhenNotified.Should().BeTrue();
        first.OwningStrategyPanel.Should().BeNull();
        second.OwningStrategyPanel.Should().BeNull();
    }

    // -----------------------------------------------------------------------
    // Control_t.Id — ownership guard (CR7)
    // -----------------------------------------------------------------------

    [Fact]
    public void Id_rename_throws_once_the_control_belongs_to_a_panel()
    {
        // CR7: the strategy index keys controls by Id at insertion time, so renaming an owned control
        // would desynchronise every lookup; the guarded setter rejects it.
        var strategy = new Strategy_t();
        var panel = new StrategyPanel_t(strategy);
        var control = new TextField_t("c_One");
        panel.Controls.Add(control);

        var act = () => control.Id = "c_Renamed";

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Id_assignment_before_parenting_is_unrestricted()
    {
        var control = new TextField_t("c_One") { Id = "c_Two" };

        control.Id.Should().Be("c_Two");
    }

    [Fact]
    public void Id_same_value_assignment_is_a_no_op_once_parented()
    {
        var strategy = new Strategy_t();
        var panel = new StrategyPanel_t(strategy);
        var control = new TextField_t("c_One");
        panel.Controls.Add(control);

        var act = () => control.Id = "c_One";

        act.Should().NotThrow();
    }

    // -----------------------------------------------------------------------
    // ReadOnlyControlCollection — shared-parameter update dedup (Low 26)
    // -----------------------------------------------------------------------

    [Fact]
    public void TryUpdateParameterValues_reports_a_shared_radio_parameter_once()
    {
        // Ungrouped radio members share one parameter and resolve to a single value source
        // (GetParameterValueSource): a failing update must be reported once, not once per member.
        var strategy = new Strategy_t();
        var panel = new StrategyPanel_t(strategy);
        panel.Controls.Add(new RadioButton_t("r_A") { ParameterRef = "P" });
        panel.Controls.Add(new RadioButton_t("r_B") { ParameterRef = "P" });
        var parameter = Substitute.For<IParameter>();
        parameter.Name.Returns("P");
        parameter
            .SetValueFromControl(Arg.Any<Control_t>())
            .Returns(new ValidationResult(ValidationResult.ResultType.Invalid, "bad"));
        strategy.Parameters.Add(parameter);

        bool result = strategy.Controls.TryUpdateParameterValues(
            strategy.Parameters,
            shortCircuit: false,
            out IList<ValidationResult>? results
        );

        result.Should().BeFalse();
        results.Should().HaveCount(1);
    }

    [Fact]
    public void TryUpdateParameterValues_lets_non_radio_controls_sharing_a_parameter_overwrite_in_order()
    {
        // CR4: two non-radio controls may legitimately share one parameter; each must push its value
        // in collection order (last one wins). Only radio-group members dedup to a single update.
        var strategy = new Strategy_t();
        var panel = new StrategyPanel_t(strategy);
        panel.Controls.Add(new TextField_t("t_A") { ParameterRef = "P" });
        panel.Controls.Add(new TextField_t("t_B") { ParameterRef = "P" });
        var parameter = Substitute.For<IParameter>();
        parameter.Name.Returns("P");
        parameter.SetValueFromControl(Arg.Any<Control_t>()).Returns(ValidationResult.ValidResult);
        strategy.Parameters.Add(parameter);

        bool result = strategy.Controls.TryUpdateParameterValues(
            strategy.Parameters,
            shortCircuit: false,
            out IList<ValidationResult>? results
        );

        result.Should().BeTrue();
        results.Should().BeNull();
        parameter.Received(2).SetValueFromControl(Arg.Any<Control_t>());
    }

    [Fact]
    public void UpdateValuesFromParameters_resets_control_when_parameter_is_empty()
    {
        var strategy = new Strategy_t();
        var panel = new StrategyPanel_t(strategy);
        var parameter = new Parameter_t<String_t>("P") { WireValue = "old" };
        var control = new TextField_t("text") { ParameterRef = "P" };
        strategy.Parameters.Add(parameter);
        panel.Controls.Add(control);

        strategy.Controls.UpdateValuesFromParameters(strategy.Parameters);
        control.GetCurrentValue().Should().Be("old");

        parameter.Reset();
        strategy.Controls.UpdateValuesFromParameters(strategy.Parameters);

        control.GetCurrentValue().Should().BeNull();
    }

    [Fact]
    public void UpdateValuesFromParameters_keeps_the_source_when_the_helper_parameter_is_reset()
    {
        var strategy = new Strategy_t();
        var panel = new StrategyPanel_t(strategy);
        var source = new CheckBox_t("source") { ParameterRef = "S" };
        var helper = new CheckBox_t("helper") { ParameterRef = "P" };
        helper.StateRules.Add(ControlCollectionGuardTests.SourceEqualsTrue());
        strategy.Parameters.Add(new Parameter_t<Boolean_t>("S") { WireValue = "Y" });
        strategy.Parameters.Add(new Parameter_t<Boolean_t>("P") { WireValue = "Y" });
        panel.Controls.Add(source);
        panel.Controls.Add(helper);

        strategy.Controls.UpdateValuesFromParameters(strategy.Parameters);
        source.SetValue(true);
        strategy.Parameters["P"].Reset();
        strategy.Controls.UpdateValuesFromParameters(strategy.Parameters);

        source.GetCurrentValue().Should().Be(true);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UpdateValuesFromParameters_keeps_a_bound_source_independent_of_declaration_order(bool sourceFirst)
    {
        var strategy = new Strategy_t();
        var panel = new StrategyPanel_t(strategy);
        var source = new CheckBox_t("source") { ParameterRef = "S" };
        var helper = new CheckBox_t("helper") { ParameterRef = "P" };
        helper.StateRules.Add(ControlCollectionGuardTests.SourceEqualsTrue());
        strategy.Parameters.Add(new Parameter_t<Boolean_t>("S") { WireValue = "Y" });
        strategy.Parameters.Add(new Parameter_t<Boolean_t>("P") { WireValue = "Y" });

        if (sourceFirst)
        {
            panel.Controls.Add(source);
            panel.Controls.Add(helper);
        }
        else
        {
            panel.Controls.Add(helper);
            panel.Controls.Add(source);
        }

        strategy.Controls.UpdateValuesFromParameters(strategy.Parameters);

        source.GetCurrentValue().Should().Be(true);
        strategy.Parameters["S"].WireValue.Should().Be("Y");
    }
}
