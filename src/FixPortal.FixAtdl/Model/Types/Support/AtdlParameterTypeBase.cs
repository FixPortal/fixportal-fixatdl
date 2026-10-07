// FP Enhancement: 2026-09-27 — new file: shared generic base extracted from AtdlValueType<T> and
// AtdlReferenceType<T>, which were near-identical copies of this machinery (duplication audit finding).
// The two pivots remain as the public storage-type discriminators; every behavioural detail (validation
// ordering, caught exception sets, message formats, null handling) is preserved verbatim from them.
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Globalization;
using System.Text;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Model.Controls.Support;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Resources;
using FixPortal.FixAtdl.Validation;
using ThrowHelper = FixPortal.FixAtdl.Diagnostics.ThrowHelper;

namespace FixPortal.FixAtdl.Model.Types.Support;

// Single shared, pre-parsed message format for every closed AtdlParameterTypeBase<TStorage>. It must
// live outside the generic class: a static field in a generic type is per-closed-type (S2743), which
// would needlessly re-parse the same invariant resource once per storage type.
file static class SharedMessageFormats
{
    internal static readonly CompositeFormat AttemptToSetConstValueParameter = CompositeFormat.Parse(
        ErrorMessages.AttemptToSetConstValueParameter
    );
}

/// <summary>
/// Implements the value/wire/conversion machinery shared by <see cref="AtdlValueType{T}"/> and
/// <see cref="AtdlReferenceType{T}"/>, the two parameter-type pivots.
/// </summary>
/// <typeparam name="TStorage">The storage type of the parameter value: <c>T?</c> (i.e.
/// Nullable&lt;T&gt;) for value-type parameters via <see cref="AtdlValueType{T}"/>, or <c>T</c> itself for
/// reference-type parameters via <see cref="AtdlReferenceType{T}"/>. Because TStorage already carries the
/// nullability, this base stays unconstrained and every member is written against TStorage?.</typeparam>
/// <remarks>Parameter types must be one of <see cref="AtdlValueType{T}"/> or
/// <see cref="AtdlReferenceType{T}"/>; this type is their shared implementation, not a third kind of
/// parameter. The historical claim (recorded on both pivots) that the duplicated code could not be
/// factored out — because one pivot uses T? internally and the other T — held only while C# could not
/// express T? over an unconstrained type parameter. Since C# 9 that annotation is "defaultable"
/// (no Nullable&lt;T&gt; re-wrapping when TStorage is itself a nullable value type), so the machinery
/// lives here exactly once.</remarks>
public abstract class AtdlParameterTypeBase<TStorage> : IParameterType
{
    /// <summary>
    /// Restricts derivation to the two in-assembly pivots, <see cref="AtdlValueType{T}"/> and
    /// <see cref="AtdlReferenceType{T}"/> (per the remarks above: this base is their shared
    /// implementation, not a third kind of parameter). Consumers deriving from the pivots are
    /// unaffected.
    /// </summary>
    private protected AtdlParameterTypeBase() { }

    /// <summary>
    /// Storage for the value of this parameter; null (no value) when not set.
    /// </summary>
    protected TStorage? _value;

    /// <summary>
    /// Gets/sets an optional constant value for this parameter.
    /// </summary>
    /// <value>The const value.</value>
    public TStorage? ConstValue
    {
        get;
        set => field = NormalizeAssignedConstValue(value);
    }

    #region IParameterType Members

    /// <summary>
    /// Indicates whether this parameter has been set to a value other than null.
    /// </summary>
    public bool IsSet => (ConstValue ?? _value) is not null;

    /// <summary>
    /// Gets the value of this parameter as seen by the Control_t that references it.  May be null if the
    /// parameter has no value, for example if it has explicitly been set via a state rule to {NULL}.
    /// </summary>
    /// <param name="hostParameter"><see cref="IParameter"/> that hosts the value.</param>
    /// <remarks>An <see cref="IControlConvertible"/> is returned enabling the parameter value to be converted into any
    /// desired type, provided that the underlying value supports that type.</remarks>
    public IControlConvertible GetValueForControl(IParameter hostParameter)
    {
        // Derived parameter types must implement IControlConvertible; surface a clear diagnostic rather
        // than a null-forgiving NRE should a future type fail to.
        return this as IControlConvertible
            ?? throw ThrowHelper.New<InternalErrorException>(
                this,
                $"Parameter type {GetType().Name} does not implement IControlConvertible."
            );
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
        if (ConstValue is not null)
        {
            return new ValidationResult(
                ValidationResult.ResultType.Invalid,
                string.Format(
                    CultureInfo.InvariantCulture,
                    SharedMessageFormats.AttemptToSetConstValueParameter,
                    ConstValue
                )
            );
        }

        try
        {
            TStorage? candidate = ConvertToNativeType(hostParameter, value);

            candidate = NormalizeControlCandidate(candidate);

            ValidationResult result = ValidateValue(candidate, hostParameter.Use == Use_t.Required);

            // Commit the converted value only when it validates (or is a legitimate null/cleared
            // state). An out-of-range or otherwise rejected candidate must NOT leave the parameter
            // reporting IsSet==true with the bad value still stored; but a clear is always committed,
            // so a required parameter cannot keep emitting a stale value after the control is emptied.
            if (result.IsValid || candidate is null)
            {
                _value = candidate;
            }

            return result;
        }
        catch (Exception ex)
            when (ex
                    is InvalidFieldValueException
                        or FormatException
                        or InvalidCastException
                        or ArgumentException
                        or OverflowException
            )
        {
            return new ValidationResult(
                ValidationResult.ResultType.Invalid,
                ErrorMessages.DataConversionFailure,
                HumanReadableTypeName
            );
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
        if (ConstValue is not null)
        {
            if (ConvertToWireValueFormat(ConstValue) == value)
            {
                return;
            }

            throw ThrowHelper.New<InvalidOperationException>(
                this,
                ErrorMessages.AttemptToSetConstValueParameter,
                ConstValue
            );
        }

        TStorage? convertedValue;

        // A '{NULL}' wire value is the FIXatdl "clear this field" instruction. Map it to null (default
        // for the storage type) before the per-type converter, then use the normal validation path below
        // so Required parameters reject it while Optional parameters clear. The subclasses' own sentinel
        // arms stay as defence for direct converter calls (Low 9).
        try
        {
            convertedValue = value == Atdl.NullValue ? default : ConvertFromWireValueFormat(value);
        }
        catch (Exception ex)
            when (ex is FormatException or OverflowException or ArgumentException or InvalidCastException)
        {
            // Translate raw BCL conversion failures (Convert.ToInt32/ToDecimal/etc. in the numeric
            // subclasses) into a domain InvalidFieldValueException, matching the control-set path
            // rather than leaking a raw exception out of the wire boundary.
            throw ThrowHelper.New<InvalidFieldValueException>(
                this,
                ex,
                ErrorMessages.InvalidParameterSetValue,
                hostParameter.Name,
                value,
                ex.Message
            );
        }

        ValidationResult result = ValidateValue(convertedValue, hostParameter.Use == Use_t.Required);

        _value = result.IsValid
            ? convertedValue
            : throw ThrowHelper.New<InvalidFieldValueException>(
                this,
                ErrorMessages.InvalidParameterSetValue,
                hostParameter.Name,
                value,
                result.ErrorText
            );
    }

    /// <summary>
    /// Gets the wire value for this parameter.  This method is used to retrieve the value of the parameter that should
    /// be transmitted over FIX.
    /// </summary>
    /// <param name="hostParameter"><see cref="IParameter"/> that is hosting this type. Parameters in Atdl4net are
    /// represented by means of the generic Parameter_t type with the appropriate type parameter, for example,
    /// Parameter_t&lt;Amt_t&gt;.</param>
    /// <returns>The parameter's current wire value (all wire values in Atdl4net are strings).</returns>
    public string? GetWireValue(IParameter hostParameter)
    {
        TStorage? value = ConstValue ?? _value;

        ValidationResult validity = ValidateValue(value, hostParameter.Use == Use_t.Required);

        if (!validity.IsValid)
        {
            if (validity.IsMissing)
            {
                throw ThrowHelper.New<MissingMandatoryValueException>(
                    this,
                    ErrorMessages.NonOptionalParameterNotSupplied,
                    hostParameter.Name
                );
            }

            throw ThrowHelper.New<InvalidFieldValueException>(
                this,
                ErrorMessages.InvalidGetParameterValue,
                hostParameter.Name,
                value,
                validity.ErrorText
            );
        }

        string? wireValue = ConvertToWireValueFormat(value);

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
        return ConstValue is not null ? ConstValue : _value!;
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
        _value = default;
    }

    #endregion

    /// <summary>
    /// Normalises a constant at assignment, before it is stored. An empty string or an empty
    /// char array is not a value (it counts as set while the wire form reports nothing) and becomes
    /// null. Numeric zeroes and <c>false</c> are real values and are left unchanged. Subclasses may
    /// reject a constant that cannot be emitted.
    /// </summary>
    /// <param name="value">Constant supplied by the caller, may be null.</param>
    /// <returns>The constant to store.</returns>
    protected virtual TStorage? NormalizeAssignedConstValue(TStorage? value)
    {
        return value is string { Length: 0 } or char[] { Length: 0 } ? default : value;
    }

    /// <summary>
    /// Hook that lets a storage class normalise a control-supplied candidate before validation. The
    /// default is the identity; <see cref="AtdlReferenceType{T}"/> overrides it to map an empty string or
    /// empty char array (a cleared reference-type control) to null.
    /// </summary>
    /// <param name="candidate">Value returned by <see cref="ConvertToNativeType"/>, may be null.</param>
    /// <returns>The candidate to validate and commit.</returns>
    protected virtual TStorage? NormalizeControlCandidate(TStorage? candidate)
    {
        return candidate;
    }

    /// <summary>
    /// Creates the <see cref="InvalidCastException"/> thrown when an <see cref="IControlConvertible"/>
    /// conversion is requested that a parameter type does not support. This is the single home of the
    /// rejection block that every parameter type previously inlined identically.
    /// </summary>
    /// <param name="value">Value reported in the exception message; callers pass either
    /// <see cref="_value"/> or <c>ConstValue ?? _value</c>, matching their historical behaviour.</param>
    /// <param name="targetType">Human-readable name of the conversion target that was requested.</param>
    /// <returns>The exception to throw.</returns>
    protected InvalidCastException UnsupportedConversion(object? value, string targetType)
    {
        return ThrowHelper.New<InvalidCastException>(
            this,
            ErrorMessages.UnsupportedParameterValueConversion,
            value,
            targetType
        );
    }

    #region Abstract Methods that all FIXatdl types must implement

    /// <summary>
    /// Validates the supplied value in terms of the parameters constraints (e.g., MinValue, MaxValue, etc.).
    /// </summary>
    /// <param name="value">Value to validate, may be null in which case no validation is applied.</param>
    /// <param name="isRequired">Set to true to check that this parameter is non-null.</param>
    /// <returns>Value passed in is returned if it is valid; otherwise an appropriate exception is thrown.</returns>
    protected abstract ValidationResult ValidateValue(TStorage? value, bool isRequired);

    /// <summary>
    /// Converts the supplied value from string format (as might be used on the FIX wire) into the type of the type
    /// parameter for this type.
    /// </summary>
    /// <param name="value">Type to convert from string, may be null.</param>
    /// <returns>If input value is not null, returns value converted from a string; null otherwise.</returns>
    protected abstract TStorage? ConvertFromWireValueFormat(string value);

    /// <summary>
    /// Converts the supplied value to a string, as might be used on the FIX wire.
    /// </summary>
    /// <param name="value">Value to convert, may be null.</param>
    /// <returns>If input value is not null, returns value converted to a string; null otherwise.</returns>
    protected abstract string? ConvertToWireValueFormat(TStorage? value);

    /// <summary>
    /// Converts the supplied value to the storage type for this class.
    /// </summary>
    /// <param name="hostParameter"><see cref="IParameter"/> that hosts this value.</param>
    /// <param name="value">Value to convert, may be null.</param>
    /// <returns>If input value is not null, returns value converted to the storage type; null otherwise.</returns>
    /// <remarks>Used when setting a parameter value from a control (or anything else that
    /// implements <see cref="IParameterConvertible"/>).</remarks>
    protected abstract TStorage? ConvertToNativeType(IParameter hostParameter, IParameterConvertible value);

    /// <summary>
    /// Gets the human-readable type name for use in error messages shown to the user.
    /// </summary>
    /// <returns>Human-readable type name.</returns>
    protected abstract string GetHumanReadableTypeName();

    #endregion
}
