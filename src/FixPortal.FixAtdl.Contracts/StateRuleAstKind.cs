namespace FixPortal.FixAtdl.Contracts;

/// <summary>
/// String constants for the <see cref="StateRuleAstNodeDto.Kind"/> discriminator.
/// Shared with the @fix-portal/fixatdl-react TypeScript evaluator; keep the values identical.
/// </summary>
public static class StateRuleAstKind
{
    /// <summary>Compare a field to a value or another field.</summary>
    public const string Compare = "compare";

    /// <summary>Logical conjunction.</summary>
    public const string And = "and";

    /// <summary>Logical disjunction.</summary>
    public const string Or = "or";

    /// <summary>Logical negation.</summary>
    public const string Not = "not";

    /// <summary>
    /// Exclusive-OR of two or more operands.  Defined in the FIXatdl spec but rare in practice;
    /// preserved as a distinct kind so evaluators can handle it explicitly rather than
    /// silently mapping it to an incorrect combinator.
    /// </summary>
    public const string Xor = "xor";
}
