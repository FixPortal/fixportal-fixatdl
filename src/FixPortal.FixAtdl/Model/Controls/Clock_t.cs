// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;
using System.Globalization;
using FixPortal.FixAtdl.Diagnostics;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Controls.Support;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Types.Support;
using FixPortal.FixAtdl.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FixPortal.FixAtdl.Model.Controls;

/// <summary>
/// Represents the Clock_t control element within FIXatdl.
/// </summary>
public class Clock_t : InitializableControl<DateTime?>
{
    // FP Enhancement: 2026-05-23 — TODO wire injected logger when refactoring class to accept ILogger.
    private static readonly ILogger _log = NullLogger.Instance;

    private DateTime? _value;

    /// <summary>
    /// Initializes a new instance of <see cref="Clock_t"/> using the supplied ID.
    /// </summary>
    /// <param name="id">ID for this control.</param>
    public Clock_t(string id)
        : base(id)
    {
        _log.LogDebug("New Clock_t created as control {Arg0}", id);
    }

    // TODO: Implement LocalMktTz as a type.
    /// <summary>The timezone in which initValue is represented in.  Required when initValue is supplied. Applicable when 
    /// xsi:type is Clock_t.</summary>
    public string LocalMktTz { get; set; } = null!;

    /// <summary>Defines the treatment of initValue time. 0: use initValue; 1: use current time if initValue time has passed.
    /// The default value is 0.</summary>
    public int? InitValueMode { get; set; }

    #region InitializableControl<T> Overrides

    /// <summary>
    /// Attempts to load the supplied FIX field value into this control.
    /// </summary>
    /// <param name="value">Value to set this control to.</param>
    /// <returns>true if it was possible to set the value of this control using the supplied value; false otherwise.</returns>
    protected override bool LoadDefaultFromFixValue(string value)
    {

        bool parsed = FixDateTime.TryParse(value, CultureInfo.InvariantCulture, out DateTime result);

        _value = parsed ? (DateTime?)result : null;

        return parsed;
    }

    /// <summary>
    /// Loads this control with any supplied InitValue. If InitValue is not supplied, then control value will
    /// be set to default/empty value.
    /// </summary>
    protected override void LoadDefaultFromInitValue()
    {
        _value = InitValue != null ? InitValueMode == 1 ? DateTime.Now > InitValue ? DateTime.Now : InitValue : InitValue : null;
    }

    #endregion

    #region Control_t Overrides

    /// <summary>
    /// Sets the value of this control using the value of the supplied parameter.
    /// </summary>
    /// <param name="parameter">Parameter to set this control's value from.</param>
    public override void SetValueFromParameter(IParameter parameter)
    {
        IControlConvertible value = parameter.GetValueForControl();

        _value = value.ToDateTime();

        _log.LogDebug("Clock_t control value is now {Value}", _value);
    }

    /// <summary>
    /// Sets the value of this control; either via a DateTime, or using the FIXatdl '{NULL}' value.  This method
    /// is either called indirectly from the user interface, or by a StateRule.
    /// </summary>
    /// <param name="newValue">Either a valid DateTime or null (meaning do not send this value over FIX).
    /// May also contain the FIXatdl '{NULL}' value as a string.</param>
    public override void SetValue(object newValue)
    {
        bool isString = newValue is string;
        bool isDateTime = newValue is DateTime;

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
            _value = isDateTime || newValue == null
                ? (DateTime?)newValue
                : throw ThrowHelper.New<InternalErrorException>(this, InternalErrors.UnexpectedArgumentType,
                newValue.GetType().FullName, "System.String, System.DateTime");
        }
    }

    /// <summary>
    /// Resets this control to either a null value or for list controls, all options unselected.
    /// </summary>
    public override void Reset()
    {
        _value = null;
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
        throw ThrowHelper.New<InvalidCastException>(this, ErrorMessages.UnsupportedControlValueConversion, _value, "Decimal", Id);
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent 32-bit signed integer using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <param name="provider">An <see cref="IFormatProvider"/> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>A nullable 32-bit signed integer equivalent to the value of this instance.</returns>
    public override int? ToInt32(IParameter targetParameter, IFormatProvider provider)
    {
        throw ThrowHelper.New<InvalidCastException>(this, ErrorMessages.UnsupportedControlValueConversion, _value, "Int32", Id);
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent 32-bit unsigned integer using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <param name="provider">An <see cref="IFormatProvider"/> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>A nullable 32-bit unsigned integer equivalent to the value of this instance.</returns>
    public override uint? ToUInt32(IParameter targetParameter, IFormatProvider provider)
    {
        throw ThrowHelper.New<InvalidCastException>(this, ErrorMessages.UnsupportedControlValueConversion, _value, "UInt32", Id);
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
    /// <returns>A string value equivalent to the value of this instance in the format YYYYMMDD-HH:MM:SS.  May be null.</returns>
    public override string ToString(IParameter targetParameter)
    {
        return _value != null ? ((DateTime)_value).ToString(FixDateTimeFormat.FixDateTime, CultureInfo.InvariantCulture) : null!;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent nullable DateTime value using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <param name="provider">An <see cref="IFormatProvider"/> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>A nullable DateTime equivalent to the value of this instance.</returns>
    public override DateTime? ToDateTime(IParameter targetParameter, IFormatProvider provider)
    {
        return _value;
    }

    /// <summary>
    /// Indicates whether the control has enumerated state (i.e., its state is held internally in an <see cref="EnumState"/> which
    /// requires special conversion, or if instead a regular value conversion is appropriate.
    /// </summary>
    public override bool HasEnumeratedState => false;

    #endregion

    #region IValueProvider Members

    /// <summary>
    /// Gets the current value of this control, for use in Edits as part of StateRules.
    /// </summary>
    /// <returns>Either a valid DateTime or null (meaning do not send this value over FIX).</returns>
    public override object GetCurrentValue()
    {
        return _value!;
    }

    #endregion
}
