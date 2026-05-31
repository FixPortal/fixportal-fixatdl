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
}
