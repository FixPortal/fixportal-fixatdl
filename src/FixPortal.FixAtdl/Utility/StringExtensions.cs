// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;
using FixPortal.FixAtdl.Diagnostics;
using FixPortal.FixAtdl.Resources;

namespace FixPortal.FixAtdl.Utility;

/// <summary>
/// Provides extension methods for System.String.
/// </summary>
public static class StringExtensions
{
    private static readonly string ExceptionContext = typeof(StringExtensions).FullName!;

    /// <summary>
    /// Gets the string representation of this enumerated type value.
    /// </summary>
    /// <typeparam name="T">Type of enum.</typeparam>
    /// <param name="value">Value to convert to the supplied enum type.</param>
    /// <returns>A valid enumerated value if the conversion was possible; an exception is thrown otherwise.</returns>
    public static T ParseAsEnum<T>(this string value) where T : struct, Enum
    {
        if (string.IsNullOrEmpty(value))
        {
            throw ThrowHelper.New<ArgumentNullException>(ExceptionContext, ErrorMessages.NullOrEmptyStringEnumParseFailure, typeof(T).Name);
        }

        try
        {
            return Enum.Parse<T>(value, true);
        }
        catch (ArgumentException ex)
        {
            throw ThrowHelper.New<ArgumentException>(ExceptionContext, ex, ErrorMessages.InvalidValueEnumParseFailure, value, typeof(T).Name);
        }
    }
}

