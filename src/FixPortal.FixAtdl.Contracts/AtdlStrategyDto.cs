namespace FixPortal.FixAtdl.Contracts;

/// <summary>
/// Wire representation of a single FIXatdl Strategy element with its parameters, panel tree, and source XML slice.
/// </summary>
public sealed record AtdlStrategyDto(
    string Name,
    string? Description,
    IReadOnlyList<AtdlParameterDto> Parameters,
    AtdlPanelDto Panel,
    string SourceXml,
    IReadOnlyList<AtdlStrategyEditDto>? StrategyEdits = null
);
