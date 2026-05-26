// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;
using System.Globalization;
using System.Linq;
using System.Text;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Model.Controls.Support;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Resources;
using FixPortal.FixAtdl.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ThrowHelper = FixPortal.FixAtdl.Diagnostics.ThrowHelper;

namespace FixPortal.FixAtdl.Model.Types.Support;

/// <summary>
/// Base class for all reference type parameters (String_t, MultipleCharValue_t, MultipleStringValue_t, etc.).
/// </summary>
/// <remarks>Parameter types must be one of <see cref="AtdlValueType{T}"/> or <see cref="AtdlReferenceType{T}"/>.
/// The reason for the differentiation is that most FIXatdl types that use value types for the underlying storage
/// (Int_t, Float_t, UTCTimestamp_t, etc.) actually use <see cref="Nullable{T}"/> so that they can also contain
/// null, meaning don't include this value in the FIX output.  However, Nullable&lt;T&gt; is a value type, not
/// a reference type, and so a different base type is required to support underlying reference type usage, such
/// as in String_t.  (This is the same reason that it isn't possible to factor out apparently duplicated code
/// across AtdlValueType&lt;T&gt; and AtdlReferenceType&lt;T&gt;, because one uses T? internally and the
/// other uses T.)</remarks>
public abstract class AtdlReferenceType<T> : IParameterType where T : class
{
    // FP Enhancement: 2026-05-23 — TODO wire injected logger when refactoring class to accept ILogger.
    private static readonly ILogger _log = NullLogger.Instance;
    private static readonly CompositeFormat _attemptToSetConstValueParameterFormat = CompositeFormat.Parse(ErrorMessages.AttemptToSetConstValueParameter);

    /// <summary>
    /// Storage for the value of this parameter; null when not set.
    /// </summary>
    protected T? _value;

    /// <summary>
    /// Gets/sets an optional constant value for this parameter.
    /// </summary>
    /// <value>The const value.</value>
    public T? ConstValue { get; set; }

    #region IParameterType Members

    /// <summary>
    /// Indicates whether this parameter has been set to a value other than null.
    /// </summary>
    public bool IsSet => (ConstValue ?? _value) != null;

    /// <summary>
    /// Gets the value of this parameter as seen by the Control_t that references it.  May be null if the 
    /// parameter has no value, for example if it has explicitly been set via a state rule to {NULL}.
    /// </summary>
    /// <param name="hostParameter"><see cref="IParameter"/> that hosts the value.</param>
    /// <remarks>An <see cref="IControlConvertible"/> is returned enabling the parameter value to be converted into any 
    /// desired type, provided that the underlying value supports that type.</remarks>
    public IControlConvertible GetValueForControl(IParameter hostParameter)
    {
        // This base type doesn't know how to convert to control value types, but derived types must
        // implement IControlConvertible.
        return (this as IControlConvertible)!;
    }

    /// <summary>
    /// Sets the value of this parameter as seen by the Control_t that references it.
    /// </summary>
    /// <param name="hostParameter"><see cref="IParameter"/> that hosts the value.</param>
    /// <param name="value">Control value that implements <see cref="IParameterConvertible"/>.</param>
    /// <remarks>An <see cref="IParameterConvertible"/> is passed in enabling the control value to be converted into any
    /// desired type, provided that the value supports conversion to that type.</remarks>
    public ValidationResult SetValueFromControl(IParameter hostParameter, IParameterConvertible value)
    {
        if (ConstValue != null)
        {
            return new ValidationResult(ValidationResult.ResultType.Invalid, string.Format(CultureInfo.InvariantCulture, _attemptToSetConstValueParameterFormat, ConstValue));
        }

        try
        {
            _value = ConvertToNativeType(hostParameter, value);

            return ValidateValue(_value, hostParameter.Use == Use_t.Required);
        }
        catch (InvalidFieldValueException ex)
        {
            _log.LogError(ex, "Invalid value of type {Arg0} for parameter {Arg1}; exception text: {Arg2}",
                hostParameter.Type, hostParameter.Name, ex.Message);

            return new ValidationResult(ValidationResult.ResultType.Invalid, ErrorMessages.DataConversionFailure, HumanReadableTypeName);
        }
        catch (FormatException ex)
        {
            _log.LogError(ex, "Unable to convert value '{Arg0}' to type {Arg1} for parameter {Arg2}; exception text: {Arg3}",
                value, hostParameter.Type, hostParameter.Name, ex.Message);

            return new ValidationResult(ValidationResult.ResultType.Invalid, ErrorMessages.DataConversionFailure, HumanReadableTypeName);
        }
        catch (InvalidCastException ex)
        {
            _log.LogError(ex, "Unable to convert value '{Arg0}' to type {Arg1} for parameter {Arg2}; exception text: {Arg3}",
                value, hostParameter.Type, hostParameter.Name, ex.Message);

            return new ValidationResult(ValidationResult.ResultType.Invalid, ErrorMessages.DataConversionFailure, HumanReadableTypeName);
        }
        catch (ArgumentException ex)
        {
            _log.LogError(ex, "Unable to convert value '{Arg0}' to type {Arg1} for parameter {Arg2}; exception text: {Arg3}",
                value, hostParameter.Type, hostParameter.Name, ex.Message);

            return new ValidationResult(ValidationResult.ResultType.Invalid, ErrorMessages.DataConversionFailure, HumanReadableTypeName);
        }
    }

    /// <summary>
    /// Sets the wire value for this parameter.  This method is typically used to initialise the parameter through the
    /// InitValue mechanism, but may also be used to initialise the parameter when doing order amendments.
    /// </summary>
    /// <param name="hostParameter"><see cref="FixPortal.FixAtdl.Model.Elements.Parameter_t{T}"/> that is hosting this type. 
    /// Parameters in Atdl4net are represented by means of the generic Parameter_t type with the appropriate type parameter, 
    /// for example, Parameter_t&lt;Amt_t&gt;.</param>
    /// <param name="value">New wire value (all wire values in Atdl4net are strings).</param>
    public void SetWireValue(IParameter hostParameter, string value)
    {
        // When ConstValue is set, the only assignment we allow is if the supplied value is the same value as ConstValue.
        if (ConstValue != null)
        {
            if (ConvertToWireValueFormat(ConstValue) == value)
            {
                return;
            }

            throw ThrowHelper.New<InvalidOperationException>(this, ErrorMessages.AttemptToSetConstValueParameter, ConstValue);
        }

        T convertedValue = ConvertFromWireValueFormat(value);

        ValidationResult result = ValidateValue(convertedValue, true);

        _value = result.IsValid
            ? convertedValue
            : throw ThrowHelper.New<InvalidFieldValueException>(this,
                ErrorMessages.InvalidParameterSetValue, hostParameter.Name, value, result.ErrorText);
    }

    /// <summary>
    /// Gets the wire value for this parameter.  This method is used to retrieve the value of the parameter that should
    /// be transmitted over FIX.
    /// </summary>
    /// <param name="hostParameter"><see cref="Parameter_t{T}"/> that is hosting this type.  Parameters in Atdl4net are
    /// represented by means of the generic Parameter_t type with the appropriate type parameter, for example, 
    /// Parameter_t&lt;Amt_t&gt;.</param>
    /// <returns>The parameter's current wire value (all wire values in Atdl4net are strings).</returns>
    public string GetWireValue(IParameter hostParameter)
    {
        T? value = ConstValue ?? _value;

        ValidationResult validity = ValidateValue(value!, hostParameter.Use == Use_t.Required);

        if (!validity.IsValid)
        {
            if (validity.IsMissing)
            {
                throw ThrowHelper.New<MissingMandatoryValueException>(this, ErrorMessages.NonOptionalParameterNotSupplied, hostParameter.Name);
            }

            throw ThrowHelper.New<InvalidFieldValueException>(ErrorMessages.InvalidGetParameterValue,
                hostParameter.Name, value, validity.ErrorText);
        }

        string wireValue = ConvertToWireValueFormat(value!);

        _log.LogDebug("Wire value for parameter {Arg0} = '{Arg1}'", hostParameter.Name, wireValue);

        return wireValue;
    }

    /// <summary>
    /// Gets the value of this parameter type in its native (i.e., raw) form, such as int, char, string, etc. 
    /// </summary>
    /// <param name="applyWireValueFormat">If set to true, the value returned is adjusted to be in the 'format'
    /// it would be if sent on the FIX wire.  For example, for Float_t parameters, setting this value to true
    /// would cause the Precision attribute setting to be applied.</param>
    /// <returns>Native parameter value.</returns>
    public virtual object GetNativeValue(bool applyWireValueFormat)
    {
        return ConstValue != null ? ConstValue : _value!;
    }

    /// <summary>
    /// Gets the human-readable name of this type.
    /// </summary>
    public string HumanReadableTypeName => GetHumanReadableTypeName();

    /// <summary>
    /// Resets this parameter value to its default state.
    /// </summary>
    public void Reset()
    {
        _value = null;
    }

    #endregion

    #region Abstract Methods that all FIXatdl value-based types must implement

    /// <summary>
    /// Validates the supplied value in terms of the parameters constraints (e.g., MinValue, MaxValue, etc.).
    /// </summary>
    /// <param name="value">Value to validate, may be null in which case no validation is applied.</param>
    /// <param name="isRequired">Set to true to check that this parameter is non-null.</param>
    /// <returns>Value passed in is returned if it is valid; otherwise an appropriate exception is thrown.</returns>
    protected abstract ValidationResult ValidateValue(T value, bool isRequired);

    /// <summary>
    /// Converts the supplied value from string format (as might be used on the FIX wire) into the type of the type
    /// parameter for this type.  
    /// </summary>
    /// <param name="value">Type to convert from string, may be null.</param>
    /// <returns>If input value is not null, returns value converted from a string; null otherwise.</returns>
    protected abstract T ConvertFromWireValueFormat(string value);

    /// <summary>
    /// Converts the supplied value to a string, as might be used on the FIX wire.
    /// </summary>
    /// <param name="value">Value to convert, may be null.</param>
    /// <returns>If input value is not null, returns value converted to a string; null otherwise.</returns>
    protected abstract string ConvertToWireValueFormat(T value);

    /// <summary>
    /// Converts the supplied value to the type parameter type (T) for this class.
    /// </summary>
    /// <param name="hostParameter"><see cref="IParameter"/> that hosts this value.</param>
    /// <param name="value">Value to convert, may be null.</param>
    /// <returns>If input value is not null, returns value converted to T; null otherwise.</returns>
    protected abstract T ConvertToNativeType(IParameter hostParameter, IParameterConvertible value);

    /// <summary>
    /// Gets the human-readable type name for use in error messages shown to the user.
    /// </summary>
    /// <returns>Human-readable type name.</returns>
    protected abstract string GetHumanReadableTypeName();

    #endregion
}
