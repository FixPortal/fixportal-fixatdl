namespace FixPortal.FixAtdl.Contracts;

/// <summary>
/// One item in a list-based FIXatdl control, pairing the enumeration identifier with its UI display string.
/// </summary>
public sealed record AtdlListItemDto(string EnumId, string UiRep);
