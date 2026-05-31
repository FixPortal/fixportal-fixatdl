// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Globalization;
using FixPortal.FixAtdl.Diagnostics;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Types.Support;
using FixPortal.FixAtdl.Resources;

namespace FixPortal.FixAtdl.Model.Controls.Support;

/// <summary>
/// Represents control elements within FIXatdl that can support one of two states (<see cref="CheckBox_t"/>, <see cref="RadioButton_t"/>).
/// </summary>
public abstract class BinaryControlBase : InitializableControl<bool?>
{
    /// <summary>
    /// The state value for this control.
    /// </summary>
    // Default to a concrete false (not null) so an unset binary control reads as "false" for
    // EQ "false" StateRules even before LoadDefaults runs (matching the post-LoadDefaults default).
    // Because false is a value that IS sent over FIX, an unset control also reads as present for
    // EX/NX edits (EX true / NX false). Reset() still sets null deliberately (null = "do not send";
    // EX false / NX true) — that construct-vs-Reset asymmetry is intentional. The three-state
    // contract is otherwise unchanged.
    protected bool? _value = false;

    /// <summary>
    /// Initializes a new instance of <see cref="BinaryControlBase"/> using the supplied ID.
    /// </summary>
    /// <param name="id">ID for this control.</param>
    protected BinaryControlBase(string id)
        : base(id)
    {
    }

    /// <summary>Output EnumID if checked/selected.  Applicable when xsi:type is CheckBox_t or RadioButton_t.</summary>
    public string CheckedEnumRef { get; set; } = null!;

    /// <summary>Output EnumID if unchecked/not selected.  Applicable when xsi:type is CheckBox_t or RadioButton_t.</summary>
    public string UncheckedEnumRef { get; set; } = null!;

    #region InitializableControl<T> Overrides

    /// <summary>
    /// Loads the supplied FIX field value into this control.
    /// </summary>
    /// <param name="value">Value to set this control to.</param>
    protected override bool LoadDefaultFromFixValue(string value)
    {
        try
        {
            SetValue(value);

            return true;
        }
        catch (InvalidFieldValueException)
        {
            return false;
        }
    }

    /// <summary>
    /// Loads this control with any supplied InitValue. If InitValue is not supplied, then control value will
    /// be set to default/empty value.
    /// </summary>
    protected override void LoadDefaultFromInitValue()
    {
        // Binary controls should have a default state of off if no InitValue is specified.
        if (InitValue != null)
        {
            SetValue(InitValue);
        }
        else
        {
            _value = false;
        }
    }

    #endregion

    #region Control_t Overrides

    /// <summary>
    /// Gets the value of this control.  May be null.
    /// </summary>
    /// <returns>One of three states, true, false or null (meaning do not send this value over FIX).</returns>
    public override object GetCurrentValue()
    {
        return _value!;
    }

    /// <summary>
    /// Sets the value of this control using the value of the supplied parameter.
    /// </summary>
    /// <param name="parameter">Parameter to set this control's value from.</param>
    public override void SetValueFromParameter(IParameter parameter)
    {
        IControlConvertible value = parameter.GetValueForControl();

        // Special treatment needed here as we can't just assume that the source parameter has enumerated values, but if it
        // does, then we can't use ToBoolean as that won't map the wire values correctly to the state of this control.
        if (HasEnumeratedState)
        {
            if (parameter.HasEnumPairs)
            {
                EnumState state = value.ToEnumState(parameter.EnumPairs);

                _value = state[CheckedEnumRef] ? true : state[UncheckedEnumRef] ? false : null;
            }
            else
            {
                throw ThrowHelper.New<InconsistentStrategyException>(this, ErrorMessages.InconsistentEnumPairsListItemsError);
            }
        }
        else
        {
            _value = value.ToBoolean();
        }
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
        bool isBool = newValue is bool;

        // Strictly this is a bit of a hack as the right thing to do when implementing CheckBoxes and RadioButtons is
        // to enforce the use of boolean inputs.  However as atdl4j supports setting of these controls' state via
        // EnumIDs in the InitValue, guess we better do the same...
        if (isString)
        {
            string? value = newValue as string;

            if (value == Atdl.NullValue)
            {
                _value = null;
            }
            else if (value == CheckedEnumRef)
            {
                _value = true;
            }
            else if (value == UncheckedEnumRef)
            {
                _value = false;
            }
            else if (!HasEnumeratedState && TryParseBooleanWireValue(value, out bool boolValue))
            {
                // When the bound parameter has no EnumPairs, a raw FIX boolean wire value (Y/N or
                // true/false) reaches here; accept it instead of silently failing init and falling back.
                _value = boolValue;
            }
            else
            {
                throw ThrowHelper.New<InvalidFieldValueException>(this, ErrorMessages.InitControlValueError,
                    Id, string.Format(CultureInfo.InvariantCulture, "'{0}' is not a valid value for this control", value));
            }
        }
        else
        {
            _value = isBool
                ? (bool?)newValue
                : newValue == null
            ? null
            : throw ThrowHelper.New<InternalErrorException>(this, InternalErrors.UnexpectedArgumentType,
                newValue.GetType().FullName!, "System.String, System.Boolean");
        }
    }

    /// <summary>
    /// Resets this control to either a null value or for list controls, all options unselected.
    /// </summary>
    public override void Reset()
    {
        _value = null;
    }

    private static bool TryParseBooleanWireValue(string? value, out bool result)
    {
        if (value == "Y")
        {
            result = true;
            return true;
        }

        if (value == "N")
        {
            result = false;
            return true;
        }

        return bool.TryParse(value, out result);
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent nullable boolean value.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <returns>One of true, false or null which is equivalent to the value of this instance.</returns>
    public override bool? ToBoolean(IParameter targetParameter)
    {
        return _value;
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
        string? wireValue = _value != null ? ToString(targetParameter) : null;


        return TryConvertToInt(wireValue, out int result) ? result : null;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent 32-bit unsigned integer using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <param name="provider">An <see cref="IFormatProvider"/> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>A nullable 32-bit unsigned integer equivalent to the value of this instance.</returns>
    public override uint? ToUInt32(IParameter targetParameter, IFormatProvider provider)
    {
        string? wireValue = _value != null ? ToString(targetParameter) : null;


        return TryConvertToUint(wireValue, out uint result) ? result : null;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent char value.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <returns>A nullable char value equivalent to the value of this instance.  May be null.</returns>
    public override char? ToChar(IParameter targetParameter)
    {
        string? wireValue = _value != null ? ToString(targetParameter) : null;

        return TryConvertToChar(wireValue, out var result) ? result : null;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent string value using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <returns>A string value equivalent to the value of this instance.  May be null.</returns>
    public override string ToString(IParameter targetParameter)
    {
        if (_value != null && HasEnumeratedState)
        {
            EnumPairCollection enumPairs = targetParameter.EnumPairs;

            string value = (bool)_value ? enumPairs.GetWireValueFromEnumId(CheckedEnumRef) : enumPairs.GetWireValueFromEnumId(UncheckedEnumRef);

            // It is possible for '{NULL}' to be provided as one of the enum wire values, so we have to act accordingly
            return value != Atdl.NullValue ? value : null!;
        }
        else
        {
            return _value != null ? _value.Value.ToString().ToLower() : null!;
        }
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
    public override bool HasEnumeratedState => CheckedEnumRef != null && UncheckedEnumRef != null;

    #endregion
}
