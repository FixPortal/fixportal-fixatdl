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
}
