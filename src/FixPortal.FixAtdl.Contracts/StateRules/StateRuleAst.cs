using FixPortal.FixAtdl.Contracts;

namespace FixPortal.FixAtdl.Contracts.StateRules;

/// <summary>
/// Factory helpers for building <see cref="StateRuleAstNodeDto"/> nodes tersely in tests
/// and in the builder.  Using the shared DTO directly keeps the internal AST and the wire
/// representation in lockstep.
/// </summary>
public static class StateRuleAst
{
    /// <summary>Builds a compare node.</summary>
    /// <param name="field">The field the comparison reads.</param>
    /// <param name="op">A <see cref="StateRuleOperator"/> value.</param>
    /// <param name="value">The literal operand, when the comparison is not against another field.</param>
    /// <param name="field2">The other field, when the operand is a field rather than a literal.</param>
    /// <returns>The compare node.</returns>
    public static StateRuleAstNodeDto Compare(string field, string op, object? value = null, string? field2 = null) =>
        new(StateRuleAstKind.Compare, op, field, value, null, field2);

    /// <summary>Builds an AND node over the given children.</summary>
    /// <param name="children">Child nodes, in document order.</param>
    /// <returns>The AND node.</returns>
    public static StateRuleAstNodeDto And(IReadOnlyList<StateRuleAstNodeDto> children) =>
        new(StateRuleAstKind.And, null, null, null, children);

    /// <summary>Builds an OR node over the given children.</summary>
    /// <param name="children">Child nodes, in document order.</param>
    /// <returns>The OR node.</returns>
    public static StateRuleAstNodeDto Or(IReadOnlyList<StateRuleAstNodeDto> children) =>
        new(StateRuleAstKind.Or, null, null, null, children);

    /// <summary>Builds a NOT node over one child.</summary>
    /// <param name="child">The negated node.</param>
    /// <returns>The NOT node.</returns>
    public static StateRuleAstNodeDto Not(StateRuleAstNodeDto child) =>
        new(StateRuleAstKind.Not, null, null, null, [child]);
}
