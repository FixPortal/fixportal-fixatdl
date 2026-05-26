// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;
using System.Globalization;
using System.Linq;
using FixPortal.FixAtdl.Diagnostics;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Types.Support;
using FixPortal.FixAtdl.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FixPortal.FixAtdl.Model.Controls.Support;

/// <summary>
/// Represents control elements within FIXatdl that can optionally contain numeric values.
/// </summary>
/// <remarks>Note that decimal.MaxValue is used to represent an invalid value.</remarks>
public class NumericControlBase : InitializableControl<decimal?>
{
    // FP Enhancement: 2026-05-23 — TODO wire injected logger when refactoring class to accept ILogger.
    private static readonly ILogger _log = NullLogger.Instance;

    private const decimal InvalidValue = decimal.MaxValue;

    /// <summary>
    /// The state value for this control.
    /// </summary>
    protected decimal? _value;

    /// <summary>
    /// Initializes a new instance of <see cref="NumericControlBase"/> using the supplied ID.
    /// </summary>
    /// <param name="id">ID for this control.</param>
    protected NumericControlBase(string id)
        : base(id)
    {
    }

    #region InitializableControl<T> Overrides

    /// <summary>
    /// Attempts to load the supplied FIX field value into this control.
    /// </summary>
    /// <param name="value">Value to set this control to.</param>
    /// <returns>true if it was possible to set the value of this control using the supplied value; false otherwise.</returns>
    protected override bool LoadDefaultFromFixValue(string value)
    {
        try
        {
            _value = Convert.ToDecimal(value, CultureInfo.InvariantCulture);

            return true;
        }
        catch (FormatException)
        {
            _log.LogError("Unable to set control {Arg0} to value '{Arg1}' as the value could not be converted to a valid number", Id, value);

            return false;
        }
    }

    /// <summary>
    /// Loads this control with any supplied InitValue. If InitValue is not supplied, then control value will
    /// be set to default/empty value.
    /// </summary>
    protected override void LoadDefaultFromInitValue()
    {
        _value = InitValue;
    }

    #endregion

    #region Control_t Overrides

    /// <summary>
    /// Gets the value of this control.  May be null.
    /// </summary>
    /// <returns>Either a valid decimal value or null (meaning do not send this value over FIX).</returns>
    public override object GetCurrentValue()
    {
        return (_value != InvalidValue ? _value : null)!;
    }

    /// <summary>
    /// Sets the value of this control; either via a boolean, or using the FIXatdl '{NULL}' value.
    /// </summary>
    /// <param name="newValue">One of three bool? states, true, false or null (meaning do not send this value over FIX).
    /// May also contain the FIXatdl '{NULL}' value as a string, or either of the CheckedEnumRef or
    /// UncheckedEnumRef string values.</param>
    public override void SetValue(object newValue)
    {
        bool isString = newValue is string;
        bool isDecimal = newValue is decimal;

        if (isString)
        {
            string? value = newValue as string;

            _value = value == Atdl.NullValue
                ? null
                : throw ThrowHelper.New<InvalidFieldValueException>(this, ErrorMessages.InitControlValueError,
                    Id, string.Format(CultureInfo.InvariantCulture, "'{0}' is not a valid value for this control", value));
        }
        else
        {
            _value = isDecimal
                ? (decimal?)newValue
                : newValue == null
            ? null
            : throw ThrowHelper.New<InternalErrorException>(this, InternalErrors.UnexpectedArgumentType,
                newValue.GetType().FullName, "System.String, System.Decimal");
        }

        _log.LogDebug("Control value is now {Value}", _value != null ? _value.Value.ToString(CultureInfo.InvariantCulture) : "null");
    }

    /// <summary>
    /// Resets this control to either a null value or for list controls, all options unselected.
    /// </summary>
    public override void Reset()
    {
        _value = null;
    }

    /// <summary>
    /// Sets the value of this control using the value of the supplied parameter.
    /// </summary>
    /// <param name="parameter">Parameter to set this control's value from.</param>
    public override void SetValueFromParameter(IParameter parameter)
    {
        IControlConvertible value = parameter.GetValueForControl();

        _value = value.ToDecimal();

        _log.LogDebug("Numeric control {Arg0} value is now {Arg1}", Id, _value);
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent nullable boolean value.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <returns>One of true, false or null which is equivalent to the value of this instance.</returns>
    public override bool? ToBoolean(IParameter targetParameter)
    {
        throw ThrowHelper.New<InvalidCastException>(this, ErrorMessages.UnsupportedControlValueConversion, _value, "Boolean", Id);
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent nullable decimal value using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <param name="provider">An <see cref="IFormatProvider"/> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>A nullable decimal equivalent to the value of this instance.</returns>
    public override decimal? ToDecimal(IParameter targetParameter, IFormatProvider provider)
    {
        return _value;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent 32-bit signed integer using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <param name="provider">An <see cref="IFormatProvider"/> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>A nullable 32-bit signed integer equivalent to the value of this instance.</returns>
    public override int? ToInt32(IParameter targetParameter, IFormatProvider provider)
    {
        return _value != null ? (int?)decimal.ToInt32((decimal)_value) : null;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent 32-bit unsigned integer using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <param name="provider">An <see cref="IFormatProvider"/> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>A nullable 32-bit unsigned integer equivalent to the value of this instance.</returns>
    public override uint? ToUInt32(IParameter targetParameter, IFormatProvider provider)
    {
        return _value != null ? (uint?)decimal.ToUInt32((decimal)_value) : null;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent char value.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <returns>A nullable char value equivalent to the value of this instance.  May be null.</returns>
    public override char? ToChar(IParameter targetParameter)
    {
        throw ThrowHelper.New<InvalidCastException>(this, ErrorMessages.UnsupportedControlValueConversion, _value, "Char", Id);
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent string value using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <returns>A string value equivalent to the value of this instance.  May be null.</returns>
    public override string ToString(IParameter targetParameter)
    {
        return _value != null ? ((decimal)_value).ToString(CultureInfo.InvariantCulture) : null!;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent nullable DateTime value using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <param name="provider">An <see cref="IFormatProvider"/> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>A nullable DateTime equivalent to the value of this instance.</returns>
    public override DateTime? ToDateTime(IParameter targetParameter, IFormatProvider provider)
    {
        throw ThrowHelper.New<InvalidCastException>(this, ErrorMessages.UnsupportedControlValueConversion, _value, "DateTime", Id);
    }

    /// <summary>
    /// Indicates whether the control has enumerated state (i.e., its state is held internally in an <see cref="EnumState"/> which
    /// requires special conversion, or if instead a regular value conversion is appropriate).
    /// </summary>
    public override bool HasEnumeratedState => false;

    #endregion
}
