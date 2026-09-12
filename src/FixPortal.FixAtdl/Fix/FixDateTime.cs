// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Globalization;
using System.Text.RegularExpressions;
using FixPortal.FixAtdl.Diagnostics;
using FixPortal.FixAtdl.Resources;

namespace FixPortal.FixAtdl.Fix;

/// <summary>
/// Static class that provides utility methods for dealing with FIX format dates and times.
/// </summary>
public static partial class FixDateTime
{
    private static readonly string ExceptionContext = "FixPortal.FixAtdl.Fix.FixDateTime";

    // Matches a literal "60" seconds field (a declared UTC leap second, FIX-legal per the UTCTimestamp_t
    // spec) preceded by "MM:" and not followed by a further digit, so it only ever matches the SS token in
    // every FixDateTimeFormat variant that carries seconds (never minutes, hours, or a malformed "600").
    [GeneratedRegex(@"(?<=:\d{2}:)60(?!\d)")]
    private static partial Regex LeapSecondPattern();

    // Matches a fraction field carrying more digits than DateTime can represent (7 tick digits).
    // FIX timestamps may legitimately carry nanosecond precision beyond that.
    [GeneratedRegex(@"(\.\d{7})\d+")]
    private static partial Regex ExcessFractionPattern();

    /// <summary>
    /// Normalises a declared UTC leap second (a literal ":60" seconds field - legal per the
    /// UTCTimestamp_t spec) to ":59" so that a DateTime parse can represent it; the caller rolls
    /// the parsed value forward by one second. DateTime has no 60th second; DateTime.AddSeconds
    /// cascades minute/hour/day/month/year rollover on its own, matching the spec's own worked
    /// example (19981231-23:59:60 -&gt; 19990101-00:00:00).
    /// </summary>
    internal static string NormaliseLeapSecond(string value, out bool wasLeapSecond)
    {
        Match leapSecondMatch = LeapSecondPattern().Match(value);
        if (!leapSecondMatch.Success)
        {
            wasLeapSecond = false;
            return value;
        }

        wasLeapSecond = true;
        return string.Concat(value.AsSpan(0, leapSecondMatch.Index), "59", value.AsSpan(leapSecondMatch.Index + 2));
    }

    /// <summary>
    /// Classifies the supplied text as a date-less (time-of-day-only) FIX value: it parses exactly
    /// against one of the <see cref="FixDateTimeFormat"/> time-only formats and nothing else.
    /// </summary>
    /// <remarks>Classifying from the parse result replaces the previous "first eight characters are
    /// digits" heuristic, which misclassified ISO-8601 (<c>2026-06-01T12:00:00Z</c>), date-only
    /// (<c>2026-06-01</c>) and leading-space date-time text as time-only and so silently degraded a
    /// date-bearing bound to a recurring daily window (R04).</remarks>
    internal static bool IsTimeOnlyText(string text)
    {
        return DateTime.TryParseExact(
            text,
            FixDateTimeFormat.TimeOnlyFormatsArray,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out _
        );
    }

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
        // Try the exact FIX formats first (with AssumeUniversal so an offset-less value is treated as UTC
        // rather than host-local, plus AdjustToUniversal so the result is canonically Kind=Utc — independent
        // of the host offset — aligning with the UTC-family WireParseStyles (M1)). Only fall back to a loose
        // locale parse for non-FIX input. Exact-first avoids a locale-dependent loose parse silently winning
        // over a valid FIX format.
        // Apply the SAME styles to both the exact-FIX-format path and the loose fallback so that a value
        // only the fallback can parse still yields a canonical Kind=Utc result. Previously the fallback
        // omitted AssumeUniversal/AdjustToUniversal, so its result Kind drifted to Unspecified (or Local
        // when the input carried an offset) — inconsistent with the exact path's documented UTC contract.
        const DateTimeStyles styles =
            DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;

        // A null (reachable via a programmatically stored null in FixTagValuesCollection) or empty
        // value is simply not parseable; a Try method must not throw.
        if (string.IsNullOrEmpty(value))
        {
            result = default;
            return false;
        }

        string parseValue = NormaliseLeapSecond(value, out bool wasLeapSecond);

        // Truncate any fraction digits beyond what DateTime can hold so a valid nanosecond-precision
        // timestamp is not rejected outright.
        parseValue = ExcessFractionPattern().Replace(parseValue, "$1");

        bool parsed =
            DateTime.TryParseExact(parseValue, FixDateTimeFormat.FormatsArray, provider, styles, out result)
            || DateTime.TryParse(parseValue, provider, styles, out result);

        if (!parsed || !wasLeapSecond)
        {
            return parsed;
        }

        // A leap second at the last representable instant would roll past DateTime.MaxValue; report
        // failure rather than throw from AddSeconds.
        if (result > DateTime.MaxValue.AddSeconds(-1))
        {
            result = default;
            return false;
        }

        result = result.AddSeconds(1);

        return true;
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

        throw ThrowHelper.New<InvalidCastException>(
            ExceptionContext,
            ErrorMessages.DataConversionError1,
            value,
            "DateTime"
        );
    }
}
