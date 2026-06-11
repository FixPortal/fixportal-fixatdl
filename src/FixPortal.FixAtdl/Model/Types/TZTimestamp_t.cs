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
/// 'string field representing a time/date combination representing local time with an offset to UTC to allow identification of 
/// local time and timezone offset of that time. The representation is based on ISO 8601.
/// Format is YYYYMMDD-HH:MM:SS[Z | [ + | - hh[:mm]]] where YYYY = 0000 to 9999, MM = 01-12, DD = 01-31 HH = 00-23 hours, 
/// MM = 00-59 minutes, SS = 00-59 seconds, hh = 01-12 offset hours, mm = 00-59 offset minutes
/// Example: 20060901-07:39Z is 07:39 UTC on 1st of September 2006
/// Example: 20060901-02:39-05 is five hours behind UTC, thus Eastern Time on 1st of September 2006
/// Example: 20060901-15:39+08 is eight hours ahead of UTC, Hong Kong/Singapore time on 1st of September 2006
/// Example: 20060901-13:09+05:30 is 5.5 hours ahead of UTC, India time on 1st of September 2006'
/// </summary>
public class TZTimestamp_t : DateTimeTypeBase
{
    private static readonly string[] _formatStrings =
    [
        FixDateTimeFormat.FixDateTimeWithTz,
        FixDateTimeFormat.FixDateTimeMinutesWithUtcDesignator,
        FixDateTimeFormat.FixDateTimeMinutesWithHourOffset,
        FixDateTimeFormat.FixDateTimeMinutesWithMinuteOffset,
        FixDateTimeFormat.FixDateTimeWithHourOffset,
        FixDateTimeFormat.FixDateTimeFractionalWithHourOffset,
        FixDateTimeFormat.FixDateTimeFractionalWithMinuteOffset
    ];

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
        return HumanReadableTypeNames.TimestampType;
    }

    private string? _originalWireValue;
    private TimeSpan? _parsedOffset;
    private DateTime? _parsedUtcValue;

    /// <summary>
    /// Converts the supplied wire value to a DateTime? and captures the timezone offset.
    /// </summary>
    protected override DateTime? ConvertFromWireValueFormat(string value)
    {
        DateTime? parsed = base.ConvertFromWireValueFormat(value);

        if (parsed == null)
        {
            _originalWireValue = null;
            _parsedOffset = null;
            _parsedUtcValue = null;
            return null;
        }

        if (DateTimeOffset.TryParseExact(value, _formatStrings, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset dto))
        {
            _originalWireValue = value;
            _parsedOffset = dto.Offset;
            _parsedUtcValue = parsed;
        }
        else
        {
            _originalWireValue = null;
            _parsedOffset = null;
            _parsedUtcValue = null;
        }

        return parsed;
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

        if (_originalWireValue != null && _parsedUtcValue != null && adjustedValue.Equals(_parsedUtcValue.Value))
        {
            return _originalWireValue;
        }

        if (_parsedOffset != null)
        {
            DateTimeOffset dto = new DateTimeOffset(adjustedValue, TimeSpan.Zero).ToOffset(_parsedOffset.Value);
            string format = dto.Ticks % TimeSpan.TicksPerSecond == 0
                ? FixDateTimeFormat.FixDateTimeWithTz
                : FixDateTimeFormat.FixDateTimeFractionalWithMinuteOffset;
            return dto.ToString(format, CultureInfo.InvariantCulture);
        }
        else
        {
            string format = adjustedValue.Ticks % TimeSpan.TicksPerSecond == 0
                ? FixDateTimeFormat.FixDateTimeWithTz
                : FixDateTimeFormat.FixDateTimeFractionalWithMinuteOffset;
            return adjustedValue.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
