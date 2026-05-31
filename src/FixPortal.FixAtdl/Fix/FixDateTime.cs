// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Globalization;
using FixPortal.FixAtdl.Diagnostics;
using FixPortal.FixAtdl.Resources;

namespace FixPortal.FixAtdl.Fix;

/// <summary>
/// Static class that provides utility methods for dealing with FIX format dates and times.
/// </summary>
public static class FixDateTime
{
    private static readonly string ExceptionContext = "FixPortal.FixAtdl.Fix.FixDateTime";

    /// <summary>
    /// Attempts to convert the supplied string to a <see cref="DateTime"/> using either the specified
    /// format provider or any of the valid FIX date/time formats.
    /// </summary>
    /// <param name="value">String value to attempt to convert.</param>
    /// <param name="provider">Format provider to use.</param>
    /// <param name="result">If successful, the DateTime equivalent representation of the supplied string; undefined otherwise.</param>
    /// <returns>True if the supplied value could be converted; false otherwise.</returns>
    public static bool TryParse(string value, IFormatProvider provider, out DateTime result)
    {
        result = DateTime.MinValue;

        // Try the exact FIX formats first (with AssumeUniversal so an offset-less value is treated as UTC
        // rather than host-local, plus AdjustToUniversal so the result is canonically Kind=Utc — independent
        // of the host offset — aligning with the UTC-family WireParseStyles (M1)). Only fall back to a loose
        // locale parse for non-FIX input. Exact-first avoids a locale-dependent loose parse silently winning
        // over a valid FIX format.
        return DateTime.TryParseExact(value, FixDateTimeFormat.AllFormats, provider, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out result) ||
            DateTime.TryParse(value, provider, DateTimeStyles.AllowWhiteSpaces, out result);
    }

    /// <summary>
    /// Attempts to convert the supplied string to a <see cref="DateTime"/> using either the specified
    /// format provider or any of the valid FIX date/time formats, throwing an exception if the conversion fails.
    /// </summary>
    /// <param name="value">String value to attempt to convert.</param>
    /// <param name="provider">Format provider to use.</param>
    /// <returns>If successful, the DateTime equivalent representation of the supplied string.</returns>
    public static DateTime Parse(string value, IFormatProvider provider)
    {

        if (TryParse(value, provider, out DateTime result))
        {
            return result;
        }

        throw ThrowHelper.New<InvalidCastException>(ExceptionContext, ErrorMessages.DataConversionError1, value, "DateTime");
    }
}

