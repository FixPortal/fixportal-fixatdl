namespace FixPortal.FixAtdl.Contracts;

/// <summary>
/// String constants for the <see cref="StateRuleAstNodeDto.Operator"/> field.
/// Shared with the @fix-portal/fixatdl-react TypeScript evaluator; keep the values identical.
/// </summary>
public static class StateRuleOperator
{
    /// <summary>Equal to.</summary>
    public const string Eq = "==";

    /// <summary>Not equal to.</summary>
    public const string Neq = "!=";

    /// <summary>Greater than.</summary>
    public const string Gt = ">";

    /// <summary>Less than.</summary>
    public const string Lt = "<";

    /// <summary>Greater than or equal to.</summary>
    public const string Ge = ">=";

    /// <summary>Less than or equal to.</summary>
    public const string Le = "<=";

    /// <summary>The field has a value (FIXatdl Exist operator).</summary>
    public const string Exists = "exists";

    /// <summary>The field has no value (FIXatdl NotExist operator).</summary>
    public const string NotExists = "not-exists";
}
