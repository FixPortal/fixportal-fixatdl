// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;
using System.Globalization;
using FixPortal.FixAtdl.Diagnostics;
using FixPortal.FixAtdl.Resources;

namespace FixPortal.FixAtdl.Fix;

/// <summary>
/// Represents a FIX tag, a non-zero positive integer.
/// </summary>
public readonly struct FixTag
{
    private readonly int _value;

    /// <summary>
    /// Initializes a new instance of FixTag.
    /// </summary>
    /// <param name="value"></param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown if a value less than or equal to zero is supplied.</exception>
    public FixTag(int value)
    {
        if (value <= 0)
        {
            throw ThrowHelper.New<ArgumentOutOfRangeException>(typeof(FixTag).FullName, ErrorMessages.NonZeroPositiveIntRequired, value);
        }

        _value = value;
    }

    /// <summary>
    /// Performs an implicit conversion from <see cref="int"/> to <see cref="FixPortal.FixAtdl.Fix.FixTag"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator FixTag(int value) => new FixTag(value);

    /// <summary>
    /// Performs an implicit conversion from <see cref="FixPortal.FixAtdl.Fix.FixField"/> to <see cref="FixPortal.FixAtdl.Fix.FixTag"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator FixTag(FixField value) => new FixTag((int)value);

    /// <summary>
    /// Performs an implicit conversion from <see cref="FixPortal.FixAtdl.Fix.FixTag"/> to <see cref="int"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator int(FixTag value) => value._value;

    /// <summary>
    /// Performs an implicit conversion from <see cref="FixPortal.FixAtdl.Fix.FixTag"/> to <see cref="FixPortal.FixAtdl.Fix.FixField"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator FixField(FixTag value) => (FixField)value._value;

    /// <summary>
    /// Returns a <see cref="string"/> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="string"/> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return _value.ToString(CultureInfo.InvariantCulture);
    }
}
