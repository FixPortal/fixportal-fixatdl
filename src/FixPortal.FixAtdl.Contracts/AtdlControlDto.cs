namespace FixPortal.FixAtdl.Contracts;

/// <summary>
/// Wire representation of a FIXatdl Control element, inlining the resolved parameter and state rules for UI rendering.
/// </summary>
public sealed record AtdlControlDto(
    string Id,
    string Type,
    string? Label,
    string? ParameterRef,
    AtdlParameterDto? Parameter,
    IReadOnlyList<AtdlListItemDto>? ListItems,
    object? InitValue,
    IReadOnlyList<AtdlStateRuleDto> StateRules,
    string? Tooltip,
    string? CheckedEnumRef = null,
    string? UncheckedEnumRef = null,
    string? RadioGroup = null,
    decimal? Increment = null,
    int? InitValueMode = null,
    string? LocalMktTz = null,
    decimal? InnerIncrement = null,
    decimal? OuterIncrement = null,
    string? InitPolicy = null,
    int? InitFixField = null
) : AtdlPanelChildDto;
