using AwesomeAssertions;
using FixPortal.FixAtdl.Contracts;
using FixPortal.FixAtdl.Contracts.StateRules;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Enumerations;
using Xunit;

namespace FixPortal.FixAtdl.Contracts.Tests;

/// <summary>
/// Tests for <see cref="StateRuleAstBuilder"/>.
/// Edit_t / EditCollection instances are constructed directly so these tests are
/// focused on the builder's AST mapping logic, not on the XML parser.
/// </summary>
public class StateRuleAstBuilderTests
{
    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static StateRuleAstBuilder Builder(EditCollection? globals = null, EditCollection? strategyEdits = null) =>
        new(globals, strategyEdits);

    /// <summary>
    /// Builds a leaf Edit_t with the given field, operator and value.
    /// </summary>
    private static Edit_t CompareEdit(string field, Operator_t op, string? value = null) =>
        new()
        {
            Field = field,
            Operator = op,
            Value = value!,
        };

    /// <summary>
    /// Builds a compound Edit_t with the given LogicOperator and child Edits.
    /// Children that have null IDs are fine for inline edits inside a compound node.
    /// </summary>
    private static Edit_t CompoundEdit(LogicOperator_t logicOp, params Edit_t[] children)
    {
        var edit = new Edit_t { LogicOperator = logicOp };
        foreach (var child in children)
        {
            edit.Edits.Add(child);
        }

        return edit;
    }

    // ---------------------------------------------------------------------------
    // Single compare nodes
    // ---------------------------------------------------------------------------

    [Fact]
    public void Preserves_field2_for_unbound_and_bound_edits()
    {
        var edit = new Edit_t
        {
            Field = "start",
            Field2 = "end",
            Operator = Operator_t.LessThan,
        };
        var bound = new Edit_t<Control_t>
        {
            Field = "start",
            Field2 = "end",
            Operator = Operator_t.LessThan,
        };

        Builder().Build(edit).Field2.Should().Be("end");
        Builder().Build(bound).Field2.Should().Be("end");
    }

    [Fact]
    public void Single_Compare_NotEqual_maps_to_neq_node()
    {
        var edit = CompareEdit("c_Side", Operator_t.NotEqual, "1");

        var node = Builder().Build(edit);

        node.Kind.Should().Be(StateRuleAstKind.Compare);
        node.Operator.Should().Be(StateRuleOperator.Neq);
        node.Field.Should().Be("c_Side");
        node.Value.Should().Be("1");
        // Factory explicitly sets Children = null for compare nodes; assert the precise contract.
        node.Children.Should().BeNull();
    }

    [Theory]
    [InlineData(Operator_t.GreaterThan)]
    [InlineData(Operator_t.LessThan)]
    [InlineData(Operator_t.GreaterThanOrEqual)]
    [InlineData(Operator_t.LessThanOrEqual)]
    public void Ordering_comparisons_require_a_value_or_field2(Operator_t op)
    {
        var act = () => Builder().Build(CompareEdit("field", op));

        act.Should().Throw<AtdlParseException>().Which.Code.Should().Be(AtdlParseExceptionCode.InvalidEditValue);
    }

    [Fact]
    public void Not_wraps_a_single_child()
    {
        var inner = CompareEdit("c_Qty", Operator_t.GreaterThan, "0");
        var edit = CompoundEdit(LogicOperator_t.Not, inner);

        var node = Builder().Build(edit);

        node.Kind.Should().Be(StateRuleAstKind.Not);
        node.Operator.Should().BeNull();
        node.Children.Should().HaveCount(1);
        node.Children[0].Kind.Should().Be(StateRuleAstKind.Compare);
        node.Children[0].Operator.Should().Be(StateRuleOperator.Gt);
    }

    [Fact]
    public void Nested_And_Or_preserves_structure()
    {
        // AND( OR(EQ, EQ), GT )
        var eq1 = CompareEdit("c_A", Operator_t.Equal, "X");
        var eq2 = CompareEdit("c_B", Operator_t.Equal, "Y");
        var orNode = CompoundEdit(LogicOperator_t.Or, eq1, eq2);
        var gt = CompareEdit("c_C", Operator_t.GreaterThan, "5");
        var andRoot = CompoundEdit(LogicOperator_t.And, orNode, gt);

        var node = Builder().Build(andRoot);

        node.Kind.Should().Be(StateRuleAstKind.And);
        node.Children.Should().HaveCount(2);

        var orChild = node.Children[0];
        orChild.Kind.Should().Be(StateRuleAstKind.Or);
        orChild.Children.Should().HaveCount(2);
        orChild.Children[0].Operator.Should().Be(StateRuleOperator.Eq);
        orChild.Children[1].Operator.Should().Be(StateRuleOperator.Eq);

        var gtChild = node.Children[1];
        gtChild.Kind.Should().Be(StateRuleAstKind.Compare);
        gtChild.Operator.Should().Be(StateRuleOperator.Gt);
        gtChild.Field.Should().Be("c_C");
        gtChild.Value.Should().Be("5");
    }

    // ---------------------------------------------------------------------------
    // Operator mapping coverage
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData(Operator_t.Equal, StateRuleOperator.Eq, "v", "v")]
    [InlineData(Operator_t.NotEqual, StateRuleOperator.Neq, "v", "v")]
    [InlineData(Operator_t.GreaterThan, StateRuleOperator.Gt, "5", "5")]
    [InlineData(Operator_t.LessThan, StateRuleOperator.Lt, "5", "5")]
    [InlineData(Operator_t.GreaterThanOrEqual, StateRuleOperator.Ge, "5", "5")]
    [InlineData(Operator_t.LessThanOrEqual, StateRuleOperator.Le, "5", "5")]
    // EX (Exist): no Value on the compare node — existence checks carry no literal value.
    [InlineData(Operator_t.Exist, StateRuleOperator.Exists, null, null)]
    // NX (NotExist): analogous to EX but negated.
    [InlineData(Operator_t.NotExist, StateRuleOperator.NotExists, null, null)]
    public void Operator_map_covers_all_Operator_t_values(
        Operator_t input,
        string expectedOp,
        string? inputValue,
        string? expectedValue
    )
    {
        // NX in original FIXatdl spec → Operator_t.NotEqual in this implementation.
        // EX in original FIXatdl spec → Operator_t.Exist.
        // The FixPortal.FixAtdl library uses Exist/NotExist rather than NX/EX strings.
        var edit = CompareEdit("c_Field", input, inputValue);

        var node = Builder().Build(edit);

        node.Operator.Should().Be(expectedOp, because: $"Operator_t.{input} should map to '{expectedOp}'");
        node.Value.Should().Be(expectedValue, because: "existence checks must not carry a value");
    }

    // ---------------------------------------------------------------------------
    // EditRef resolution
    // ---------------------------------------------------------------------------

    [Fact]
    public void EditRef_resolves_from_global_registry()
    {
        var globalEdits = new EditCollection
        {
            new Edit_t
            {
                Id = "GlobalEQ",
                Field = "c_X",
                Operator = Operator_t.Equal,
                Value = "Z",
            },
        };

        var node = Builder(globalEdits).BuildFromRef("GlobalEQ");

        node.Kind.Should().Be(StateRuleAstKind.Compare);
        node.Operator.Should().Be(StateRuleOperator.Eq);
        node.Field.Should().Be("c_X");
        node.Value.Should().Be("Z");
    }

    [Fact]
    public void EditRef_strategy_level_takes_precedence_over_global()
    {
        var globalEdits = new EditCollection();
        var strategyEdits = new EditCollection();

        // Both registries contain the same ID; the strategy-level one should win.
        globalEdits.Add(
            new Edit_t
            {
                Id = "Shared",
                Field = "c_Global",
                Operator = Operator_t.Equal,
                Value = "G",
            }
        );
        strategyEdits.Add(
            new Edit_t
            {
                Id = "Shared",
                Field = "c_Strategy",
                Operator = Operator_t.Equal,
                Value = "S",
            }
        );

        var node = Builder(globalEdits, strategyEdits).BuildFromRef("Shared");

        node.Field.Should().Be("c_Strategy", because: "strategy-level edits shadow global edits with the same ID");
    }

    [Fact]
    public void Unresolved_EditRef_throws_AtdlParseException_with_correct_code()
    {
        var act = () => Builder().BuildFromRef("NoSuchEdit");

        act.Should().Throw<AtdlParseException>().Which.Code.Should().Be(AtdlParseExceptionCode.UnresolvedEditRef);
    }

    // ---------------------------------------------------------------------------
    // XOR logic operator
    // ---------------------------------------------------------------------------

    [Fact]
    public void Xor_logic_operator_produces_xor_kind_node()
    {
        // XOR is defined in the FIXatdl spec but very rare; the builder must emit a
        // distinct "xor" kind so evaluators have an explicit contract to honour rather
        // than silently receiving a wrong combinator.
        var left = CompareEdit("c_Side", Operator_t.Equal, "1");
        var right = CompareEdit("c_Side", Operator_t.Equal, "2");
        var edit = CompoundEdit(LogicOperator_t.Xor, left, right);

        var node = Builder().Build(edit);

        node.Kind.Should().Be(StateRuleAstKind.Xor);
        node.Operator.Should().BeNull();
        node.Children.Should().HaveCount(2);
        node.Children[0].Kind.Should().Be(StateRuleAstKind.Compare);
        node.Children[0].Operator.Should().Be(StateRuleOperator.Eq);
        node.Children[0].Field.Should().Be("c_Side");
        node.Children[0].Value.Should().Be("1");
        node.Children[1].Kind.Should().Be(StateRuleAstKind.Compare);
        node.Children[1].Operator.Should().Be(StateRuleOperator.Eq);
        node.Children[1].Field.Should().Be("c_Side");
        node.Children[1].Value.Should().Be("2");
    }

    // ---------------------------------------------------------------------------
    // Unknown / out-of-range operator
    // ---------------------------------------------------------------------------

    [Fact]
    public void Unknown_operator_throws_AtdlParseException()
    {
        // Cast an out-of-range int to Operator_t to simulate a future extension
        // or a corrupt enum value; the builder must not silently ignore it.
        var edit = new Edit_t { Field = "c_X", Operator = (Operator_t)999 };

        var act = () => Builder().Build(edit);

        act.Should()
            .Throw<AtdlParseException>()
            .Which.Code.Should()
            .Be(AtdlParseExceptionCode.UnknownStateRuleOperator);
    }

    [Fact]
    public void Edit_with_neither_operator_nor_logic_operator_throws()
    {
        // A malformed Edit_t with no Operator and no LogicOperator must be rejected.
        var edit = new Edit_t { Field = "c_X" };

        var act = () => Builder().Build(edit);

        act.Should()
            .Throw<AtdlParseException>()
            .Which.Code.Should()
            .Be(AtdlParseExceptionCode.UnknownStateRuleOperator);
    }

    // ---------------------------------------------------------------------------
    // M-02 — NOT with wrong operand count must throw, not IndexOutOfRange
    // ---------------------------------------------------------------------------

    [Fact]
    public void NOT_with_zero_operands_throws_AtdlParseException_not_IndexOutOfRange()
    {
        // <Edit logicOperator="NOT"/> with no children → children[0] throws ArgumentOutOfRangeException today.
        // After fix, it must throw AtdlParseException(UnknownStateRuleOperator) → maps to 422.
        var edit = new Edit_t { LogicOperator = LogicOperator_t.Not };

        var act = () => Builder().Build(edit);

        act.Should()
            .Throw<AtdlParseException>()
            .Which.Code.Should()
            .Be(AtdlParseExceptionCode.UnknownStateRuleOperator);
    }

    [Fact]
    public void NOT_with_two_operands_throws_AtdlParseException()
    {
        // FIXatdl NOT must have exactly one child operand.
        var child1 = CompareEdit("c_A", Operator_t.Equal, "1");
        var child2 = CompareEdit("c_B", Operator_t.Equal, "2");
        var edit = CompoundEdit(LogicOperator_t.Not, child1, child2);

        var act = () => Builder().Build(edit);

        act.Should()
            .Throw<AtdlParseException>()
            .Which.Code.Should()
            .Be(AtdlParseExceptionCode.UnknownStateRuleOperator);
    }

    // ---------------------------------------------------------------------------
    // Operand-less EQ/NEQ must be rejected at build time
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData(Operator_t.Equal)]
    [InlineData(Operator_t.NotEqual)]
    public void Compare_with_neither_value_nor_field2_throws_at_build_time(Operator_t op)
    {
        // An operand-less EQ/NEQ would evaluate as `== {NULL}`. The null-literal behaviour is
        // deliberate when the author WROTE value="{NULL}" (pinned in state-rule-cases.json),
        // but a compare that carries no operand at all is a malformed document acquiring a
        // meaning its author never wrote — reject it when building.
        var edit = new Edit_t { Field = "c_X", Operator = op };

        var act = () => Builder().Build(edit);

        act.Should().Throw<AtdlParseException>().Which.Code.Should().Be(AtdlParseExceptionCode.InvalidEditValue);
    }

    [Theory]
    [InlineData(Operator_t.Equal)]
    [InlineData(Operator_t.NotEqual)]
    public void Bound_compare_with_neither_value_nor_field2_throws_at_build_time(Operator_t op)
    {
        var edit = new Edit_t<Control_t> { Field = "c_X", Operator = op };

        var act = () => Builder().Build(edit);

        act.Should().Throw<AtdlParseException>().Which.Code.Should().Be(AtdlParseExceptionCode.InvalidEditValue);
    }

    // ---------------------------------------------------------------------------
    // Unbounded recursion guard (deeply-nested Edit tree)
    // ---------------------------------------------------------------------------

    [Fact]
    public void Deeply_nested_AND_chain_throws_MaxDepthExceeded_instead_of_overflowing_stack()
    {
        // Build a 200-deep chain of single-child AND nodes — well past the 64-level guard.
        Edit_t current = CompareEdit("c_Leaf", Operator_t.Equal, "1");
        for (var i = 0; i < 200; i++)
        {
            current = CompoundEdit(LogicOperator_t.And, current);
        }

        var act = () => Builder().Build(current);

        act.Should().Throw<AtdlParseException>().Which.Code.Should().Be(AtdlParseExceptionCode.MaxDepthExceeded);
    }

    [Fact]
    public void Value_and_field2_together_are_rejected()
    {
        var edit = new Edit_t
        {
            Field = "c_X",
            Operator = Operator_t.Equal,
            Value = "1",
            Field2 = "c_Y",
        };

        var act = () => Builder().Build(edit);

        act.Should().Throw<AtdlParseException>().Which.Code.Should().Be(AtdlParseExceptionCode.InvalidEditValue);
    }

    [Fact]
    public void Bound_value_and_field2_together_are_rejected()
    {
        var edit = new Edit_t<Control_t>
        {
            Field = "c_X",
            Operator = Operator_t.Equal,
            Value = "1",
            Field2 = "c_Y",
        };

        var act = () => Builder().Build(edit);

        act.Should().Throw<AtdlParseException>().Which.Code.Should().Be(AtdlParseExceptionCode.InvalidEditValue);
    }

    [Fact]
    public void Comparison_operator_with_logic_operator_is_rejected()
    {
        var edit = new Edit_t
        {
            Field = "c_X",
            Operator = Operator_t.Equal,
            Value = "1",
            LogicOperator = LogicOperator_t.Not,
        };
        edit.Edits.Add(CompareEdit("c_Y", Operator_t.Equal, "2"));

        var act = () => Builder().Build(edit);

        act.Should().Throw<AtdlParseException>().Which.Code.Should().Be(AtdlParseExceptionCode.InvalidEditValue);
    }

    [Fact]
    public void Comparison_operator_with_child_edits_is_rejected()
    {
        var edit = CompareEdit("c_X", Operator_t.Equal, "1");
        edit.Edits.Add(CompareEdit("c_Y", Operator_t.Equal, "2"));

        var act = () => Builder().Build(edit);

        act.Should().Throw<AtdlParseException>().Which.Code.Should().Be(AtdlParseExceptionCode.InvalidEditValue);
    }

    [Theory]
    [InlineData(LogicOperator_t.And)]
    [InlineData(LogicOperator_t.Or)]
    [InlineData(LogicOperator_t.Xor)]
    public void Empty_logic_children_are_rejected(LogicOperator_t logic)
    {
        var act = () => Builder().Build(new Edit_t { LogicOperator = logic });

        act.Should().Throw<AtdlParseException>().Which.Code.Should().Be(AtdlParseExceptionCode.InvalidEditValue);
    }
}
