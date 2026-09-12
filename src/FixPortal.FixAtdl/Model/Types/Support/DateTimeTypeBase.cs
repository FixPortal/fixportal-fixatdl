// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Globalization;
using System.Text.RegularExpressions;
using FixPortal.FixAtdl.Diagnostics;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Controls.Support;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Resources;
using FixPortal.FixAtdl.Validation;

namespace FixPortal.FixAtdl.Model.Types.Support;

/// <summary>
/// Base class for all date and time related FIXatdl types (except MonthYear_t).
/// </summary>
public abstract partial class DateTimeTypeBase : AtdlValueType<DateTime>, IControlConvertible
{
    // TY1-G — MaxValue/MinValue are backed by fields whose setters also clear the corresponding
    // time-only slot, so a programmatic bound assignment cannot leave a stale _maxTimeOfDay/
    // _minTimeOfDay (written earlier via MaxValueText/MinValueText) applying alongside the new bound.
    private DateTime? _maxValue;
    private DateTime? _minValue;

    /// <summary>
    /// Maximum value for this date/time type, i.e., the latest acceptable date/time.
    /// </summary>
    public DateTime? MaxValue
    {
        get => _maxValue;
        set
        {
            _maxValue = value;
            _maxTimeOfDay = null;
            _maxTimeOfDayIsOffsetAnchored = false;
        }
    }

    /// <summary>
    /// Minimum value for this date/time type, i.e., the earliest acceptable date/time.
    /// </summary>
    public DateTime? MinValue
    {
        get => _minValue;
        set
        {
            _minValue = value;
            _minTimeOfDay = null;
            _minTimeOfDayIsOffsetAnchored = false;
        }
    }

    // C2 — time-only bound capture. A maxValue/minValue written as a bare time-of-day (HH:mm:ss[.fff])
    // is a time-of-day constraint, not a date+time one. The reflective parser routes the raw bound text
    // through MaxValueText/MinValueText; a time-only value is stored here (and compared on the time
    // component only), while a full datetime / date-only value continues to populate MaxValue/MinValue.
    private TimeOnly? _maxTimeOfDay;
    private TimeOnly? _minTimeOfDay;

    // A time-only bound carrying its own explicit offset suffix (the same base XML Schema "time" type
    // used for Clock initValue) is already UTC-anchored and needs no zone lookup; a bare bound with no
    // offset is market-local, per the spec's own worked example, and must be compared against the value
    // converted into the parameter's localMktTz zone (GetTimeOfDayForBounds). Tracked per-bound since
    // min and max can independently carry (or omit) an offset on the same parameter.
    private bool _maxTimeOfDayIsOffsetAnchored;
    private bool _minTimeOfDayIsOffsetAnchored;

    [GeneratedRegex(@"(?:Z|[+-]\d{2}:?\d{2})$")]
    private static partial Regex TrailingOffsetPattern();

    /// <summary>
    /// Indicates whether this type represents a time-only value.
    /// </summary>
    internal virtual bool IsTimeOnlyType => false;

    /// <summary>Deserialization-only round-trip of the raw <c>maxValue</c> attribute text; parsed with
    /// time-only awareness on set (C2). The getter returns the last raw text set (or null), even after
    /// <see cref="MaxValue"/> is assigned directly. Not intended for programmatic use; set
    /// <see cref="MaxValue"/> directly for a full date+time bound.</summary>
    public string? MaxValueText
    {
        get;
        set
        {
            field = value;
            if (value == null)
            {
                MaxValue = null;
                return;
            }
            SetBound(value, isMax: true);
        }
    }

    /// <summary>Deserialization-only round-trip of the raw <c>minValue</c> attribute text; parsed with
    /// time-only awareness on set (C2). The getter returns the last raw text set (or null), even after
    /// <see cref="MinValue"/> is assigned directly. Not intended for programmatic use; set
    /// <see cref="MinValue"/> directly for a full date+time bound.</summary>
    public string? MinValueText
    {
        get;
        set
        {
            field = value;
            if (value == null)
            {
                MinValue = null;
                return;
            }
            SetBound(value, isMax: false);
        }
    }

    private void SetBound(string text, bool isMax)
    {
        bool isTimeOnly = IsTimeOnlyType || IsDateLess(text);
        if (isTimeOnly)
        {
            DateTime parsed = FixDateTime.Parse(text, CultureInfo.InvariantCulture);
            DateTime normalised = parsed.Kind == DateTimeKind.Local ? parsed.ToUniversalTime() : parsed;
            TimeOnly timeOfDay = TimeOnly.FromDateTime(normalised);
            bool isOffsetAnchored = TrailingOffsetPattern().IsMatch(text);
            if (isMax)
            {
                _maxValue = null;
                _maxTimeOfDay = timeOfDay;
                _maxTimeOfDayIsOffsetAnchored = isOffsetAnchored;
            }
            else
            {
                _minValue = null;
                _minTimeOfDay = timeOfDay;
                _minTimeOfDayIsOffsetAnchored = isOffsetAnchored;
            }
        }
        else
        {
            // FixDateTime.Parse normalises FIX-format input to canonical Kind=Utc (AdjustToUniversal), so
            // the exact-format path already yields a UTC wall-clock. The Local-kind branch is a defensive
            // guard for the loose-locale fallback parse, which may still return Kind=Local; normalise it to
            // UTC so a full-datetime bound's wall-clock matches the canonically-UTC value it is compared
            // against, keeping the comparison host-timezone-independent.
            DateTime parsed = FixDateTime.Parse(text, CultureInfo.InvariantCulture);
            DateTime normalised = parsed.Kind == DateTimeKind.Local ? parsed.ToUniversalTime() : parsed;
            if (isMax)
            {
                MaxValue = normalised;
            }
            else
            {
                MinValue = normalised;
            }
        }
    }

    private static bool IsDateLess(string text)
    {
        if (text.Length < 8)
        {
            return true;
        }
        for (int i = 0; i < 8; i++)
        {
            if (!char.IsDigit(text[i]))
            {
                return true;
            }
        }
        return false;
    }

    #region AtdlReferenceType<string> Overrides

    /// <summary>
    /// Validates the supplied value in terms of the parameters constraints (e.g., MinValue, MaxValue, etc.).
    /// </summary>
    /// <param name="value">Value to validate, may be null in which case no validation is applied.</param>
    /// <param name="isRequired">Set to true to check that this parameter is non-null.</param>
    /// <returns>ValidationResult indicating whether the supplied value is valid.</returns>
    /// <remarks>DateTime.MaxValue (a date and time at the end of the year 9999) is used to indicate an invalid date or time.</remarks>
    protected override ValidationResult ValidateValue(DateTime? value, bool isRequired)
    {
        if (value != null)
        {
            if (value == DateTime.MaxValue)
            {
                return new ValidationResult(
                    ValidationResult.ResultType.Invalid,
                    ErrorMessages.InvalidDateOrTimeValueUnknown
                );
            }

            ValidationResult? boundViolation = CheckBounds((DateTime)value);
            if (boundViolation != null)
            {
                return boundViolation;
            }
        }
        else if (isRequired)
        {
            return new ValidationResult(
                ValidationResult.ResultType.Missing,
                ErrorMessages.NonOptionalParameterNotSupplied2
            );
        }

        return ValidationResult.ValidResult;
    }

    private ValidationResult? CheckBounds(DateTime value)
    {
        DateTime normalisedVal = NormaliseToUtc(value);

        return CheckDateTimeBounds(value, normalisedVal) ?? CheckTimeOfDayBounds(value, normalisedVal);
    }

    private ValidationResult? CheckDateTimeBounds(DateTime value, DateTime normalisedVal)
    {
        if (MaxValue != null && normalisedVal > NormaliseToUtc(MaxValue.Value))
        {
            return new ValidationResult(
                ValidationResult.ResultType.Invalid,
                ErrorMessages.MaxValueExceeded,
                value,
                MaxValue
            );
        }

        if (MinValue != null && normalisedVal < NormaliseToUtc(MinValue.Value))
        {
            return new ValidationResult(
                ValidationResult.ResultType.Invalid,
                ErrorMessages.MinValueNotMet,
                value,
                MinValue
            );
        }

        return null;
    }

    private ValidationResult? CheckTimeOfDayBounds(DateTime value, DateTime normalisedVal)
    {
        // An offset-anchored bound already resolved itself to a UTC time-of-day when parsed - compare it
        // against the value's own UTC time-of-day, not the zone-local one, or the two would be compared
        // in different frames (the "conflicting timezone annotation" case). Each of min/max is anchored
        // independently, so the two comparisons may legitimately use different frames on the same value.
        TimeOnly zoneTimeOfDay = GetTimeOfDayForBounds(normalisedVal);
        TimeOnly utcTimeOfDay = TimeOnly.FromDateTime(normalisedVal);

        if (_maxTimeOfDay != null)
        {
            TimeOnly valueTimeOfDay = _maxTimeOfDayIsOffsetAnchored ? utcTimeOfDay : zoneTimeOfDay;
            if (valueTimeOfDay > _maxTimeOfDay)
            {
                return new ValidationResult(
                    ValidationResult.ResultType.Invalid,
                    ErrorMessages.MaxValueExceeded,
                    value,
                    _maxTimeOfDay
                );
            }
        }

        if (_minTimeOfDay != null)
        {
            TimeOnly valueTimeOfDay = _minTimeOfDayIsOffsetAnchored ? utcTimeOfDay : zoneTimeOfDay;
            if (valueTimeOfDay < _minTimeOfDay)
            {
                return new ValidationResult(
                    ValidationResult.ResultType.Invalid,
                    ErrorMessages.MinValueNotMet,
                    value,
                    _minTimeOfDay
                );
            }
        }

        return null;
    }

    internal virtual TimeOnly GetTimeOfDayForBounds(DateTime utcValue)
    {
        return TimeOnly.FromDateTime(utcValue);
    }

    private static DateTime NormaliseToUtc(DateTime dt)
    {
        if (dt.Kind == DateTimeKind.Local)
        {
            return dt.ToUniversalTime();
        }
        if (dt.Kind == DateTimeKind.Unspecified)
        {
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        }
        return dt;
    }

    /// <summary>
    /// Converts the supplied value from string format (as might be used on the FIX wire) into the type of the type
    /// parameter for this type.
    /// </summary>
    /// <param name="value">Type to convert from string, cannot be null.</param>
    /// <returns>Value converted from a string if the conversion succeeded; otherwise an exception is thrown.</returns>
    protected override DateTime? ConvertFromWireValueFormat(string value)
    {
        // A '{NULL}' sentinel means "clear this field" — return null rather than throwing, matching
        // Boolean_t/String_t/Data_t (C4, {NULL}-handling theme). An empty string is not a clear (empty
        // FIX fields are invalid) and still falls through to throw.
        if (value is null or Atdl.NullValue)
        {
            return null;
        }

        string[] formats = GetDateTimeFormatStrings();

        if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, WireParseStyles, out DateTime result))
        {
            return result;
        }

        throw ThrowHelper.New<InvalidCastException>(this, ErrorMessages.InvalidDateOrTimeValue, value);
    }

    /// <summary>
    /// <see cref="DateTimeStyles"/> applied when parsing a wire value. The default preserves the
    /// parsed text as-is (whitespace tolerated). Timezone-bearing types override this to normalise
    /// an explicit offset to UTC so that round-trip output is canonical and host-offset-independent.
    /// Date-only / local types deliberately do NOT adjust, so they must not change this.
    /// </summary>
    protected virtual DateTimeStyles WireParseStyles => DateTimeStyles.AllowWhiteSpaces;

    /// <summary>
    /// Converts the supplied value to a string, as might be used on the FIX wire.
    /// </summary>
    /// <param name="value">Value to convert, may be null.</param>
    /// <returns>If input value is not null, returns value converted to a string; null otherwise.</returns>
    protected override string? ConvertToWireValueFormat(DateTime? value)
    {
        string format = GetDateTimeFormatStrings()[0];

        return value?.ToString(format, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converts the supplied value to the type parameter type (DateTime?) for this class.
    /// </summary>
    /// <param name="hostParameter"><see cref="IParameter"/> that hosts this value.</param>
    /// <param name="value">Value to convert, may be null.</param>
    /// <returns>If input value is not null, returns value converted to T?; null otherwise.</returns>
    /// <remarks>Used when setting a parameter value from a control (or anything else that
    /// implements <see cref="IParameterConvertible"/>).</remarks>
    protected override DateTime? ConvertToNativeType(IParameter hostParameter, IParameterConvertible value)
    {
        return value.ToDateTime(hostParameter, CultureInfo.InvariantCulture);
    }

    #endregion

    #region IControlConvertible Members

    /// <summary>
    /// Converts the value of this instance to an equivalent nullable boolean value.
    /// </summary>
    /// <returns>One of true, false or null which is equivalent to the value of this instance.</returns>
    public bool? ToBoolean()
    {
        throw ThrowHelper.New<InvalidCastException>(
            this,
            ErrorMessages.UnsupportedParameterValueConversion,
            _value,
            "Boolean"
        );
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent string value using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="provider">An <see cref="IFormatProvider"/> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>A string value equivalent to the value of this instance.  May be null.</returns>
    public string? ToString(IFormatProvider? provider)
    {
        DateTime? value = ConstValue ?? _value;

        return ConvertToWireValueFormat(value);
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent nullable decimal value using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>A nullable decimal equivalent to the value of this instance.</returns>
    public decimal? ToDecimal()
    {
        throw ThrowHelper.New<InvalidCastException>(
            this,
            ErrorMessages.UnsupportedParameterValueConversion,
            _value,
            "Decimal"
        );
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent nullable DateTime value using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>A nullable DateTime equivalent to the value of this instance.</returns>
    public DateTime? ToDateTime()
    {
        return ConstValue ?? _value;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent EnumState value.
    /// </summary>
    /// <returns>A valid EnumState, assuming the source value can be correctly converted.</returns>
    /// <remarks>This method converts the enum value to a string, looks up the EnumID from the supplied
    /// EnumPairCollection and then returns a new EnumState.  This method may be a little slow for
    /// very large enumerations.</remarks>
    public EnumState ToEnumState(EnumPairCollection enumPairs)
    {
        throw ThrowHelper.New<InvalidCastException>(
            this,
            ErrorMessages.UnsupportedParameterValueConversion,
            _value,
            "Enumerated Type"
        );
    }

    #endregion

    /// <summary>
    /// Gets the DateTime format strings to use when converting this date/time to a FIX string and vice versa.
    /// </summary>
    /// <returns>Format strings suitable when calling DateTime.ToString().  At least one format string will be
    /// returned.</returns>
    /// <remarks>When converting from DateTime to string, the first member of the returned array is used.  When
    /// converting from string to DateTime, every member of the array is supplied to
    /// <see cref="DateTime.TryParseExact(string, string[], IFormatProvider, DateTimeStyles, out DateTime)"/>,
    /// which tries them in turn — the formats are not dispatched by string length.</remarks>
    protected abstract string[] GetDateTimeFormatStrings();
}
