using System.Collections.Specialized;
using FixPortal.FixAtdl.Model;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Controls;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Model.Types;
using FixPortal.FixAtdl.Utility;

namespace FixPortal.FixAtdl.Tests.Model.Collections;

/// <summary>
/// Re-entrancy and helper-refresh cases for <see cref="ControlCollection"/>.
/// </summary>
public class ControlCollectionGuardTests
{
    [Fact]
    public void Reentrant_mutation_throws_before_parent_links_are_torn()
    {
        // StrategyPanel already subscribes, so one more handler makes CheckReentrancy throw.
        // A single listener is allowed to re-enter.
        AssertReentrantLeavesPriorState((controls, _) => controls.Clear());

        AssertReentrantLeavesPriorState((controls, intruder) => controls.Insert(0, intruder), intruderStaysOut: true);

        AssertReentrantLeavesPriorState((controls, intruder) => controls[0] = intruder, intruderStaysOut: true);
    }

    [Fact]
    public void UpdateValuesFromParameters_infers_an_unbound_source_from_the_helper()
    {
        var strategy = new Strategy_t();
        var panel = new StrategyPanel_t(strategy);
        var source = new CheckBox_t("source");
        var helper = new CheckBox_t("helper") { ParameterRef = "P" };
        helper.StateRules.Add(SourceEqualsTrue());
        strategy.Parameters.Add(new Parameter_t<Boolean_t>("P") { WireValue = "Y" });
        panel.Controls.Add(source);
        panel.Controls.Add(helper);
        source.SetValue(true);

        strategy.Controls.UpdateValuesFromParameters(strategy.Parameters);

        source.GetCurrentValue().Should().Be(false);
    }

    [Fact]
    public void UpdateValuesFromParameters_runs_the_helper_pass_when_a_control_refresh_throws()
    {
        var strategy = new Strategy_t();
        var panel = new StrategyPanel_t(strategy);
        var spinner = new SingleSpinner_t("spinner") { ParameterRef = "Bad" };
        var source = new CheckBox_t("source");
        var helper = new CheckBox_t("helper") { ParameterRef = "P" };
        helper.StateRules.Add(SourceEqualsTrue());
        strategy.Parameters.Add(new Parameter_t<Boolean_t>("Bad") { WireValue = "Y" });
        strategy.Parameters.Add(new Parameter_t<Boolean_t>("P") { WireValue = "Y" });
        panel.Controls.Add(spinner);
        panel.Controls.Add(source);
        panel.Controls.Add(helper);
        source.SetValue(true);

        var act = () => strategy.Controls.UpdateValuesFromParameters(strategy.Parameters);

        act.Should().Throw<InvalidCastException>().WithMessage("*spinner*");
        source.GetCurrentValue().Should().Be(false);
    }

    private static void AssertReentrantLeavesPriorState(
        Action<ControlCollection, TextField_t> reenter,
        bool intruderStaysOut = false
    )
    {
        var strategy = new Strategy_t();
        var panel = new StrategyPanel_t(strategy);
        var (first, second) = (new TextField_t("c_First"), new TextField_t("c_Second"));
        panel.Controls.Add(first);
        panel.Controls.Add(second);

        var intruder = new TextField_t("c_Intruder");
        var trigger = new TextField_t("c_Trigger");
        Control_t[] membershipBeforeReentrantCall = null!;
        StrategyPanel_t?[] parentsBeforeReentrantCall = null!;
        panel.Controls.CollectionChanged += (_, args) =>
        {
            if (args.Action != NotifyCollectionChangedAction.Add || args.NewItems?[0] is not TextField_t added)
            {
                return;
            }

            if (added.Id != "c_Trigger")
            {
                return;
            }

            membershipBeforeReentrantCall = [.. panel.Controls];
            parentsBeforeReentrantCall = [.. panel.Controls.Select(control => control.OwningStrategyPanel)];
            reenter(panel.Controls, intruder);
        };

        var act = () => panel.Controls.Add(trigger);

        act.Should().Throw<InvalidOperationException>();
        panel.Controls.Should().Equal(membershipBeforeReentrantCall);
        parentsBeforeReentrantCall.Should().Equal(panel.Controls.Select(control => control.OwningStrategyPanel));
        if (intruderStaysOut)
        {
            panel.Controls.Should().NotContain(intruder);
            intruder.OwningStrategyPanel.Should().BeNull();
        }
    }

    internal static StateRule_t SourceEqualsTrue() =>
        new()
        {
            Value = Atdl.NullValue,
            Edit = new Edit_t<Control_t>
            {
                Field = "source",
                Operator = Operator_t.Equal,
                Value = "true",
            },
        };
}
