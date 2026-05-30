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
using FixPortal.FixAtdl.Model.Reference;
using FixPortal.FixAtdl.Model.Types.Support;
using FixPortal.FixAtdl.Resources;
using FixPortal.FixAtdl.Utility;

namespace FixPortal.FixAtdl.Validation;

/// <summary>
/// Provides value conversion for <see cref="FixPortal.FixAtdl.Model.Elements.Edit_t"/> evaluation.
/// </summary>
public static class EditValueConverter
{
    private static readonly string ExceptionContext = typeof(EditValueConverter).FullName!;

    /// <summary>
    /// Attempts to convert the second parameter value to a comparable type of the first parameter.
    /// </summary>
    /// <param name="typeInstanceToMatch">Instance of a the target comparable type.</param>
    /// <param name="value">Value to convert.</param>
    /// <returns>Converted value as an <see cref="IComparable"/>.</returns>
    /// <exception cref="InvalidCastException">Thrown if the value cannot be converted to the target type.</exception>
    /// <exception cref="FormatException">Thrown if the value cannot be converted into a valid numeric type.</exception>
    public static IComparable ConvertToComparableType(object typeInstanceToMatch, string value)
    {
        // If we don't have a valid type to convert to, then best leave the value alone.
        if (typeInstanceToMatch == null)
        {
            return value;
        }

        // A null operand is a missing Edit value, not a zero. The numeric Convert.To* paths would
        // silently coerce null to 0 (masking a missing right-hand side and making comparisons pass
        // spuriously) while the enum/MonthYear/Tenor paths would NRE. Reject it consistently with a
        // domain exception, matching ConvertToBool's null handling (O-G2).
        if (value == null)
        {
            throw ThrowHelper.New<InvalidFieldValueException>(ExceptionContext, ErrorMessages.IllegalUseOfNullError);
        }

        string? type = typeInstanceToMatch.GetType().FullName;

        try
        {
            return type switch
            {
                "System.Decimal" => Convert.ToDecimal(value, CultureInfo.InvariantCulture),
                "System.Boolean" => ConvertToBool(value),
                "System.Int32" => Convert.ToInt32(value, CultureInfo.InvariantCulture),
                "System.UInt32" => Convert.ToUInt32(value, CultureInfo.InvariantCulture),
                "System.Char" => Convert.ToChar(value),
                "System.DateTime" => FixDateTime.Parse(value, CultureInfo.InvariantCulture),
                "System.String" => value,
                "FixPortal.FixAtdl.Model.Reference.IsoCountryCode" => value.ParseAsEnum<IsoCountryCode>(),
                "FixPortal.FixAtdl.Model.Reference.IsoCurrencyCode" => value.ParseAsEnum<IsoCurrencyCode>(),
                "FixPortal.FixAtdl.Model.Reference.IsoLanguageCode" => value.ParseAsEnum<IsoLanguageCode>(),
                "FixPortal.FixAtdl.Model.Types.Support.MonthYear" => MonthYear.Parse(value),
                "FixPortal.FixAtdl.Model.Types.Support.Tenor" => Tenor.Parse(value),
                "FixPortal.FixAtdl.Model.Controls.Support.EnumState" => value,
                _ => throw ThrowHelper.New<InvalidCastException>(ExceptionContext, ErrorMessages.DataConversionError1, value, type),
            };
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            // Translate raw Convert.* conversion failures into a domain InvalidFieldValueException,
            // matching the boundary established elsewhere rather than leaking a raw BCL exception (M4).
            throw ThrowHelper.New<InvalidFieldValueException>(ExceptionContext, ex, ErrorMessages.DataConversionError1, value, type);
        }
    }

    private static bool ConvertToBool(string value)
    {
        if (value == null)
        {
            throw ThrowHelper.New<InvalidFieldValueException>(ExceptionContext, ErrorMessages.IllegalUseOfNullError);
        }

        return value.ToUpperInvariant() switch
        {
            "N" => false,
            "Y" => true,
            // Guard the bool.Parse so an unparseable value raises a domain error instead of a raw
            // FormatException (and use the invariant upper-case above for culture safety).
            _ => bool.TryParse(value, out bool result)
                ? result
                : throw ThrowHelper.New<InvalidFieldValueException>(ExceptionContext, ErrorMessages.DataConversionError1, value, "System.Boolean"),
        };
    }
}
