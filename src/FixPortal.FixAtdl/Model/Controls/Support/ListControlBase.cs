// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Diagnostics;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Types.Support;
using FixPortal.FixAtdl.Resources;

namespace FixPortal.FixAtdl.Model.Controls.Support;

/// <summary>
/// Base class for the subset of FIXatdl controls that allow ListItems.
/// </summary>
/// <remarks>The following controls support ListItems:
/// <list type="bullet">
/// <item><description>CheckBoxList_t</description></item>
/// <item><description>DropDownList_t</description></item>
/// <item><description>EditableDropDownList_t</description></item>
/// <item><description>MultiSelectList_t</description></item>
/// <item><description>RadioButtonList_t</description></item>
/// <item><description>SingleSelectList_t</description></item>
/// <item><description>Slider_t</description></item>
/// </list>
/// </remarks>
public abstract class ListControlBase : InitializableControl<string>
{
    /// <summary>
    /// EnumState for this control which provides storage of the state of each ListItem.
    /// </summary>
    protected EnumState _value = null!;

    /// <summary>
    /// The ListItems for this control; will be empty if no ListItem sub-elements are present.
    /// </summary>
    protected readonly ListItemCollection _listItems = [];

    /// <summary>
    /// Indicates whether the EnumState value for this control can be set to a value other than one of the enumerated
    /// values.  (This property is present to support editable drop-down list controls.)
    /// </summary>
    protected virtual bool IsNonEnumValueAllowed => false;

    /// <summary>
    /// Initializes the base Control_t class with the supplied control identifier.
    /// </summary>
    /// <param name="id">ID for this control.</param>
    protected ListControlBase(string id)
        : base(id)
    {
    }

    #region InitializableControl<T> Overrides

    /// <summary>
    /// Attempts to load the supplied FIX field value into this control.
    /// </summary>
    /// <param name="value">Value to set this control to.</param>
    /// <returns>true if it was possible to set the value of this control using the supplied value; false otherwise.</returns>
    /// <remarks>Although the method name might suggest that value is a FIX wire value, for list controls, this
    /// parameter is in fact an enumID.</remarks>
    protected override bool LoadDefaultFromFixValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        _value = new EnumState(ListItems.EnumIds);

        try
        {
            _value.LoadInitValue(value, IsNonEnumValueAllowed);
        }
        catch (Exception ex) when (ex is ArgumentException or FixAtdlException)
        {
            // Honour the bool contract: an invalid EnumID falls back to "not initialised" rather than
            // aborting initialisation by throwing (as BinaryControlBase already does).
            return false;
        }

        return true;
    }

    /// <summary>
    /// Loads this control with any supplied InitValue. If InitValue is not supplied, then control value will
    /// be set to default/empty value.
    /// </summary>
    protected override void LoadDefaultFromInitValue()
    {
        _value = new EnumState(ListItems.EnumIds);

        if (InitValue != null)
        {
            _value.LoadInitValue(InitValue, IsNonEnumValueAllowed);
        }
    }

    #endregion

    #region Control_t Overrides

    /// <summary>
    /// Gets the value of this control as an EnumState.
    /// </summary>
    /// <returns>EnumState that reflects which checkboxes are selected.</returns>
    public override object GetCurrentValue()
    {
        return _value;
    }

    /// <summary>
    /// Sets the value of this control; either via an EnumState, or using the FIXatdl '{NULL}' value.
    /// </summary>
    /// <param name="newValue">Value that reflects which checkboxes are selected.</param>
    public override void SetValue(object newValue)
    {
        if (_value == null)
        {
            throw ThrowHelper.New<InternalErrorException>(this, InternalErrors.UnexpectedNullReference, "_value",
                "FixPortal.FixAtdl.Model.Types.Support.EnumState");
        }

        if (newValue == null || newValue as string == Atdl.NullValue)
        {
            _value.ClearAll();
        }
        else if (newValue is EnumState enumState)
        {
            _value.UpdateFrom(enumState);
        }
        else
        {
            // Reject a non-EnumState argument with a clear diagnostic rather than (newValue as
            // EnumState)! resolving to null and UpdateFrom throwing ArgumentNullException.
            throw ThrowHelper.New<InternalErrorException>(this, InternalErrors.UnexpectedArgumentType,
                newValue.GetType().FullName, "FixPortal.FixAtdl.Model.Types.Support.EnumState");
        }
    }

    /// <summary>
    /// Resets this control to either a null value or for list controls, all options unselected.
    /// </summary>
    public override void Reset()
    {
        _value?.ClearAll();
    }

    /// <summary>
    /// Gets the collection of ListItems for this control.
    /// </summary>
    public ListItemCollection ListItems => _listItems;

    /// <summary>
    /// Indicates whether this control has one or more ListItems.
    /// </summary>
    public bool HasListItems => _listItems.HasItems;

    /// <summary>
    /// Sets the value of this control using the value of the supplied parameter.
    /// </summary>
    /// <param name="parameter">Parameter to set this control's value from.</param>
    public override void SetValueFromParameter(IParameter parameter)
    {
        IControlConvertible value = parameter.GetValueForControl();

        _value = value.ToEnumState(parameter.EnumPairs);
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent nullable boolean value.
    /// </summary>
    /// <returns>One of true, false or null which is equivalent to the value of this instance.</returns>
    public override bool? ToBoolean(IParameter targetParameter)
    {
        throw ThrowHelper.New<InvalidCastException>(this, ErrorMessages.UnsupportedControlValueConversion, _value, "Boolean", Id);
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent nullable decimal value using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="targetParameter"></param>
    /// <param name="provider">An <see cref="IFormatProvider"/> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>A nullable decimal equivalent to the value of this instance.</returns>
    public override decimal? ToDecimal(IParameter targetParameter, IFormatProvider provider)
    {
        string wireValue = ToString(targetParameter);

        return TryConvertToDecimal(wireValue, out decimal result) ? result : null;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent 32-bit signed integer using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="targetParameter"></param>
    /// <param name="provider">An <see cref="IFormatProvider"/> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>A nullable 32-bit signed integer equivalent to the value of this instance.</returns>
    public override int? ToInt32(IParameter targetParameter, IFormatProvider provider)
    {
        string wireValue = ToString(targetParameter);

        return TryConvertToInt(wireValue, out int result) ? result : null;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent 32-bit unsigned integer using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="targetParameter"></param>
    /// <param name="provider">An <see cref="IFormatProvider"/> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>A nullable 32-bit unsigned integer equivalent to the value of this instance.</returns>
    public override uint? ToUInt32(IParameter targetParameter, IFormatProvider provider)
    {
        string wireValue = ToString(targetParameter);

        return TryConvertToUint(wireValue, out uint result) ? result : null;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent char value.
    /// </summary>
    /// <returns>A nullable char value equivalent to the value of this instance.  May be null.</returns>
    public override char? ToChar(IParameter targetParameter)
    {
        string wireValue = ToString(targetParameter);

        return TryConvertToChar(wireValue, out var result) ? result : null;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent string value using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>A string value equivalent to the value of this instance.  May be null.</returns>
    public override string ToString(IParameter targetParameter)
    {
        if (_value == null)
        {
            throw ThrowHelper.New<InternalErrorException>(this, InternalErrors.UnexpectedNullReference, "_value", GetType().Name);
        }

        try
        {
            return _value.ToWireValue(targetParameter.EnumPairs);
        }
        catch (InvalidOperationException ex)
        {
            throw ThrowHelper.Rethrow(this, ex, ErrorMessages.UnsuccessfulSetParameterOperation, targetParameter.Name, Id, ex.Message);
        }
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent nullable DateTime value using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="targetParameter"></param>
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
    public override bool HasEnumeratedState => _value != null;

    #endregion
}
