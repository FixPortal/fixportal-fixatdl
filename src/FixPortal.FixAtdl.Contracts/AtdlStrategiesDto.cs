namespace FixPortal.FixAtdl.Contracts;

/// <summary>
/// Top-level container holding all strategies parsed from one ATDL file.
/// </summary>
public sealed record AtdlStrategiesDto(IReadOnlyList<AtdlStrategyDto> Strategies);
