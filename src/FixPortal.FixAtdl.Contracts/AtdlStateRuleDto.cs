namespace FixPortal.FixAtdl.Contracts;

/// <summary>
/// Wire representation of one FIXatdl StateRule binding an AST condition to a UI effect on a control.
/// </summary>
public sealed record AtdlStateRuleDto(
    string Effect,
    bool TargetValue,
    StateRuleAstNodeDto Expression,
    string? TargetStringValue = null
);

/// <summary>
/// String constants for the legal state-rule effects, matching the values used in <see cref="AtdlStateRuleDto.Effect"/>.
/// </summary>
public static class AtdlStateRuleEffect
{
    /// <summary>Enable or disable the control.</summary>
    public const string Enabled = "enabled";

    /// <summary>Show or hide the control.</summary>
    public const string Visible = "visible";

    /// <summary>Set the control's value.</summary>
    public const string Value = "value";
}
