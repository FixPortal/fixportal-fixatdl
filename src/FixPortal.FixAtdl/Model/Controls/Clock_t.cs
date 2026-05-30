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
using FixPortal.FixAtdl.Model.Controls.Support;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Types.Support;
using FixPortal.FixAtdl.Resources;

namespace FixPortal.FixAtdl.Model.Controls;

/// <summary>
/// Represents the Clock_t control element within FIXatdl.
/// </summary>
public class Clock_t : InitializableControl<DateTime?>
{
    private DateTime? _value;

    /// <summary>
    /// Initializes a new instance of <see cref="Clock_t"/> using the supplied ID.
    /// </summary>
    /// <param name="id">ID for this control.</param>
    public Clock_t(string id)
        : base(id)
    {
    }

    // TODO: Implement LocalMktTz as a type.
    /// <summary>The timezone in which initValue is represented in.  Required when initValue is supplied. Applicable when
    /// xsi:type is Clock_t.  Null when not supplied in the ATDL.</summary>
    public string? LocalMktTz { get; set; }

    /// <summary>Defines the treatment of initValue time. 0: use initValue; 1: use current time if initValue time has passed.
    /// The default value is 0.</summary>
    public int? InitValueMode { get; set; }

    /// <summary>
    /// The time source used when <see cref="InitValueMode"/> == 1. Defaults to the system clock;
    /// assign a fake in tests. (LocalMktTz timezone resolution is not yet applied — see remarks on
    /// LoadDefaultFromInitValue; both values are still compared in the host's local representation.)
    /// </summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    #region InitializableControl<T> Overrides

    /// <summary>
    /// Attempts to load the supplied FIX field value into this control.
    /// </summary>
    /// <param name="value">Value to set this control to.</param>
    /// <returns>true if it was possible to set the value of this control using the supplied value; false otherwise.</returns>
    protected override bool LoadDefaultFromFixValue(string value)
    {

        bool parsed = FixDateTime.TryParse(value, CultureInfo.InvariantCulture, out DateTime result);

        _value = parsed ? result : null;

        return parsed;
    }

    /// <summary>
    /// Loads this control with any supplied InitValue. If InitValue is not supplied, then control value will
    /// be set to default/empty value.
    /// </summary>
    protected override void LoadDefaultFromInitValue()
    {
        // Surface an invalid initValueMode (only null/0/1 are defined) rather than silently treating
        // anything that is not 1 as 0 (#4).
        if (InitValueMode is not (null or 0 or 1))
        {
            throw ThrowHelper.New<InvalidFieldValueException>(this, ErrorMessages.InitControlValueError,
                Id, string.Format(CultureInfo.InvariantCulture, "initValueMode '{0}' is invalid; expected 0 or 1", InitValueMode));
        }

        if (InitValue == null)
        {
            _value = null;
            return;
        }

        if (InitValueMode == 1)
        {
            // initValueMode 1: use the current time if the initValue time has already passed. Snapshot
            // "now" ONCE from the injected TimeProvider (the original read DateTime.Now twice, risking a
            // sub-tick inconsistency, and was untestable).
            DateTime now = TimeProvider.GetLocalNow().DateTime;

            _value = now > InitValue.Value ? now : InitValue;
        }
        else
        {
            _value = InitValue;
        }
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

            if (value == Atdl.NullValue)
            {
                _value = null;
            }
            else if (FixDateTime.TryParse(value, CultureInfo.InvariantCulture, out DateTime parsed))
            {
                // Accept a serialized timestamp so the control can round-trip its own ToString output,
                // not just {NULL} (#3).
                _value = parsed;
            }
            else
            {
                throw ThrowHelper.New<InvalidFieldValueException>(this, ErrorMessages.InitControlValueError,
                    Id, string.Format(CultureInfo.InvariantCulture, "'{0}' is not a valid value for this control", value));
            }
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
