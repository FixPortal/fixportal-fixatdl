// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Globalization;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Types.Support;
using FixPortal.FixAtdl.Resources;

namespace FixPortal.FixAtdl.Model.Types;

/// <summary>
/// 'string field representing the time represented based on ISO 8601. This is the time with a UTC offset to allow identification 
/// of local time and timezone of that time.
/// Format is HH:MM[:SS][Z | [ + | - hh[:mm]]] where HH = 00-23 hours, MM = 00-59 minutes, SS = 00-59 seconds, hh = 01-12 offset 
/// hours, mm = 00-59 offset minutes.
/// Example: 07:39Z is 07:39 UTC
/// Example: 02:39-05 is five hours behind UTC, thus Eastern Time
/// Example: 15:39+08 is eight hours ahead of UTC, Hong Kong/Singapore time
/// Example: 13:09+05:30 is 5.5 hours ahead of UTC, India time'
/// </summary>
public class TZTimeOnly_t : DateTimeTypeBase
{
    private static readonly string[] _formatStrings = [FixDateTimeFormat.FixTimeOnlyWithTz];

    /// <summary>
    /// Gets the DateTime format strings to use when converting this date/time to a FIX string and vice versa.
    /// </summary>
    /// <returns>Format strings suitable when calling DateTime.ToString().</returns>
    /// <remarks>When converting from DateTime to string, the first member of the returned array is used.  When
    /// converting from string to DateTime, the member of the array that has the same length as the string
    /// value is used.</remarks>
    protected override string[] GetDateTimeFormatStrings()
    {
        return _formatStrings;
    }

    /// <summary>
    /// Normalise the explicit timezone offset carried by the wire value to UTC so that the
    /// round-tripped value is canonical (emitted as 'Z') and independent of the parsing host's
    /// local offset. The instant is preserved exactly; the original offset *representation*
    /// (e.g. "-05") is not retained — full fidelity would require carrying a DateTimeOffset.
    /// </summary>
    protected override DateTimeStyles WireParseStyles =>
        DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;

    /// <summary>
    /// Gets the human-readable type name for use in error messages shown to the user.
    /// </summary>
    /// <returns>Human-readable type name.</returns>
    protected override string GetHumanReadableTypeName()
    {
        return HumanReadableTypeNames.TimeType;
    }
}

