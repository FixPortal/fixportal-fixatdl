using System.Globalization;

namespace FixPortal.FixAtdl.Model.Types.Support;

/// <summary>
/// Shared original-wire-value round-trip logic for the timezone-bearing date/time types
/// (<see cref="TZTimestamp_t"/> and <see cref="TZTimeOnly_t"/>). Both types store the parsed
/// instant normalised to UTC but cache the exact original wire string, so an unchanged value
/// re-emits verbatim — offset included — while a programmatically-set value is emitted canonical
/// UTC ('Z'). The cache is per-instance state and stays on the owning type; only the stateless
/// capture/emission logic lives here.
/// </summary>
internal static class WireValueRoundTrip
{
    /// <summary>
    /// Captures the exact original wire string alongside the parsed value, but only when the wire
    /// text re-parses exactly against the owning type's format set; otherwise clears both slots.
    /// </summary>
    /// <param name="wireValue">The wire string exactly as supplied to the parse path.</param>
    /// <param name="formatStrings">The owning type's accepted FIX format strings.</param>
    /// <param name="parsedValue">The value an unchanged emission is compared against — the parsed
    /// UTC instant for a date/time type, or the sentinel-anchored time-of-day for a time-only type.
    /// Null (parse failed) clears both slots.</param>
    /// <param name="originalWireValue">Receives the captured wire string, or null when not captured.</param>
    /// <param name="capturedParsedValue">Receives <paramref name="parsedValue"/>, or null when not captured.</param>
    internal static void Capture(
        string wireValue,
        string[] formatStrings,
        DateTime? parsedValue,
        out string? originalWireValue,
        out DateTime? capturedParsedValue
    )
    {
        if (
            parsedValue != null
            && DateTimeOffset.TryParseExact(
                wireValue,
                formatStrings,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _
            )
        )
        {
            originalWireValue = wireValue;
            capturedParsedValue = parsedValue;
        }
        else
        {
            originalWireValue = null;
            capturedParsedValue = null;
        }
    }

    /// <summary>
    /// Converts the supplied value to its FIX wire representation, preserving fractional seconds when
    /// present. The exact original wire string is round-tripped while the instant is unchanged from
    /// what was parsed; any other value is emitted canonical UTC ('Z').
    /// </summary>
    /// <param name="value">Value to convert, may be null.</param>
    /// <param name="originalWireValue">The captured original wire string, or null when none was captured.</param>
    /// <param name="parsedValue">The parsed value the captured wire string belongs to.</param>
    /// <param name="wholeSecondsFormat">Format used when the value carries no sub-second component.</param>
    /// <param name="fractionalSecondsFormat">Format used when the value carries a sub-second component.</param>
    /// <returns>The FIX wire representation, or null.</returns>
    internal static string? Emit(
        DateTime? value,
        string? originalWireValue,
        DateTime? parsedValue,
        string wholeSecondsFormat,
        string fractionalSecondsFormat
    )
    {
        if (value == null)
        {
            return null;
        }

        DateTime adjustedValue = value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
        };

        // Round-trip the exact original wire string only when the instant is unchanged from
        // what was parsed. For any other instant emit canonical UTC ('Z'): the parsed offset
        // belongs to the parsed instant alone and must not be re-applied to a value set
        // programmatically (control/SetWireValue) after the parse, which previously emitted
        // the wrong offset.
        if (originalWireValue != null && parsedValue != null && adjustedValue.Equals(parsedValue.Value))
        {
            return originalWireValue;
        }

        string format =
            adjustedValue.Ticks % TimeSpan.TicksPerSecond == 0 ? wholeSecondsFormat : fractionalSecondsFormat;
        return adjustedValue.ToString(format, CultureInfo.InvariantCulture);
    }
}
