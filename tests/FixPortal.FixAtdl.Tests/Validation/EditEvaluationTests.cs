using System.Text;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Utility;
using FixPortal.FixAtdl.Xml;

namespace FixPortal.FixAtdl.Tests.Validation;

/// <summary>
/// Characterisation tests for Edit_t comparison and logic operators.
/// Extends EditEvaluatorTests by covering the full operator matrix, the
/// Exist/NotExist family, all four logic operators (And/Or/Not/Xor) and
/// their short-circuit behaviour.
/// </summary>
public class EditEvaluationTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Strategy_t LoadTwap(string xml)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return new StrategiesReader().Load(stream).Strategies[0];
    }

    private static Edit_t<IParameter> MakeEdit(Strategy_t twap, string field, Operator_t op, string? value = null)
    {
        var edit = new Edit_t<IParameter> { Field = field, Operator = op, Value = value! };
        ((IResolvable<Strategy_t, IParameter>)edit).Resolve(twap, twap.Parameters);
        return edit;
    }

    // ── Comparison operators (full matrix) ───────────────────────────────────

    [Theory]
    [InlineData(Operator_t.Equal, "100", "100", true)]
    [InlineData(Operator_t.Equal, "100", "101", false)]
    [InlineData(Operator_t.NotEqual, "100", "100", false)]
    [InlineData(Operator_t.NotEqual, "100", "101", true)]
    [InlineData(Operator_t.GreaterThan, "101", "100", true)]
    [InlineData(Operator_t.GreaterThan, "100", "100", false)]
    [InlineData(Operator_t.GreaterThan, "99", "100", false)]
    [InlineData(Operator_t.LessThan, "99", "100", true)]
    [InlineData(Operator_t.LessThan, "100", "100", false)]
    [InlineData(Operator_t.LessThan, "101", "100", false)]
    [InlineData(Operator_t.GreaterThanOrEqual, "100", "100", true)]
    [InlineData(Operator_t.GreaterThanOrEqual, "101", "100", true)]
    [InlineData(Operator_t.GreaterThanOrEqual, "99", "100", false)]
    [InlineData(Operator_t.LessThanOrEqual, "100", "100", true)]
    [InlineData(Operator_t.LessThanOrEqual, "99", "100", true)]
    [InlineData(Operator_t.LessThanOrEqual, "101", "100", false)]
    public async Task Comparison_operator_evaluates_correctly(
        Operator_t op, string paramValue, string editValue, bool expected)
    {
        var xml = await FixtureFiles.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        var twap = LoadTwap(xml);
        twap.Parameters["Participation"].WireValue = paramValue;

        var edit = MakeEdit(twap, "Participation", op, editValue);
        edit.Evaluate();

        edit.CurrentState.Should().Be(expected);
    }

    // ── Exist / NotExist ─────────────────────────────────────────────────────

    [Fact]
    public async Task Exist_is_true_when_parameter_has_value()
    {
        var xml = await FixtureFiles.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        var twap = LoadTwap(xml);
        twap.Parameters["Participation"].WireValue = "50";

        var edit = MakeEdit(twap, "Participation", Operator_t.Exist);
        edit.Evaluate();

        edit.CurrentState.Should().BeTrue();
    }

    [Fact]
    public async Task Exist_is_false_when_parameter_was_never_set()
    {
        var xml = await FixtureFiles.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        var twap = LoadTwap(xml);
        // Participation is optional; don't assign WireValue — GetCurrentValue returns null.

        var edit = MakeEdit(twap, "Participation", Operator_t.Exist);
        edit.Evaluate();

        edit.CurrentState.Should().BeFalse();
    }

    [Fact]
    public async Task NotExist_is_true_when_parameter_was_never_set()
    {
        var xml = await FixtureFiles.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        var twap = LoadTwap(xml);
        // Participation is optional; don't assign WireValue — GetCurrentValue returns null.

        var edit = MakeEdit(twap, "Participation", Operator_t.NotExist);
        edit.Evaluate();

        edit.CurrentState.Should().BeTrue();
    }

    [Fact]
    public async Task NotExist_is_false_when_parameter_has_value()
    {
        var xml = await FixtureFiles.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        var twap = LoadTwap(xml);
        twap.Parameters["Participation"].WireValue = "50";

        var edit = MakeEdit(twap, "Participation", Operator_t.NotExist);
        edit.Evaluate();

        edit.CurrentState.Should().BeFalse();
    }

    // ── Logic operators ──────────────────────────────────────────────────────
    //
    // Since both child edits share the same "Participation" parameter, the
    // parameter value is set once before Evaluate() and both children compare
    // against their own constant editValue.  This lets us construct AND/OR/NOT/XOR
    // scenarios by varying what constant each child compares against.

    private static Edit_t<IParameter> MakeLogicEdit(
        Strategy_t twap, LogicOperator_t logic, params (Operator_t op, string editVal)[] children)
    {
        var parent = new Edit_t<IParameter> { LogicOperator = logic };

        foreach (var (op, editVal) in children)
        {
            var child = new Edit_t<IParameter> { Field = "Participation", Operator = op, Value = editVal };
            ((IResolvable<Strategy_t, IParameter>)child).Resolve(twap, twap.Parameters);
            parent.Edits.Add(child);
        }

        ((IResolvable<Strategy_t, IParameter>)parent).Resolve(twap, twap.Parameters);
        return parent;
    }

    [Theory]
    // AND: param=100; child1 checks ==100 (true), child2 checks ==100 (true) → true
    [InlineData("100", "100", "100", true)]
    // AND: param=100; child1 checks ==999 (false), child2 checks ==100 (true) → false
    [InlineData("100", "999", "100", false)]
    // AND: param=100; child1 checks ==100 (true), child2 checks ==999 (false) → false
    [InlineData("100", "100", "999", false)]
    public async Task And_logic_operator_evaluates_correctly(
        string paramVal, string editVal1, string editVal2, bool expected)
    {
        var xml = await FixtureFiles.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        var twap = LoadTwap(xml);
        twap.Parameters["Participation"].WireValue = paramVal;

        var parent = MakeLogicEdit(twap, LogicOperator_t.And,
            (Operator_t.Equal, editVal1),
            (Operator_t.Equal, editVal2));

        parent.Evaluate(FixFieldValueProvider.Empty);

        parent.CurrentState.Should().Be(expected);
    }

    [Theory]
    // OR: param=100; child1 checks ==999 (false), child2 checks ==888 (false) → false
    [InlineData("100", "999", "888", false)]
    // OR: param=100; child1 checks ==100 (true), child2 checks ==999 (false) → true
    [InlineData("100", "100", "999", true)]
    // OR: param=100; child1 checks ==999 (false), child2 checks ==100 (true) → true
    [InlineData("100", "999", "100", true)]
    public async Task Or_logic_operator_evaluates_correctly(
        string paramVal, string editVal1, string editVal2, bool expected)
    {
        var xml = await FixtureFiles.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        var twap = LoadTwap(xml);
        twap.Parameters["Participation"].WireValue = paramVal;

        var parent = MakeLogicEdit(twap, LogicOperator_t.Or,
            (Operator_t.Equal, editVal1),
            (Operator_t.Equal, editVal2));

        parent.Evaluate(FixFieldValueProvider.Empty);

        parent.CurrentState.Should().Be(expected);
    }

    [Theory]
    // NOT: param=100; child checks ==100 (true) → NOT true = false
    [InlineData("100", "100", false)]
    // NOT: param=100; child checks ==999 (false) → NOT false = true
    [InlineData("100", "999", true)]
    public async Task Not_logic_operator_evaluates_correctly(
        string paramVal, string editVal, bool expected)
    {
        var xml = await FixtureFiles.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        var twap = LoadTwap(xml);
        twap.Parameters["Participation"].WireValue = paramVal;

        var parent = MakeLogicEdit(twap, LogicOperator_t.Not, (Operator_t.Equal, editVal));

        parent.Evaluate(FixFieldValueProvider.Empty);

        parent.CurrentState.Should().Be(expected);
    }

    [Theory]
    // XOR: param=100; child1 ==100 (T), child2 ==999 (F) → exactly 1 true → true
    [InlineData("100", "100", "999", true)]
    // XOR: param=100; child1 ==100 (T), child2 ==100 (T) → both true → false
    [InlineData("100", "100", "100", false)]
    // XOR: param=100; child1 ==999 (F), child2 ==888 (F) → neither true → false
    [InlineData("100", "999", "888", false)]
    public async Task Xor_logic_operator_evaluates_correctly(
        string paramVal, string editVal1, string editVal2, bool expected)
    {
        var xml = await FixtureFiles.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        var twap = LoadTwap(xml);
        twap.Parameters["Participation"].WireValue = paramVal;

        var parent = MakeLogicEdit(twap, LogicOperator_t.Xor,
            (Operator_t.Equal, editVal1),
            (Operator_t.Equal, editVal2));

        parent.Evaluate(FixFieldValueProvider.Empty);

        parent.CurrentState.Should().Be(expected);
    }

    // ── StrategyEdit wrapper ──────────────────────────────────────────────────

    [Fact]
    public async Task StrategyEdit_CurrentState_follows_wrapped_edit()
    {
        var xml = await FixtureFiles.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        var twap = LoadTwap(xml);
        twap.Parameters["Participation"].WireValue = "50";

        var edit = MakeEdit(twap, "Participation", Operator_t.Equal, "50");
        var strategyEdit = new StrategyEdit_t
        {
            Edit = edit,
            ErrorMessage = "Participation must be 50"
        };
        ((IResolvable<Strategy_t, IParameter>)strategyEdit).Resolve(twap, twap.Parameters);

        strategyEdit.Evaluate();

        strategyEdit.CurrentState.Should().BeTrue();
    }

    [Fact]
    public async Task StrategyEdit_CurrentState_is_false_when_edit_fails()
    {
        var xml = await FixtureFiles.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        var twap = LoadTwap(xml);
        twap.Parameters["Participation"].WireValue = "30";

        var edit = MakeEdit(twap, "Participation", Operator_t.Equal, "50");
        var strategyEdit = new StrategyEdit_t
        {
            Edit = edit,
            ErrorMessage = "Participation must be 50"
        };
        ((IResolvable<Strategy_t, IParameter>)strategyEdit).Resolve(twap, twap.Parameters);

        strategyEdit.Evaluate();

        strategyEdit.CurrentState.Should().BeFalse();
    }

    // ── Sources property ─────────────────────────────────────────────────────

    [Fact]
    public async Task Sources_contains_field_name_after_resolve()
    {
        var xml = await FixtureFiles.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        var twap = LoadTwap(xml);
        twap.Parameters["Participation"].WireValue = "50";

        var edit = MakeEdit(twap, "Participation", Operator_t.Equal, "50");
        var strategyEdit = new StrategyEdit_t { Edit = edit, ErrorMessage = "err" };
        ((IResolvable<Strategy_t, IParameter>)strategyEdit).Resolve(twap, twap.Parameters);

        strategyEdit.Sources.Should().Contain("Participation");
    }

    [Fact]
    public async Task StrategyEditCollection_EvaluateAll_returns_false_when_edit_fails()
    {
        var xml = await FixtureFiles.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        var twap = LoadTwap(xml);
        twap.Parameters["Participation"].WireValue = "30";

        var edit = MakeEdit(twap, "Participation", Operator_t.Equal, "50");
        var strategyEdit = new StrategyEdit_t { Edit = edit, ErrorMessage = "Participation must be 50" };
        ((IResolvable<Strategy_t, IParameter>)strategyEdit).Resolve(twap, twap.Parameters);

        var col = new StrategyEditCollection { strategyEdit };
        col.EvaluateAll(FixFieldValueProvider.Empty, shortCircuit: false).Should().BeFalse();
        col.EvaluateAll(FixFieldValueProvider.Empty, shortCircuit: true).Should().BeFalse();
    }
}
