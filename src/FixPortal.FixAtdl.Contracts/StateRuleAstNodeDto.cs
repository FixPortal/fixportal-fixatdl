// FP Enhancement: 2026-10-07 — boolean wire tokens on compare nodes.
namespace FixPortal.FixAtdl.Contracts;

/// <summary>
/// Wire-safe representation of one node in a StateRule condition tree.
/// Re-used as the internal AST type to avoid a separate model that would diverge over time.
/// </summary>
/// <param name="Kind">
/// Node kind: <c>"compare"</c> | <c>"and"</c> | <c>"or"</c> | <c>"not"</c>.
/// Use <see cref="StateRuleAstKind"/> constants rather than literal strings.
/// </param>
/// <param name="Operator">
/// For <c>"compare"</c> nodes: the comparison verb.
/// Use <see cref="StateRuleOperator"/> constants rather than literal strings.
/// <c>null</c> for logical-combinator nodes (<c>"and"</c>, <c>"or"</c>, <c>"not"</c>).
/// </param>
/// <param name="Field">
/// The control ID or parameter name referenced by a <c>"compare"</c> node; <c>null</c> for
/// logical-combinator nodes.
/// </param>
/// <param name="Value">
/// The right-hand operand for <c>"compare"</c> nodes.  Carried as the raw string from
/// the FIXatdl Edit element; numeric coercion is the evaluator's responsibility.
/// <c>null</c> for existence checks (<c>"exists"</c> / <c>"not-exists"</c>) and for
/// logical-combinator nodes.
/// </param>
/// <param name="Children">
/// Ordered child nodes for logical-combinator nodes; <c>null</c> or empty for leaf
/// <c>"compare"</c> nodes.
/// </param>
/// <param name="Field2">Optional right-hand field reference, used instead of a literal Value.</param>
/// <param name="ComparisonType">Parameter type or Clock_t/EnumState control type; absent for generic scalar state rules.</param>
/// <param name="TrueWireValue">Boolean parameter true wire token, when a compare must apply that mapping.</param>
/// <param name="FalseWireValue">Boolean parameter false wire token, when a compare must apply that mapping.</param>
public sealed record StateRuleAstNodeDto(
    string Kind,
    string? Operator,
    string? Field,
    object? Value,
    IReadOnlyList<StateRuleAstNodeDto>? Children,
    string? Field2 = null,
    string? ComparisonType = null,
    string? TrueWireValue = null,
    string? FalseWireValue = null
);
