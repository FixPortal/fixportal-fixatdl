// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Globalization;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Controls.Support;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Types.Support;
using FixPortal.FixAtdl.Resources;
using FixPortal.FixAtdl.Validation;
using ThrowHelper = FixPortal.FixAtdl.Diagnostics.ThrowHelper;

namespace FixPortal.FixAtdl.Model.Types;

/// <summary>
/// Represents a sequence of digits with optional decimal point and sign character (ASCII characters "-", "0" - "9" and "."); the
/// absence of the decimal point within the string will be interpreted as the float representation of an integer value.
/// All float fields must accommodate up to fifteen significant digits. The number of decimal places used should be a
/// factor of business/market needs and mutual agreement between counterparties. Note that float values may contain
/// leading zeros (e.g. "00023.23" = "23.23") and may contain or omit trailing zeros after the decimal point
/// (e.g. "23.0" = "23.0000" = "23" = "23.").
/// Note that fields which are derived from float may contain negative values unless explicitly specified otherwise.'
/// </summary>
public class Float_t : AtdlValueType<decimal>, IControlConvertible
{
    /// <summary>
    /// Gets/sets the maximum value for this parameter.<br/>
    /// Maximum value of the parameter accepted by the algorithm provider.
    /// </summary>
    /// <value>The maximum value.</value>
    public decimal? MaxValue { get; set; }

    /// <summary>
    /// Gets/sets the minimum value for this parameter.<br/>
    /// Minimum value of the parameter accepted by the algorithm provider.
    /// </summary>
    /// <value>The minimum value.</value>
    public decimal? MinValue { get; set; }

    /// <summary>
    /// Gets/sets the precision of this value, taken as the number of digits to the right of the decimal point in
    /// which to round when populating the FIX message. Lack of this attribute indicates that the value entered by
    /// the user should be taken as-is without rounding.
    /// </summary>
    public int? Precision
    {
        get;
        set
        {
            if (value is < 0 or > 28)
            {
                throw ThrowHelper.New<Diagnostics.Exceptions.InvalidFieldValueException>(
                    this,
                    "Precision must be between 0 and 28."
                );
            }

            // A document can set constValue before precision. Re-check the stored constant
            // against the precision that will actually be emitted, and leave both unchanged
            // when the rounded constant falls outside MinValue or MaxValue.
            int? previous = field;
            field = value;
            if (ConstValue is not decimal constant)
            {
                return;
            }

            ValidationResult validity = ValidateValue(constant, isRequired: false);
            if (validity.IsValid)
            {
                return;
            }

            field = previous;
            throw ThrowHelper.New<Diagnostics.Exceptions.InvalidFieldValueException>(
                this,
                validity.ErrorText ?? "Rounded value falls outside the validated bounds."
            );
        }
    }

    #region AtdlValueType<T> Overrides

    /// <summary>
    /// Validates the supplied value in terms of the parameters constraints (e.g., MinValue, MaxValue, etc.).
    /// </summary>
    /// <param name="value">Value to validate, may be null in which case no validation is applied.</param>
    /// <param name="isRequired">Set to true to check that this parameter is non-null.</param>
    /// <returns>ValidationResult indicating whether the supplied value is valid.</returns>
    protected override ValidationResult ValidateValue(decimal? value, bool isRequired)
    {
        if (value != null)
        {
            // Compare the value as it will be emitted. 0.16 at precision 1 becomes 0.2 and must
            // fail MaxValue 0.16 on the way in. Precision null keeps the raw comparison.
            decimal comparable = (decimal)value;
            if (Precision != null)
            {
                comparable = Round(comparable, Precision.Value)!.Value;
            }

            if (MaxValue != null && comparable > MaxValue)
            {
                return new ValidationResult(
                    ValidationResult.ResultType.Invalid,
                    ErrorMessages.MaxValueExceeded,
                    comparable,
                    MaxValue
                );
            }

            if (MinValue != null && comparable < MinValue)
            {
                return new ValidationResult(
                    ValidationResult.ResultType.Invalid,
                    ErrorMessages.MinValueNotMet,
                    comparable,
                    MinValue
                );
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

    /// <summary>
    /// Rejects a constant whose emitted (rounded) value falls outside <see cref="MinValue"/> or
    /// <see cref="MaxValue"/>. When <see cref="Precision"/> is null the constant is stored unchanged.
    /// A rejected constant is not stored, so a later wire assignment is not reported as an attempt
    /// to change a constant.
    /// </summary>
    /// <param name="value">Constant supplied by the caller, may be null.</param>
    /// <returns>The constant to store.</returns>
    protected override decimal? NormalizeAssignedConstValue(decimal? value)
    {
        if (value is null || Precision is null)
        {
            return value;
        }

        ValidationResult validity = ValidateValue(value, isRequired: false);
        if (validity.IsValid)
        {
            return value;
        }

        throw ThrowHelper.New<Diagnostics.Exceptions.InvalidFieldValueException>(
            this,
            validity.ErrorText ?? "Rounded value falls outside the validated bounds."
        );
    }

    /// <summary>
    /// Converts the supplied value from string format (as might be used on the FIX wire) into the type of the type
    /// parameter for this type.
    /// </summary>
    /// <param name="value">Type to convert from string; cannot be null as empty fields are invalid in FIX.</param>
    /// <returns>Value converted from a string.</returns>
    protected override decimal? ConvertFromWireValueFormat(string value)
    {
        // AtdlValueType.SetWireValue maps the '{NULL}' sentinel to null before this method runs; a
        // direct C# null means the same "clear this field". An empty string is not a clear (empty
        // FIX fields are invalid) and still falls through to throw.
        if (value is null)
        {
            return null;
        }

        // Explicit styles: the FIX float alphabet is '-', '0'-'9' and '.', so a thousands-separated
        // spelling ("1,000.5"), an exponent spelling ("1E2") and a whitespace-padded one (" 1") must
        // all fail rather than parse as 1000.5, 100 or 1 (R21; exponent/whitespace excluded on review).
        return decimal.Parse(
            value,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture
        );
    }

    /// <summary>
    /// Converts the supplied value to a string, as might be used on the FIX wire.  If the supplied value is
    /// null, this means the field is not to be included in the outgoing FIX message.
    /// </summary>
    /// <param name="value">Value to convert, may be null.</param>
    /// <returns>If input value is not null, returns value converted to a string; null otherwise.</returns>
    protected override string? ConvertToWireValueFormat(decimal? value)
    {
        if (value == null)
        {
            return null;
        }

        if (Precision == null)
        {
            return ((decimal)value).ToString(CultureInfo.InvariantCulture);
        }

        decimal rounded = Round(value, Precision.Value)!.Value;

        // Emission rounds again. A bound or Precision change made after the value was stored
        // is checked here.
        ValidationResult validity = ValidateValue(rounded, isRequired: false);
        if (!validity.IsValid)
        {
            throw ThrowHelper.New<Diagnostics.Exceptions.InvalidFieldValueException>(
                this,
                "Rounded value {0} at precision {1} falls outside the validated bounds ({2}).",
                rounded,
                Precision.Value,
                validity.ErrorText
            );
        }

        return rounded.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converts the supplied value to the type parameter type (T?) for this class.
    /// </summary>
    /// <param name="hostParameter">Parameter that this value belongs to.</param>
    /// <param name="value">Value to convert, may be null.</param>
    /// <returns>If input value is not null, returns value converted to T?; null otherwise.</returns>
    /// <remarks>Used when setting a parameter value from a control (or anything else that
    /// implements <see cref="IParameterConvertible"/>).</remarks>
    protected override decimal? ConvertToNativeType(IParameter hostParameter, IParameterConvertible value)
    {
        return value.ToDecimal(hostParameter, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Gets the value of this parameter type in its native (i.e., raw) form, such as int, char, string, etc.
    /// </summary>
    /// <param name="applyWireValueFormat">If set to true, the value returned is adjusted to be in the 'format'
    /// it would be if sent on the FIX wire.  For example, for Float_t parameters, setting this value to true
    /// would cause the Precision attribute setting to be applied.</param>
    /// <returns>Native parameter value.</returns>
    public override object GetNativeValue(bool applyWireValueFormat)
    {
        decimal? value = ConstValue != null ? ConstValue : _value;

        if (value != null && applyWireValueFormat && Precision != null)
        {
            return Round(value, (int)Precision)!;
        }

        return value!;
    }

    /// <summary>
    /// Gets the human-readable type name for use in error messages shown to the user.
    /// </summary>
    /// <returns>Human-readable type name.</returns>
    protected override string GetHumanReadableTypeName()
    {
        return HumanReadableTypeNames.NumericType;
    }

    #endregion

    /// <summary>
    /// Rounds the supplied value to the specified number of decimal places.
    /// </summary>
    /// <param name="value">Value to be rounded. May be null.</param>
    /// <param name="precision">Number of places to round to.</param>
    /// <returns>If the supplied value is non-null, the rounded value is returned; otherwise returns null.</returns>
    /// <remarks>FIXatdl does not mandate a rounding mode for the <c>precision</c> attribute. This library
    /// uses <see cref="MidpointRounding.AwayFromZero"/> by convention (e.g. 2.5 → 3, -2.5 → -3); trailing
    /// zeros are not padded, which is wire-legal for float fields (batch 5, M4).</remarks>
    protected static decimal? Round(decimal? value, int precision)
    {
        return value != null ? Math.Round((decimal)value, precision, MidpointRounding.AwayFromZero) : null;
    }

    #region IControlConvertible Members

    /// <summary>
    /// Converts the value of this instance to an equivalent nullable boolean value.
    /// </summary>
    /// <returns>One of true, false or null which is equivalent to the value of this instance.</returns>
    public bool? ToBoolean()
    {
        throw UnsupportedConversion(_value, "Boolean");
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent string value using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="provider">An <see cref="IFormatProvider"/> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>A string value equivalent to the value of this instance.  May be null.</returns>
    public virtual string? ToString(IFormatProvider? provider)
    {
        decimal? value = ConstValue ?? _value;

        return value?.ToString(provider);
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent nullable decimal value using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>A nullable decimal equivalent to the value of this instance.</returns>
    public virtual decimal? ToDecimal()
    {
        return ConstValue ?? _value;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent nullable DateTime value using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>A nullable DateTime equivalent to the value of this instance.</returns>
    public DateTime? ToDateTime()
    {
        throw UnsupportedConversion(_value, "DateTime");
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent EnumState value.
    /// </summary>
    /// <returns>A valid EnumState, assuming the source value can be correctly converted.</returns>
    public EnumState ToEnumState(EnumPairCollection enumPairs)
    {
        throw UnsupportedConversion(_value, "EnumState");
    }

    #endregion
}
