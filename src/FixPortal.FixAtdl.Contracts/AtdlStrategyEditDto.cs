namespace FixPortal.FixAtdl.Contracts;

/// <summary>Validation assertion over parameter values, with its authored failure message.</summary>
public sealed record AtdlStrategyEditDto(string ErrorMessage, StateRuleAstNodeDto Expression);
