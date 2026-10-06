namespace FixPortal.FixAtdl.Contracts;

/// <summary>
/// Wire representation of a FIXatdl Parameter element, carrying type metadata, constraints, and optional enumerated values.
/// </summary>
public sealed record AtdlParameterDto(
    string Name,
    int? FixTag,
    string Type,
    IReadOnlyList<AtdlEnumPairDto>? EnumValues,
    object? Min,
    object? Max,
    int? Precision,
    bool MutableOnCxlRpl,
    string? UseValue,
    object? DefaultValue,
    string? TrueWireValue = null,
    string? FalseWireValue = null,
    bool? InvertOnWire = null,
    string? LocalMktTz = null,
    object? ConstValue = null,
    int? MinLength = null,
    int? MaxLength = null,
    bool? MultiplyBy100 = null
);
