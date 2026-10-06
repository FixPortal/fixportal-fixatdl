namespace FixPortal.FixAtdl.Contracts;

/// <summary>
/// Wire representation of a FIXatdl StrategyPanel, carrying layout metadata and an ordered list of child panels or controls.
/// </summary>
public sealed record AtdlPanelDto(
    string? Title,
    string Border,
    string Orientation,
    bool Collapsible,
    bool Collapsed,
    IReadOnlyList<AtdlPanelChildDto> Children
) : AtdlPanelChildDto;
