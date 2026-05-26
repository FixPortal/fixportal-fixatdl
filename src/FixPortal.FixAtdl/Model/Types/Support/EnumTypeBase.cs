// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Diagnostics;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Controls.Support;
using FixPortal.FixAtdl.Resources;

namespace FixPortal.FixAtdl.Model.Types.Support;

/// <summary>
/// Provides shared conversion behavior for enum-backed FIXatdl value types.
/// </summary>
public abstract class EnumTypeBase<T> : AtdlValueType<T>, IControlConvertible where T : struct
{
    #region IControlConvertible Members

    /// <summary>
    /// Converts the value of this instance to an equivalent nullable boolean value.
    /// </summary>
    /// <returns>One of true, false or null which is equivalent to the value of this instance.</returns>
    public bool? ToBoolean()
    {
        throw ThrowHelper.New<InvalidCastException>(this, ErrorMessages.UnsupportedParameterValueConversion, _value, "Boolean");
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent string value using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="provider">An <see cref="IFormatProvider"/> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>A string value equivalent to the value of this instance.  May be null.</returns>
    public string? ToString(IFormatProvider? provider)
    {
        T? value = ConstValue ?? _value;

        return value != null ? Enum.GetName(typeof(T), value) : null;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent nullable decimal value using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>A nullable decimal equivalent to the value of this instance.</returns>
    public decimal? ToDecimal()
    {
        throw ThrowHelper.New<InvalidCastException>(this, ErrorMessages.UnsupportedParameterValueConversion, _value, "Decimal");
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent nullable DateTime value using the specified culture-specific formatting information.
    /// </summary>
    /// <returns>A nullable DateTime equivalent to the value of this instance.</returns>
    public DateTime? ToDateTime()
    {
        throw ThrowHelper.New<InvalidCastException>(this, ErrorMessages.UnsupportedParameterValueConversion, _value, "DateTime");
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent EnumState value.
    /// </summary>
    /// <returns>A valid EnumState, assuming the source value can be correctly converted.</returns>
    /// <remarks>This method converts the enum value to a string, looks up the EnumID from the supplied
    /// EnumPairCollection and then returns a new EnumState.  This method may be a little slow for
    /// very large enumerations.</remarks>
    public EnumState ToEnumState(EnumPairCollection enumPairs)
    {
        EnumState state = new(enumPairs.EnumIds);

        string wireValue = ToString(null)!;

        if (enumPairs.TryParseWireValue(wireValue, out string? enumId))
        {
            state[enumId!] = true;
        }

        return state;
    }

    #endregion
}
