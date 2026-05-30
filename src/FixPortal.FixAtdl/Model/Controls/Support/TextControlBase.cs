// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Globalization;
using FixPortal.FixAtdl.Diagnostics;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Types.Support;
using FixPortal.FixAtdl.Resources;

namespace FixPortal.FixAtdl.Model.Controls.Support;

/// <summary>
/// Represents control elements within FIXatdl that can support textual information.  Applies to the following controls:
/// <list type="bullet">
/// <item><description><see cref="FixPortal.FixAtdl.Model.Controls.HiddenField_t"/></description></item>
/// <item><description><see cref="FixPortal.FixAtdl.Model.Controls.Label_t"/></description></item>
/// <item><description><see cref="FixPortal.FixAtdl.Model.Controls.TextField_t"/></description></item>
/// </list>
/// </summary>
public abstract class TextControlBase : InitializableControl<string>
{
    /// <summary>
    /// The state value for this control; null when the control has no value set.
    /// </summary>
    protected string? _value;

    /// <summary>
    /// Initializes a new instance of <see cref="TextControlBase"/> using the supplied ID.
    /// </summary>
    /// <param name="id">ID for this control.</param>
    protected TextControlBase(string id)
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
        _value = value;

        return true;
    }

    /// <summary>
    /// Loads this control with any supplied InitValue. If InitValue is not supplied, then control value will
    /// be set to default/empty value.
    /// </summary>
    protected override void LoadDefaultFromInitValue()
    {
        SetValue(InitValue);
    }

    #endregion

    #region Control_t Overrides

    /// <summary>
    /// Sets the value of this control; either via a string or using the FIXatdl '{NULL}' value.
    /// </summary>
    /// <param name="newValue">Valid string or null (meaning do not send this value over FIX).
    /// May also contain the FIXatdl '{NULL}' value as a string.</param>
    public override void SetValue(object newValue)
    {
        _value = newValue switch
        {
            string value => value == Atdl.NullValue ? null : value,
            null => null,
            _ => throw ThrowHelper.New<InternalErrorException>(this, InternalErrors.UnexpectedArgumentType,
                newValue.GetType().FullName!, "System.String")
        };
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

        _value = value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Gets the value of this control.  May be null.
    /// </summary>
    /// <returns>This control's value (as a string) or null (meaning do not send this value over FIX).</returns>
    public override object GetCurrentValue()
    {
        return _value!;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent nullable boolean value.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <returns>One of true, false or null which is equivalent to the value of this instance.</returns>
    public override bool? ToBoolean(IParameter targetParameter)
    {
        if (string.IsNullOrEmpty(_value))
        {
            return null;
        }


        if (bool.TryParse(_value, out bool result))
        {
            return result;
        }

        throw ThrowHelper.New<InvalidCastException>(this, ErrorMessages.InvalidBooleanValue, _value, bool.TrueString.ToLower(), bool.FalseString.ToLower());
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent nullable decimal value using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <param name="provider">An <see cref="IFormatProvider"/> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>A nullable decimal equivalent to the value of this instance.</returns>
    public override decimal? ToDecimal(IParameter targetParameter, IFormatProvider provider)
    {

        return TryConvertToDecimal(_value, out decimal result) ? result : null;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent 32-bit signed integer using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <param name="provider">An <see cref="IFormatProvider"/> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>A nullable 32-bit signed integer equivalent to the value of this instance.</returns>
    public override int? ToInt32(IParameter targetParameter, IFormatProvider provider)
    {

        return TryConvertToInt(_value, out int result) ? result : null;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent 32-bit unsigned integer using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <param name="provider">An <see cref="IFormatProvider"/> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>A nullable 32-bit unsigned integer equivalent to the value of this instance.</returns>
    public override uint? ToUInt32(IParameter targetParameter, IFormatProvider provider)
    {

        return TryConvertToUint(_value, out uint result) ? result : null;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent char value.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <returns>A nullable char value equivalent to the value of this instance.  May be null.</returns>
    public override char? ToChar(IParameter targetParameter)
    {
        return TryConvertToChar(_value, out var result) ? result : null;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent string value using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <returns>A string value equivalent to the value of this instance.  May be null.</returns>
    public override string ToString(IParameter targetParameter)
    {
        return _value!;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent nullable DateTime value using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <param name="provider">An <see cref="IFormatProvider"/> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>A nullable DateTime equivalent to the value of this instance.</returns>
    public override DateTime? ToDateTime(IParameter targetParameter, IFormatProvider provider)
    {
        if (string.IsNullOrEmpty(_value))
        {
            return null;
        }


        if (!FixDateTime.TryParse(_value, provider, out DateTime result))
        {
            throw ThrowHelper.New<InvalidCastException>(this, ErrorMessages.InvalidDateOrTimeValue, _value);
        }

        return result;
    }

    /// <summary>
    /// Indicates whether the control has enumerated state (i.e., its state is held internally in an <see cref="EnumState"/> which
    /// requires special conversion, or if instead a regular value conversion is appropriate).
    /// </summary>
    public override bool HasEnumeratedState => false;

    #endregion
}
