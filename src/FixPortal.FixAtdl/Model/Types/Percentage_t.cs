// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Globalization;
using FixPortal.FixAtdl.Model.Controls.Support;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Resources;
using FixPortal.FixAtdl.Validation;

namespace FixPortal.FixAtdl.Model.Types;

/// <summary>
/// 'float field representing a percentage (e.g. 0.05 represents 5% and 0.9525 represents 95.25%). Note the number of 
/// decimal places may vary.'
/// </summary>
public class Percentage_t : Float_t
{
    /// <summary>
    /// Applicable for xsi:type of Percentage_t. If true then percent values must be multiplied by 100 before being
    /// sent on the wire. For example, if multiplyBy100 were false then the percentage, 75%, would be sent as 0.75 
    /// on the wire. However, if multiplyBy100 were true then 75 would be sent on the wire.
    /// If not provided it should be interpreted as false.
    /// Use of this attribute is not recommended. The motivation for this attribute is to maximize compatibility 
    /// with algorithmic interfaces that are non-compliant with FIX in regard to their handling of percentages. In
    /// these cases an integer parameter should be used instead of a percentage.
    /// </summary>
    public bool? MultiplyBy100 { get; set; }

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
            // The bound comparison is against the native fraction (0.75 == 75%); the error message,
            // however, is shown in the user-facing whole-percent units the control uses, so always
            // scale by 100 regardless of MultiplyBy100 (which governs only the wire representation).
            if (MaxValue != null && (decimal)value > MaxValue)
            {
                return new ValidationResult(ValidationResult.ResultType.Invalid, ErrorMessages.MaxValueExceeded,
                    RemoveTrailingZeroes(value * 100)!, RemoveTrailingZeroes(MaxValue.Value * 100)!);
            }

            if (MinValue != null && (decimal)value < MinValue)
            {
                return new ValidationResult(ValidationResult.ResultType.Invalid, ErrorMessages.MinValueExceeded,
                    RemoveTrailingZeroes(value * 100)!, RemoveTrailingZeroes(MinValue.Value * 100)!);
            }
        }
        else if (isRequired)
        {
            return new ValidationResult(ValidationResult.ResultType.Missing, ErrorMessages.NonOptionalParameterNotSupplied2);
        }

        return ValidationResult.ValidResult;
    }

    /// <summary>
    /// Converts the supplied value from string format (as might be used on the FIX wire) into the type of the type
    /// parameter for this type.  This implementation adjusts for the fact that percentage values are typically
    /// shown as whole numbers (5, 10, 15) on the user interface but sent over the FIX wire as decimals (0.05, 0.1, 0.15).
    /// </summary>
    /// <param name="value">Type to convert from string; cannot be null as empty fields are invalid in FIX.</param>
    /// <returns>Value converted from a string.</returns>
    protected override decimal? ConvertFromWireValueFormat(string value)
    {
        decimal? decimalValue = base.ConvertFromWireValueFormat(value);

        return MultiplyBy100 == true ? (decimal)decimalValue! / 100 : (decimal)decimalValue!;
    }

    /// <summary>
    /// Converts the supplied value to a string, as might be used on the FIX wire.  If the supplied value is
    /// null, this means the field is not to be included in the outgoing FIX message.  This implementation adjusts for the 
    /// fact that percentage values are typically shown as whole numbers (5, 10, 15) on the user interface but sent over 
    /// the FIX wire as decimals (0.05, 0.1, 0.15).
    /// </summary>
    /// <param name="value">Value to convert, may be null.</param>
    /// <returns>If input value is not null, returns value converted to a string; null otherwise.</returns>
    protected override string ConvertToWireValueFormat(decimal? value)
    {
        if (value == null)
        {
            return null!;
        }

        decimal adjustedValue = MultiplyBy100 == true ? (decimal)RemoveTrailingZeroes(value * 100)! : (decimal)value;

        if (Precision == null)
        {
            return adjustedValue.ToString(CultureInfo.InvariantCulture);
        }

        return Round(adjustedValue, Precision.Value)!.Value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converts the supplied value to the type parameter type (T?) for this class.
    /// </summary>
    /// <param name="hostParameter">Parameter that this value belongs to.</param>
    /// <param name="value">Value to convert, may be null.</param>
    /// <returns>If input value is not null, returns value converted to T?; null otherwise.</returns>
    /// <remarks>Used when setting a parameter value from a control (or anything else that
    /// implements <see cref="IParameterConvertible"/>).<br/><br/>
    /// Unlike all other (non-enumerated) control/parameter relationships, Percentage_t does not have a 
    /// one-to-one mapping with its associated control value as the control will typically contain a user-oriented 
    /// format (e.g., 25) when the parameter must contain the true value (i.e., 0.25, assuming multiplyBy100 
    /// is not set to true).</remarks>
    protected override decimal? ConvertToNativeType(IParameter hostParameter, IParameterConvertible value)
    {
        decimal? convertedValue = value.ToDecimal(hostParameter, CultureInfo.InvariantCulture);

        return convertedValue != null ? convertedValue / 100 : null;
    }

    /// <summary>
    /// Gets the value of this parameter type in its native (i.e., raw) form, such as int, char, string, etc. 
    /// </summary>
    /// <param name="applyWireValueFormat">If set to true, the value returned is adjusted to be in the 'format'
    /// it would be if sent on the FIX wire.  In this case, we have to apply both Precision and the MultiplyBy100
    /// flag.</param>
    /// <returns>Native parameter value.</returns>
    public override object GetNativeValue(bool applyWireValueFormat)
    {
        decimal? value = ConstValue switch
        {
            null => _value,
            _ => MultiplyBy100 == true ? ConstValue / 100 : ConstValue,
        };

        if (value != null && applyWireValueFormat)
        {
            decimal adjustedValue = MultiplyBy100 == true ? (decimal)RemoveTrailingZeroes(value * 100)! : (decimal)value;

            return Precision != null
                ? Math.Round(adjustedValue, Precision.Value, MidpointRounding.AwayFromZero)
                : adjustedValue;
        }

        return value!;
    }

    #endregion

    #region IControlConvertible Members

    /// <summary>
    /// Converts the value of this instance to an equivalent nullable decimal value using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>A nullable decimal equivalent to the value of this instance.</returns>
    public override decimal? ToDecimal()
    {
        decimal? value = ConstValue ?? _value;

        // The control always works in whole-percent units (75 for 75%); MultiplyBy100 affects only
        // the wire representation, not the control. The native value is always the fraction (0.75),
        // so scale up by 100 in both cases. (Previously the MultiplyBy100==true branch returned the
        // raw fraction, so a load/edit/save cycle shrank the displayed value 100x.)
        return value != null ? RemoveTrailingZeroes(value * 100) : null;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent string value using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="provider">An <see cref="IFormatProvider"/> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>A string value equivalent to the value of this instance.  May be null.</returns>
    public override string? ToString(IFormatProvider? provider)
    {
        decimal? value = ToDecimal();

        return value != null ? ((decimal)value).ToString(provider) : null;
    }

    #endregion

    private static decimal? RemoveTrailingZeroes(decimal? value)
    {
        if (value == null)
        {
            return null;
        }

        // We use this slightly ugly manipulation to remove the trailing zeroes that multiplication by 100 produces
        return decimal.Parse(((decimal)value).ToString("G29", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }
}
