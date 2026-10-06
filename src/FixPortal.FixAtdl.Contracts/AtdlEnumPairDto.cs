namespace FixPortal.FixAtdl.Contracts;

/// <summary>
/// One legal enumerated value for a FIXatdl parameter, pairing the enumeration identifier with the FIX wire value.
/// </summary>
public sealed record AtdlEnumPairDto(string EnumId, string WireValue);
