// FP Enhancement: 2026-10-07 — invariant enum ids and exact JSON integer wires.
using System.Globalization;
using System.Text.Json;

namespace FixPortal.FixAtdl.Contracts;

/// <summary>
/// Formats a FIXatdl parameter value into the FIX wire string for tag 960 (StrategyParameterValue).
/// Centralised so value formatting is reusable across future emitters and the AtdlDtoMapper.
/// </summary>
public static class FixValueFormatter
{
    /// <summary>
    /// Produces the tag-960 wire value string for the given parameter value.
    /// </summary>
    /// <param name="value">The raw value from the filled-values dictionary.</param>
    /// <param name="fixatdlType">The FIXatdl type string (e.g. "Int_t", "Boolean_t").</param>
    /// <param name="enumValues">
    ///   The parameter's enum pairs, if any.  When non-null and the value matches an EnumId,
    ///   the corresponding WireValue is emitted instead of the raw value.
    /// </param>
    public static string Format(object value, string fixatdlType, IReadOnlyList<AtdlEnumPairDto>? enumValues)
    {
        // Enum-bound parameters: map EnumId → WireValue.  If the filled value does not match
        // any EnumId (e.g. a free-text entry when the control is not a pure dropdown), fall
        // through to the type-based formatting below so the raw string is emitted.
        value = Unwrap(value);

        if (enumValues is { Count: > 0 } && Convert.ToString(value, CultureInfo.InvariantCulture) is string strValue)
        {
            var match = enumValues.FirstOrDefault(pair =>
                string.Equals(pair.EnumId, strValue, StringComparison.Ordinal)
            );

            // No matching EnumId — fall through to type-based formatting below rather than
            // returning the raw string, so non-enum-bound types (dates, numerics, etc.) still
            // get their canonical wire representation.
            if (match is not null)
            {
                return match.WireValue;
            }
        }

        return fixatdlType switch
        {
            // FIX Boolean is Y/N, not true/false.
            "Boolean_t" => value is bool b ? FixBoolean(b) : (value.ToString() ?? string.Empty),

            // Numeric types: InvariantCulture to avoid locale-specific decimal separators.
            "Float_t" or "Qty_t" or "Price_t" or "PriceOffset_t" or "Amt_t" or "Percentage_t" => FormatNumeric(value),

            // Integer-like types: format as integer string.
            "Int_t" or "Length_t" or "NumInGroup_t" or "SeqNum_t" or "TagNum_t" => FormatInteger(value),

            // Date/time types: FIX canonical formats, always InvariantCulture.
            "UTCTimestamp_t" => FormatUtcTimestamp(value),
            "LocalMktDate_t" or "UTCDateOnly_t" => FormatDate(value),
            "UTCTimeOnly_t" => FormatTimeOnly(value),
            "MonthYear_t" => FormatMonthYear(value),

            // For all remaining types — including String_t, Char_t, Currency_t, Exchange_t,
            // MultipleStringValue_t, etc. — emit the value as-is.  FIXatdl forms primarily
            // produce strings, so ToString() is safe and correct here.
            _ => value.ToString() ?? string.Empty,
        };
    }

    // ---------------------------------------------------------------------------
    // Private formatters
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Unwraps a <see cref="JsonElement"/> (as arrives when filled values were deserialised by
    /// System.Text.Json without a post-processing step) to its underlying CLR value, so the
    /// type-based formatting below can pattern-match on the real type instead of always hitting
    /// the ToString() fallback.
    /// </summary>
    private static object Unwrap(object value)
    {
        if (value is not JsonElement el)
        {
            return value;
        }

        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString() ?? string.Empty,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => UnwrapNumber(el),
            JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
            _ => el.GetRawText(),
        };
    }

    /// <summary>FIX Boolean is the literal Y/N, not true/false.</summary>
    private static string FixBoolean(bool value) => value ? "Y" : "N";

    private static string FormatNumeric(object value)
    {
        // "R" (round-trip) can emit scientific notation (e.g. "1E-05"), which is not a valid
        // FIX float. Format via decimal instead — no exponents, no precision loss for the
        // magnitudes FIX values use — and InvariantCulture so the separator is always ".".
        // Fall back to raw ToString() when the value is already a string (e.g. from a form
        // text field) or cannot be converted, rather than throwing — the counterparty will
        // validate the content.
        return value switch
        {
            double d when !double.IsFinite(d) => throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "FIX numeric values must be finite."
            ),
            float f when !float.IsFinite(f) => throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "FIX numeric values must be finite."
            ),
            double d => d.ToString("0.############################", CultureInfo.InvariantCulture),
            float f => f.ToString("0.############################", CultureInfo.InvariantCulture),
            decimal m => m.ToString(CultureInfo.InvariantCulture),
            string s => s,
            _ => TryConvert(
                () => Convert.ToDecimal(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
                value
            ),
        };
    }

    private static string FormatInteger(object value)
    {
        // Fall back to raw string when the value is already a string representation,
        // or cannot be converted, rather than throwing — defensive against form values
        // that arrive as strings or incompatible CLR types.
        return value switch
        {
            int i => i.ToString(CultureInfo.InvariantCulture),
            long l => l.ToString(CultureInfo.InvariantCulture),
            string s => s,
            double d => FormatWholeNumber(d),
            float f => FormatWholeNumber(f),
            decimal m => FormatWholeNumber(m),
            _ => TryConvert(
                () => Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
                value
            ),
        };
    }

    private static object UnwrapNumber(JsonElement element)
    {
        if (element.TryGetInt64(out long whole))
        {
            return whole;
        }

        if (element.TryGetDecimal(out decimal number))
        {
            return number;
        }

        return element.GetDouble();
    }

    private static string FormatWholeNumber(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "FIX integer values must be integral.");
        }

        try
        {
            return FormatWholeNumber(new decimal(value));
        }
        catch (OverflowException)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "FIX integer values must be integral.");
        }
    }

    private static string FormatWholeNumber(float value) => FormatWholeNumber((double)value);

    private static string FormatWholeNumber(decimal value)
    {
        if (value != decimal.Truncate(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "FIX integer values must be integral.");
        }

        return decimal.ToInt64(value).ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Runs a conversion delegate, falling back to <c>value.ToString()</c> when the CLR type
    /// is incompatible with the requested conversion (e.g. a non-numeric string, or a type
    /// that doesn't implement <see cref="IConvertible"/>) rather than throwing.
    /// </summary>
    private static string TryConvert(Func<string> convert, object value)
    {
        try
        {
            return convert();
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            return value.ToString() ?? string.Empty;
        }
    }

    /// <summary>
    /// Converts a Local-kind DateTime to UTC; leaves Utc/Unspecified untouched (Unspecified is
    /// treated as already-UTC, matching prior behaviour for callers that don't set Kind).
    /// </summary>
    private static DateTime ToUtc(DateTime dt) => dt.Kind == DateTimeKind.Local ? dt.ToUniversalTime() : dt;

    private static string FormatUtcTimestamp(object value)
    {
        // FIX UTCTimestamp wire format: yyyyMMdd-HH:mm:ss.fff
        return value switch
        {
            DateTime dt => ToUtc(dt).ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.UtcDateTime.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
    }

    private static string FormatDate(object value)
    {
        // FIX UTCDateOnly / LocalMktDate wire format: yyyyMMdd
        return value switch
        {
            DateTime dt => dt.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            DateOnly d => d.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
    }

    private static string FormatTimeOnly(object value)
    {
        // FIX UTCTimeOnly wire format: HH:mm:ss.fff
        return value switch
        {
            DateTime dt => ToUtc(dt).ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.UtcDateTime.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture),
            TimeOnly t => t.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture),
            TimeSpan ts => ts.ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
    }

    private static string FormatMonthYear(object value)
    {
        // FIX MonthYear wire format: yyyyMM (or yyyyMMww for week form, which is less common).
        return value switch
        {
            DateTime dt => dt.ToString("yyyyMM", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString("yyyyMM", CultureInfo.InvariantCulture),
            DateOnly d => d.ToString("yyyyMM", CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
    }
}
