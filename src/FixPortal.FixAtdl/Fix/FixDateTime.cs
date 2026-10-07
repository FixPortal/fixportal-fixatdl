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

    private const DateTimeStyles CanonicalStyles =
        DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;

    // Matches a literal "60" seconds field (a declared UTC leap second, FIX-legal per the UTCTimestamp_t
    // spec) only when it directly follows "23:59:" and is not followed by a further digit. A leap second
    // can only ever be declared as the 60th second of 23:59 UTC, so a ":60" at any other minute is left
    // in place and fails parsing like any other invalid value rather than being silently shifted forward.
    [GeneratedRegex(@"(?<=23:59:)60(?!\d)")]
    private static partial Regex LeapSecondPattern();

    // Matches a fraction field carrying more digits than DateTime can represent (7 tick digits).
    // FIX timestamps may legitimately carry nanosecond precision beyond that.
    [GeneratedRegex(@"(\.\d{7})\d+")]
    private static partial Regex ExcessFractionPattern();

    /// <summary>
    /// Normalises a declared UTC leap second (a literal ":60" seconds field — legal per the
    /// UTCTimestamp_t spec only as the 60th second of 23:59 UTC) to ":59" so that a DateTime parse can
    /// represent it; the caller rolls the parsed value forward by one second. DateTime has no 60th
    /// second; DateTime.AddSeconds cascades minute/hour/day/month/year rollover on its own, matching
    /// the spec's own worked example (19981231-23:59:60 -&gt; 19990101-00:00:00). A ":60" seconds field
    /// at any other minute is not a legal leap second and is left untouched, so it fails parsing like
    /// any other invalid value rather than being silently shifted into the next minute.
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
    /// Truncates fractional-second digits beyond what DateTime can hold (seven tick digits), so a valid
    /// higher-precision value parses/classifies instead of being rejected outright.
    /// </summary>
    internal static string TruncateExcessFraction(string value) => ExcessFractionPattern().Replace(value, "$1");

    /// <summary>
    /// Classifies the supplied text as a date-less (time-of-day-only) FIX value: it parses exactly
    /// against one of the <see cref="FixDateTimeFormat"/> time-only formats and nothing else.
    /// </summary>
    /// <remarks>Classifying from the parse result replaces the previous "first eight characters are
    /// digits" heuristic, which misclassified ISO-8601 (<c>2026-06-01T12:00:00Z</c>), date-only
    /// (<c>2026-06-01</c>) and leading-space date-time text as time-only and so silently degraded a
    /// date-bearing bound to a recurring daily window (R04). The same normalisation the parse path
    /// applies (leap second, excess-fraction truncation) runs here too, so a value the parser accepts
    /// — "23:59:60", "12:34:56.123456789" — is not misclassified as date-bearing.</remarks>
    internal static bool IsTimeOnlyText(string text)
    {
        string parseValue = NormaliseLeapSecond(text, out _);
        parseValue = TruncateExcessFraction(parseValue);

        return DateTime.TryParseExact(
            parseValue,
            FixDateTimeFormat.TimeOnlyFormatsArray,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out _
        );
    }

    /// <summary>
    /// Attempts to convert the supplied string to a <see cref="DateTime"/> using any of the valid FIX
    /// date/time formats, falling back to a loose parse with the specified format provider.
    /// </summary>
    /// <param name="value">String value to attempt to convert; null reports failure rather than throwing.</param>
    /// <param name="provider">Format provider used by the loose fallback parse only.</param>
    /// <param name="result">If successful, the DateTime equivalent representation of the supplied string; undefined otherwise.</param>
    /// <returns>True if the supplied value could be converted; false otherwise.</returns>
    /// <remarks>The exact FIX-format parse is culture-invariant, because the FIX wire format itself is
    /// culture-fixed; <paramref name="provider"/> applies only to the loose fallback parse of non-FIX
    /// input. A date-less (time-of-day-only) input is anchored to 0001-01-01 with Kind=Utc rather than
    /// to the host's current date, so the result is deterministic and host-timezone-independent.</remarks>
    public static bool TryParse(string? value, IFormatProvider provider, out DateTime result)
    {
        // Try the exact FIX formats first (with AssumeUniversal so an offset-less value is treated as UTC
        // rather than host-local, plus AdjustToUniversal so the result is canonically Kind=Utc — independent
        // of the host offset — aligning with the UTC-family WireParseStyles). Only fall back to a loose
        // locale parse for non-FIX input. Exact-first avoids a locale-dependent loose parse silently winning
        // over a valid FIX format.
        // Apply the SAME styles to both the exact-FIX-format path and the loose fallback so that a value
        // only the fallback can parse still yields a canonical Kind=Utc result.

        // A null (reachable via a programmatically stored null in FixTagValuesCollection) or empty
        // value is simply not parseable; a Try method must not throw.
        if (string.IsNullOrEmpty(value))
        {
            result = default;
            return false;
        }

        ExactParseStatus exact = ParseExactFix(value, out result);
        if (exact == ExactParseStatus.Parsed)
        {
            return true;
        }

        // An exact FIX value that cannot be represented (a leap second past DateTime.MaxValue) must not
        // fall through to the loose parse and succeed by another spelling.
        if (exact == ExactParseStatus.Rejected)
        {
            return false;
        }

        string parseValue = NormaliseLeapSecond(value, out bool wasLeapSecond);
        parseValue = TruncateExcessFraction(parseValue);

        // The caller's provider applies only to this loose fallback. The exact path above is
        // culture-fixed: resolving the format's ':' as a culture-sensitive time separator would reject
        // valid FIX timestamps under a culture whose TimeSeparator is not ':' (e.g. fi-FI uses '.').
        if (!DateTime.TryParse(parseValue, provider, CanonicalStyles, out result))
        {
            return false;
        }

        return ApplyPostParseAdjustments(value, wasLeapSecond, ref result);
    }

    /// <summary>
    /// Parses <paramref name="value"/> against the exact FIX formats only. Bounds use this so a loose
    /// spelling such as <c>1:00 PM</c> cannot become an absolute timestamp on the host's current date.
    /// </summary>
    internal static bool TryParseExactFix(string? value, out DateTime result)
    {
        if (string.IsNullOrEmpty(value))
        {
            result = default;
            return false;
        }

        return ParseExactFix(value, out result) == ExactParseStatus.Parsed;
    }

    private static ExactParseStatus ParseExactFix(string value, out DateTime result)
    {
        string parseValue = NormaliseLeapSecond(value, out bool wasLeapSecond);
        parseValue = TruncateExcessFraction(parseValue);

        if (
            !DateTime.TryParseExact(
                parseValue,
                FixDateTimeFormat.FormatsArray,
                CultureInfo.InvariantCulture,
                CanonicalStyles,
                out result
            )
        )
        {
            return ExactParseStatus.NotExact;
        }

        return ApplyPostParseAdjustments(value, wasLeapSecond, ref result)
            ? ExactParseStatus.Parsed
            : ExactParseStatus.Rejected;
    }

    // Roll a declared leap second forward before re-anchoring a time-only value. Pinning the date
    // first made "23:59:60" land on 0001-01-02 00:00:00; the wire path then re-anchored that midnight
    // to 0001-01-01, so the two paths disagreed. A date-less value otherwise takes its date from the
    // host clock (and AdjustToUniversal can roll it across midnight), so the re-anchor still pins the
    // rolled time-of-day to 0001-01-01 with Kind=Utc.
    private static bool ApplyPostParseAdjustments(string original, bool wasLeapSecond, ref DateTime result)
    {
        if (wasLeapSecond)
        {
            // A leap second at the last representable instant would roll past DateTime.MaxValue; report
            // failure rather than throw from AddSeconds.
            if (result > DateTime.MaxValue.AddSeconds(-1))
            {
                result = default;
                return false;
            }

            result = result.AddSeconds(1);
        }

        if (IsTimeOnlyText(original))
        {
            result = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc).Add(result.TimeOfDay);
        }

        return true;
    }

    private enum ExactParseStatus
    {
        NotExact,
        Parsed,
        Rejected,
    }

    /// <summary>
    /// Attempts to convert the supplied string to a <see cref="DateTime"/> using any of the valid FIX
    /// date/time formats, falling back to a loose parse with the specified format provider, and throwing
    /// an exception if the conversion fails.
    /// </summary>
    /// <param name="value">String value to attempt to convert.</param>
    /// <param name="provider">Format provider used by the loose fallback parse only.</param>
    /// <returns>If successful, the DateTime equivalent representation of the supplied string.</returns>
    /// <exception cref="FormatException">Thrown when the supplied value cannot be converted to a DateTime.</exception>
    public static DateTime Parse(string value, IFormatProvider provider)
    {
        if (TryParse(value, provider, out DateTime result))
        {
            return result;
        }

        throw ThrowHelper.New<FormatException>(ExceptionContext, ErrorMessages.DataConversionError1, value, "DateTime");
    }
}
