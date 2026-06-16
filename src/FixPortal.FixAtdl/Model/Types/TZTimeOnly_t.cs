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
    private static readonly string[] _formatStrings =
    [
        FixDateTimeFormat.FixTimeOnlyWithTz,
        FixDateTimeFormat.FixTimeOnlyMinutesWithUtcDesignator,
        FixDateTimeFormat.FixTimeOnlyMinutesWithHourOffset,
        FixDateTimeFormat.FixTimeOnlyMinutesWithMinuteOffset,
        FixDateTimeFormat.FixTimeOnlyWithHourOffset,
        FixDateTimeFormat.FixTimeOnlyFractionalWithHourOffset,
        FixDateTimeFormat.FixTimeOnlyFractionalWithMinuteOffset
    ];

    /// <inheritdoc />
    internal override bool IsTimeOnlyType => true;

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

    private string? _originalWireValue;
    private DateTime? _parsedUtcValue;

    /// <summary>
    /// Gets the human-readable type name for use in error messages shown to the user.
    /// </summary>
    /// <returns>Human-readable type name.</returns>
    protected override string GetHumanReadableTypeName()
    {
        return HumanReadableTypeNames.TimeType;
    }

    /// <summary>
    /// Converts the supplied wire value to a canonical UTC time-only value with a stable sentinel date.
    /// </summary>
    /// <param name="value">Wire-formatted time value.</param>
    /// <returns>The parsed time anchored to <c>0001-01-01</c>.</returns>
    protected override DateTime? ConvertFromWireValueFormat(string value)
    {
        DateTime? parsed = base.ConvertFromWireValueFormat(value);

        if (parsed == null)
        {
            _originalWireValue = null;
            _parsedUtcValue = null;
            return null;
        }

        DateTime result = parsed.Value;
        DateTime anchored = new DateTime(1, 1, 1, result.Hour, result.Minute, result.Second, result.Millisecond, result.Kind)
            .AddTicks(result.Ticks % TimeSpan.TicksPerMillisecond);

        if (DateTimeOffset.TryParseExact(value, _formatStrings, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            _originalWireValue = value;
            _parsedUtcValue = anchored;
        }
        else
        {
            _originalWireValue = null;
            _parsedUtcValue = null;
        }

        return anchored;
    }

    /// <summary>
    /// Converts the supplied value to a string, preserving fractional seconds when present.
    /// </summary>
    /// <param name="value">Value to convert, may be null.</param>
    /// <returns>The FIX wire representation, or null.</returns>
    protected override string? ConvertToWireValueFormat(DateTime? value)
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
        if (_originalWireValue != null && _parsedUtcValue != null && adjustedValue.Equals(_parsedUtcValue.Value))
        {
            return _originalWireValue;
        }

        string format = adjustedValue.Ticks % TimeSpan.TicksPerSecond == 0
            ? FixDateTimeFormat.FixTimeOnlyWithTz
            : FixDateTimeFormat.FixTimeOnlyFractionalWithMinuteOffset;
        return adjustedValue.ToString(format, CultureInfo.InvariantCulture);
    }
}
