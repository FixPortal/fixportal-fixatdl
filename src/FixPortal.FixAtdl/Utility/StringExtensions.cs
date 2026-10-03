// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Globalization;
using System.Reflection;
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
    /// <remarks>Bare numeric strings are rejected even when they match a defined member's value
    /// ("3" would otherwise parse to <c>IsoCurrencyCode.ALL</c>): a name, never a number, is a
    /// legitimate enum value here.</remarks>
    public static T ParseAsEnum<T>(this string value)
        where T : struct, Enum
    {
        if (string.IsNullOrEmpty(value))
        {
            throw ThrowHelper.New<ArgumentNullException>(
                ExceptionContext,
                ErrorMessages.NullOrEmptyStringEnumParseFailure,
                typeof(T).Name
            );
        }

        bool isFlags = typeof(T).GetCustomAttribute<FlagsAttribute>() is not null;

        // Enum.Parse treats a comma-separated list as a flags combination for EVERY enum, [Flags] or
        // not: "AED,AFN" parses to 1 | 2 = 3, which is a *defined* member of IsoCurrencyCode (ALL) and
        // would slip past the IsDefined guard below as a silently different value. For a non-[Flags]
        // enum a comma is never a legitimate single member, so reject it before parsing.
        if (!isFlags && value.Contains(','))
        {
            throw ThrowHelper.New<ArgumentException>(
                ExceptionContext,
                ErrorMessages.InvalidValueEnumParseFailure,
                value,
                typeof(T).Name
            );
        }

        // Enum.Parse also accepts a bare numeric value, and "3" IS a defined member's value
        // (IsoCurrencyCode.ALL), so it slips past the IsDefined guard below as a silently different
        // currency; "036" parses to 36 and maps to the wrong member outright. A name, never a number,
        // is a legitimate enum value here — reject numeric strings for every enum, [Flags] or not.
        // NumberStyles.Integer matches Enum.Parse's numeric surface (optional surrounding whitespace
        // and a leading sign). An overflowing digit string fails TryParse and falls through to
        // Enum.Parse, whose OverflowException the catch below already maps to ArgumentException.
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
        {
            throw ThrowHelper.New<ArgumentException>(
                ExceptionContext,
                ErrorMessages.InvalidValueEnumParseFailure,
                value,
                typeof(T).Name
            );
        }

        T result;

        try
        {
            result = Enum.Parse<T>(value, true);
        }
        catch (Exception ex) when (ex is ArgumentException or OverflowException)
        {
            throw ThrowHelper.New<ArgumentException>(
                ExceptionContext,
                ex,
                ErrorMessages.InvalidValueEnumParseFailure,
                value,
                typeof(T).Name
            );
        }

        // Enum.Parse accepts a raw underlying numeric value even when it is not a defined member
        // (e.g. "999"), letting undefined enum values slip into the model. Reject anything that is
        // not a defined member — except for [Flags] enums, where a combined value (the bitwise OR
        // of several members) is legitimately not itself a single defined member.
        if (!Enum.IsDefined(result) && !isFlags)
        {
            throw ThrowHelper.New<ArgumentException>(
                ExceptionContext,
                ErrorMessages.InvalidValueEnumParseFailure,
                value,
                typeof(T).Name
            );
        }

        return result;
    }
}
