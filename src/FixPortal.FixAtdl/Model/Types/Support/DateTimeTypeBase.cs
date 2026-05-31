// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Globalization;
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
public abstract class DateTimeTypeBase : AtdlValueType<DateTime>, IControlConvertible
{
    /// <summary>
    /// Maximum value for this date/time type, i.e., the latest acceptable date/time.
    /// </summary>
    public DateTime? MaxValue { get; set; }

    /// <summary>
    /// Minimum value for this date/time type, i.e., the earliest acceptable date/time.
    /// </summary>
    public DateTime? MinValue { get; set; }

    // C2 — time-only bound capture. A maxValue/minValue written as a bare time-of-day (HH:mm:ss[.fff])
    // is a time-of-day constraint, not a date+time one. The reflective parser routes the raw bound text
    // through MaxValueText/MinValueText; a time-only value is stored here (and compared on the time
    // component only), while a full datetime / date-only value continues to populate MaxValue/MinValue.
    private TimeOnly? _maxTimeOfDay;
    private TimeOnly? _minTimeOfDay;

    private static readonly string[] _timeOnlyBoundFormats = ["HH:mm:ss.fff", "HH:mm:ss"];

    /// <summary>Deserialization-only round-trip of the raw <c>maxValue</c> attribute text; parsed with
    /// time-only awareness on set (C2). The getter returns the last raw text set (or null). Not intended
    /// for programmatic use; set <see cref="MaxValue"/> directly for a full date+time bound.</summary>
    public string? MaxValueText
    {
        get;
        set { field = value; SetBound(value!, isMax: true); }
    }

    /// <summary>Deserialization-only round-trip of the raw <c>minValue</c> attribute text; parsed with
    /// time-only awareness on set (C2). The getter returns the last raw text set (or null). Not intended
    /// for programmatic use; set <see cref="MinValue"/> directly for a full date+time bound.</summary>
    public string? MinValueText
    {
        get;
        set { field = value; SetBound(value!, isMax: false); }
    }

    private void SetBound(string text, bool isMax)
    {
        if (TimeOnly.TryParseExact(text, _timeOnlyBoundFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly timeOfDay))
        {
            if (isMax) { _maxTimeOfDay = timeOfDay; } else { _minTimeOfDay = timeOfDay; }
        }
        else
        {
            // FixDateTime.Parse uses AssumeUniversal (no AdjustToUniversal), so on a non-UTC host the
            // moment is correct but expressed with Kind=Local — its wall-clock is host-offset-shifted.
            // Normalise to UTC so a full-datetime bound's wall-clock matches the canonically-UTC value
            // it is compared against; otherwise the comparison is host-timezone-dependent.
            DateTime parsed = FixDateTime.Parse(text, CultureInfo.InvariantCulture);
            DateTime normalised = parsed.Kind == DateTimeKind.Local ? parsed.ToUniversalTime() : parsed;
            if (isMax) { MaxValue = normalised; } else { MinValue = normalised; }
        }
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
                return new ValidationResult(ValidationResult.ResultType.Invalid, ErrorMessages.InvalidDateOrTimeValueUnknown);
            }

            ValidationResult? boundViolation = CheckBounds((DateTime)value);
            if (boundViolation != null)
            {
                return boundViolation;
            }
        }
        else if (isRequired)
        {
            return new ValidationResult(ValidationResult.ResultType.Missing, ErrorMessages.NonOptionalParameterNotSupplied2);
        }

        return ValidationResult.ValidResult;
    }

    private ValidationResult? CheckBounds(DateTime value)
    {
        if (MaxValue != null && value > MaxValue)
        {
            return new ValidationResult(ValidationResult.ResultType.Invalid, ErrorMessages.MaxValueExceeded, value, MaxValue);
        }

        if (MinValue != null && value < MinValue)
        {
            return new ValidationResult(ValidationResult.ResultType.Invalid, ErrorMessages.MinValueExceeded, value, MinValue);
        }

        TimeOnly valueTimeOfDay = TimeOnly.FromDateTime(value);

        if (_maxTimeOfDay != null && valueTimeOfDay > _maxTimeOfDay)
        {
            return new ValidationResult(ValidationResult.ResultType.Invalid, ErrorMessages.MaxValueExceeded, value, _maxTimeOfDay);
        }

        if (_minTimeOfDay != null && valueTimeOfDay < _minTimeOfDay)
        {
            return new ValidationResult(ValidationResult.ResultType.Invalid, ErrorMessages.MinValueExceeded, value, _minTimeOfDay);
        }

        return null;
    }

    /// <summary>
    /// Converts the supplied value from string format (as might be used on the FIX wire) into the type of the type
    /// parameter for this type.  
    /// </summary>
    /// <param name="value">Type to convert from string, cannot be null.</param>
    /// <returns>Value converted from a string if the conversion succeeded; otherwise an exception is thrown.</returns>
    protected override DateTime? ConvertFromWireValueFormat(string value)
    {
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
    protected override string ConvertToWireValueFormat(DateTime? value)
    {
        string format = GetDateTimeFormatStrings()[0];

        return value != null ? ((DateTime)value).ToString(format, CultureInfo.InvariantCulture) : null!;
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
        throw ThrowHelper.New<InvalidCastException>(this, ErrorMessages.UnsupportedParameterValueConversion, _value, "Boolean");
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent string value using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="provider">An <see cref="IFormatProvider"/> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>A string value equivalent to the value of this instance.  May be null.</returns>
    public string? ToString(IFormatProvider? provider)
    {
        DateTime? value = ConstValue ?? _value;

        return value != null ? ((DateTime)value).ToString(GetDateTimeFormatStrings()[0], CultureInfo.InvariantCulture) : null;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent nullable decimal value using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>A nullable decimal equivalent to the value of this instance.</returns>
    public decimal? ToDecimal()
    {
        throw ThrowHelper.New<InvalidCastException>(this, ErrorMessages.UnsupportedParameterValueConversion, _value, "Decimal");
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
        throw ThrowHelper.New<InvalidCastException>(this, ErrorMessages.UnsupportedParameterValueConversion, _value, "Enumerated Type");
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
