using System.Text;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Utility;
using FixPortal.FixAtdl.Xml;

namespace FixPortal.FixAtdl.Tests.Model.Collections;

/// <summary>
/// Characterisation tests for EditEvaluatingCollection&lt;T&gt;: And/Or/Not/Xor logic
/// operators and the null-operator guard.
/// </summary>
public class EditEvaluatingCollectionTests
{
    // -----------------------------------------------------------------------
    // Fixture helpers
    // -----------------------------------------------------------------------

    private static Strategy_t LoadTwap()
    {
        // We need the real fixture on disk so we spin up the fixture reader.
        // ReadAllText is synchronous-friendly from a [Fact]; the async variant is
        // used in integration tests where the TestContext is available.
        string xml = FixtureFiles.ReadAllText("Fixtures/twap.xml");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return new StrategiesReader().Load(stream).Strategies[0];
    }

    [Fact]
    public void LoadTwap_is_not_cwd_dependent()
    {
        // Assert that the file is loaded from AppContext.BaseDirectory, avoiding process-global CWD mutation.
        var path = Path.Join(AppContext.BaseDirectory, "Fixtures", "twap.xml");
        File.Exists(path).Should().BeTrue();

        var act = () => LoadTwap();
        act.Should().NotThrow();
    }

    /// <summary>
    /// Creates a resolved Edit_t&lt;IParameter&gt; against the TWAP fixture.
    /// </summary>
    private static Edit_t<IParameter> MakeEdit(
        Strategy_t twap,
        string field,
        Operator_t op,
        string value)
    {
        var edit = new Edit_t<IParameter>
        {
            Field = field,
            Operator = op,
            Value = value,
        };

        ((IResolvable<Strategy_t, IParameter>)edit).Resolve(twap, twap.Parameters);

        return edit;
    }

    // -----------------------------------------------------------------------
    // Null LogicOperator guard
    // -----------------------------------------------------------------------

    [Fact]
    public void Evaluate_throws_when_LogicOperator_is_null()
    {
        var twap = LoadTwap();
        twap.Parameters["Participation"].WireValue = "50";

        // NOTE: No LogicOperator set — Evaluate must throw InvalidOperationException.
        var collection = new EditEvaluatingCollection<IParameter>
        {
            MakeEdit(twap, "Participation", Operator_t.GreaterThan, "0"),
        };

        var act = () => collection.Evaluate(FixFieldValueProvider.Empty);
        act.Should().Throw<InvalidOperationException>();
    }

    // -----------------------------------------------------------------------
    // And
    // -----------------------------------------------------------------------

    [Fact]
    public void And_both_true_yields_true()
    {
        var twap = LoadTwap();
        // Participation = 50 → (> 0) = true AND (< 100) = true → AND = true
        twap.Parameters["Participation"].WireValue = "50";

        var collection = new EditEvaluatingCollection<IParameter>
        {
            LogicOperator = LogicOperator_t.And
        };
        collection.Add(MakeEdit(twap, "Participation", Operator_t.GreaterThan, "0"));
        collection.Add(MakeEdit(twap, "Participation", Operator_t.LessThan, "100"));

        collection.Evaluate(FixFieldValueProvider.Empty);

        collection.CurrentState.Should().BeTrue();
    }

    [Fact]
    public void And_one_false_yields_false()
    {
        var twap = LoadTwap();
        // Participation = 150 → (> 0) = true AND (< 100) = false → AND = false
        twap.Parameters["Participation"].WireValue = "150";

        var collection = new EditEvaluatingCollection<IParameter>
        {
            LogicOperator = LogicOperator_t.And
        };
        collection.Add(MakeEdit(twap, "Participation", Operator_t.GreaterThan, "0"));
        collection.Add(MakeEdit(twap, "Participation", Operator_t.LessThan, "100"));

        collection.Evaluate(FixFieldValueProvider.Empty);

        collection.CurrentState.Should().BeFalse();
    }

    [Fact]
    public void And_short_circuits_on_first_false()
    {
        var twap = LoadTwap();
        // Participation = -1 → (> 0) = false; short-circuit should skip second edit
        twap.Parameters["Participation"].WireValue = "-1";

        var collection = new EditEvaluatingCollection<IParameter>
        {
            LogicOperator = LogicOperator_t.And
        };
        collection.Add(MakeEdit(twap, "Participation", Operator_t.GreaterThan, "0"));

        // Sentinel edit with neither Operator nor LogicOperator set, which throws on Evaluate
        var sentinel = new Edit_t<IParameter>();
        collection.Add(sentinel);

        var act = () => collection.Evaluate(FixFieldValueProvider.Empty);
        act.Should().NotThrow();
        collection.CurrentState.Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // Or
    // -----------------------------------------------------------------------

    [Fact]
    public void Or_one_true_operand_yields_true()
    {
        var twap = LoadTwap();
        // Participation = 50 → (> 0) = true OR (> 200) = false → OR = true.
        twap.Parameters["Participation"].WireValue = "50";

        var collection = new EditEvaluatingCollection<IParameter>
        {
            LogicOperator = LogicOperator_t.Or
        };
        collection.Add(MakeEdit(twap, "Participation", Operator_t.GreaterThan, "0"));
        collection.Add(MakeEdit(twap, "Participation", Operator_t.GreaterThan, "200"));

        collection.Evaluate(FixFieldValueProvider.Empty);

        collection.CurrentState.Should().BeTrue();
    }

    [Fact]
    public void Or_short_circuits_on_first_true()
    {
        var twap = LoadTwap();
        // Participation = 50 → (> 0) = true; short-circuit should skip second edit
        twap.Parameters["Participation"].WireValue = "50";

        var collection = new EditEvaluatingCollection<IParameter>
        {
            LogicOperator = LogicOperator_t.Or
        };
        collection.Add(MakeEdit(twap, "Participation", Operator_t.GreaterThan, "0"));

        // Sentinel edit with neither Operator nor LogicOperator set, which throws on Evaluate
        var sentinel = new Edit_t<IParameter>();
        collection.Add(sentinel);

        var act = () => collection.Evaluate(FixFieldValueProvider.Empty);
        act.Should().NotThrow();
        collection.CurrentState.Should().BeTrue();
    }

    [Fact]
    public void Or_both_false_yields_false()
    {
        var twap = LoadTwap();
        // Participation = -5 → (> 0) = false OR (> 200) = false → OR = false
        twap.Parameters["Participation"].WireValue = "-5";

        var collection = new EditEvaluatingCollection<IParameter>
        {
            LogicOperator = LogicOperator_t.Or
        };
        collection.Add(MakeEdit(twap, "Participation", Operator_t.GreaterThan, "0"));
        collection.Add(MakeEdit(twap, "Participation", Operator_t.GreaterThan, "200"));

        collection.Evaluate(FixFieldValueProvider.Empty);

        collection.CurrentState.Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // Not
    // -----------------------------------------------------------------------

    [Fact]
    public void Not_single_true_operand_yields_false()
    {
        var twap = LoadTwap();
        // Participation = 50 → (> 0) = true; NOT → false
        twap.Parameters["Participation"].WireValue = "50";

        var collection = new EditEvaluatingCollection<IParameter>
        {
            LogicOperator = LogicOperator_t.Not
        };
        collection.Add(MakeEdit(twap, "Participation", Operator_t.GreaterThan, "0"));

        collection.Evaluate(FixFieldValueProvider.Empty);

        collection.CurrentState.Should().BeFalse();
    }

    [Fact]
    public void Not_single_false_operand_yields_true()
    {
        var twap = LoadTwap();
        // Participation = -1 → (> 0) = false; NOT → true
        twap.Parameters["Participation"].WireValue = "-1";

        var collection = new EditEvaluatingCollection<IParameter>
        {
            LogicOperator = LogicOperator_t.Not
        };
        collection.Add(MakeEdit(twap, "Participation", Operator_t.GreaterThan, "0"));

        collection.Evaluate(FixFieldValueProvider.Empty);

        collection.CurrentState.Should().BeTrue();
    }

    // -----------------------------------------------------------------------
    // Xor
    // -----------------------------------------------------------------------

    [Fact]
    public void Xor_exactly_one_true_yields_true()
    {
        var twap = LoadTwap();
        // Participation = 50 → (> 0) = true, (> 100) = false → XOR = true (one true)
        twap.Parameters["Participation"].WireValue = "50";

        var collection = new EditEvaluatingCollection<IParameter>
        {
            LogicOperator = LogicOperator_t.Xor
        };
        collection.Add(MakeEdit(twap, "Participation", Operator_t.GreaterThan, "0"));
        collection.Add(MakeEdit(twap, "Participation", Operator_t.GreaterThan, "100"));

        collection.Evaluate(FixFieldValueProvider.Empty);

        collection.CurrentState.Should().BeTrue();
    }

    [Fact]
    public void Xor_both_true_yields_false()
    {
        var twap = LoadTwap();
        // Participation = 150 → (> 0) = true, (> 100) = true → XOR = false (two true)
        twap.Parameters["Participation"].WireValue = "150";

        var collection = new EditEvaluatingCollection<IParameter>
        {
            LogicOperator = LogicOperator_t.Xor
        };
        collection.Add(MakeEdit(twap, "Participation", Operator_t.GreaterThan, "0"));
        collection.Add(MakeEdit(twap, "Participation", Operator_t.GreaterThan, "100"));

        collection.Evaluate(FixFieldValueProvider.Empty);

        collection.CurrentState.Should().BeFalse();
    }

    [Fact]
    public void Xor_both_false_yields_false()
    {
        var twap = LoadTwap();
        // Participation = -5 → (> 0) = false, (> 100) = false → XOR = false (none true)
        twap.Parameters["Participation"].WireValue = "-5";

        var collection = new EditEvaluatingCollection<IParameter>
        {
            LogicOperator = LogicOperator_t.Xor
        };
        collection.Add(MakeEdit(twap, "Participation", Operator_t.GreaterThan, "0"));
        collection.Add(MakeEdit(twap, "Participation", Operator_t.GreaterThan, "100"));

        collection.Evaluate(FixFieldValueProvider.Empty);

        collection.CurrentState.Should().BeFalse();
    }

    [Fact]
    public void Xor_three_operands_all_true_yields_false()
    {
        var twap = LoadTwap();
        // Participation = 150 → (> 0), (> 50), (> 100) are all true → XOR = false (three true)
        twap.Parameters["Participation"].WireValue = "150";

        var collection = new EditEvaluatingCollection<IParameter>
        {
            LogicOperator = LogicOperator_t.Xor
        };
        collection.Add(MakeEdit(twap, "Participation", Operator_t.GreaterThan, "0"));
        collection.Add(MakeEdit(twap, "Participation", Operator_t.GreaterThan, "50"));
        collection.Add(MakeEdit(twap, "Participation", Operator_t.GreaterThan, "100"));

        collection.Evaluate(FixFieldValueProvider.Empty);

        collection.CurrentState.Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // Sources aggregation
    // -----------------------------------------------------------------------

    [Fact]
    public void Sources_aggregates_field_names_from_added_edits()
    {
        var twap = LoadTwap();
        twap.Parameters["Participation"].WireValue = "50";

        var collection = new EditEvaluatingCollection<IParameter>
        {
            LogicOperator = LogicOperator_t.And
        };

        var edit = MakeEdit(twap, "Participation", Operator_t.GreaterThan, "0");
        collection.Add(edit);

        // Sources should contain "Participation" after the edit is inserted.
        collection.Sources.Should().Contain("Participation");
    }
}
