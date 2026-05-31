using System.Text;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Controls.Support;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Utility;
using FixPortal.FixAtdl.Xml;

namespace FixPortal.FixAtdl.Tests.Validation;

/// <summary>
/// Conformance regressions for the batch-5 edit-evaluation findings (H1, H2, M2, M3),
/// driven against real broker-ATDL patterns.
/// </summary>
public class EditConformanceTests
{
    private static Strategy_t LoadFirst(string xml)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return new StrategiesReader().Load(stream).Strategies[0];
    }

    // ── H1 — EX/NX for list controls ─────────────────────────────────────────

    [Theory]
    [InlineData(Operator_t.NotExist, true)]
    [InlineData(Operator_t.Exist, false)]
    public async Task Unselected_list_control_reports_not_exists(Operator_t op, bool expected)
    {
        var xml = await File.ReadAllTextAsync("Fixtures/pov.xml", TestContext.Current.CancellationToken);
        var pov = LoadFirst(xml);

        // Materialise the (all-false) EnumState — without this GetCurrentValue() is null and the
        // test would pass vacuously through the existing null path rather than exercising the bug.
        var dropdown = pov.Controls["c_Aggression"];
        dropdown.LoadInitValue(FixFieldValueProvider.Empty);

        var edit = new Edit_t<Control_t> { Field = "c_Aggression", Operator = op };
        ((IResolvable<Strategy_t, Control_t>)edit).Resolve(pov, pov.Controls);
        edit.Evaluate();

        edit.CurrentState.Should().Be(expected);
    }

    [Theory]
    [InlineData(Operator_t.Exist, true)]
    [InlineData(Operator_t.NotExist, false)]
    public async Task Selected_list_control_reports_exists(Operator_t op, bool expected)
    {
        var xml = await File.ReadAllTextAsync("Fixtures/pov.xml", TestContext.Current.CancellationToken);
        var pov = LoadFirst(xml);

        var dropdown = pov.Controls["c_Aggression"];
        dropdown.LoadInitValue(FixFieldValueProvider.Empty);

        var selected = new EnumState(["PASSIVE", "NEUTRAL", "AGGRESSIVE"]);
        selected["NEUTRAL"] = true;
        dropdown.SetValue(selected);

        var edit = new Edit_t<Control_t> { Field = "c_Aggression", Operator = op };
        ((IResolvable<Strategy_t, Control_t>)edit).Resolve(pov, pov.Controls);
        edit.Evaluate();

        edit.CurrentState.Should().Be(expected);
    }

    // ── H2 — inequality against a missing FIX field is indeterminate ──────────

    [Theory]
    [InlineData(Operator_t.GreaterThan)]
    [InlineData(Operator_t.GreaterThanOrEqual)]
    [InlineData(Operator_t.LessThan)]
    [InlineData(Operator_t.LessThanOrEqual)]
    public async Task Inequality_against_missing_fix_field_is_false(Operator_t op)
    {
        var xml = await File.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        var twap = LoadFirst(xml);
        twap.Parameters["Participation"].WireValue = "50";

        // Field2 is a FIX_ field that is never supplied → the RHS resolves to null.
        var edit = new Edit_t<IParameter>
        {
            Field = "Participation",
            Operator = op,
            Field2 = "FIX_DoesNotExist",
        };
        ((IResolvable<Strategy_t, IParameter>)edit).Resolve(twap, twap.Parameters);
        edit.Evaluate(FixFieldValueProvider.Empty);

        edit.CurrentState.Should().BeFalse();
    }

    // ── M2 — both 'value' and 'field2' set is rejected at resolve ─────────────

    [Fact]
    public async Task Edit_with_both_value_and_field2_is_rejected_on_resolve()
    {
        var xml = await File.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        var twap = LoadFirst(xml);

        var edit = new Edit_t<IParameter>
        {
            Field = "Participation",
            Operator = Operator_t.Equal,
            Value = "50",
            Field2 = "FIX_OrderQty",
        };

        var act = () => ((IResolvable<Strategy_t, IParameter>)edit).Resolve(twap, twap.Parameters);

        act.Should().Throw<InconsistentStrategyException>()
            .WithMessage("*value*field2*", because: "the M2 guard should name both mutually-exclusive attributes");
    }

    // ── M3 — EQ "false" fires for a default (unset) binary control ────────────

    private const string CheckBoxStrategyXml =
        """
        <?xml version="1.0" encoding="UTF-8"?>
        <Strategies xmlns="http://www.fixprotocol.org/FIXatdl-1-1/Core"
                    xmlns:val="http://www.fixprotocol.org/FIXatdl-1-1/Validation"
                    xmlns:lay="http://www.fixprotocol.org/FIXatdl-1-1/Layout"
                    xmlns:flow="http://www.fixprotocol.org/FIXatdl-1-1/Flow"
                    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                    strategyIdentifierTag="5001">
          <Strategy name="S" version="1" wireValue="S" uiRep="S" providerID="DEMO">
            <Parameter name="P" xsi:type="Int_t" fixTag="9001" use="optional"/>
            <lay:StrategyLayout>
              <lay:StrategyPanel title="P" orientation="VERTICAL" collapsible="false" border="Line">
                <lay:Control ID="EnableStartTime" xsi:type="lay:CheckBox_t" label=""/>
              </lay:StrategyPanel>
            </lay:StrategyLayout>
          </Strategy>
        </Strategies>
        """;

    [Fact]
    public void Eq_false_fires_for_default_unset_checkbox()
    {
        var strategy = LoadFirst(CheckBoxStrategyXml);

        // Deliberately do NOT call LoadDefaults — represents a default/unset checkbox.
        var edit = new Edit_t<Control_t>
        {
            Field = "EnableStartTime",
            Operator = Operator_t.Equal,
            Value = "false",
        };
        ((IResolvable<Strategy_t, Control_t>)edit).Resolve(strategy, strategy.Controls);
        edit.Evaluate();

        edit.CurrentState.Should().BeTrue();
    }

    [Theory]
    [InlineData(Operator_t.Exist, true)]
    [InlineData(Operator_t.NotExist, false)]
    public void Default_unset_checkbox_reports_exists(Operator_t op, bool expected)
    {
        // M3 × H1 interaction, locked deliberately: a default binary control holds concrete false,
        // which is a value that IS sent over FIX, so it reads as present (EX true / NX false) —
        // consistent with the post-LoadDefaults state. Only an explicit Reset() reads as absent.
        var strategy = LoadFirst(CheckBoxStrategyXml);

        var edit = new Edit_t<Control_t> { Field = "EnableStartTime", Operator = op };
        ((IResolvable<Strategy_t, Control_t>)edit).Resolve(strategy, strategy.Controls);
        edit.Evaluate();

        edit.CurrentState.Should().Be(expected);
    }

    [Fact]
    public void Reset_checkbox_reports_not_exists()
    {
        // The construct-vs-Reset asymmetry is deliberate: Reset() sets null ("do not send"), which
        // EX/NX read as absent — unlike the concrete-false construction default above.
        var strategy = LoadFirst(CheckBoxStrategyXml);
        strategy.Controls["EnableStartTime"].Reset();

        var edit = new Edit_t<Control_t> { Field = "EnableStartTime", Operator = Operator_t.NotExist };
        ((IResolvable<Strategy_t, Control_t>)edit).Resolve(strategy, strategy.Controls);
        edit.Evaluate();

        edit.CurrentState.Should().BeTrue();
    }
}
