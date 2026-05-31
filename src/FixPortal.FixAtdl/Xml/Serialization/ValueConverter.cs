// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Globalization;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Resources;
using ThrowHelper = FixPortal.FixAtdl.Diagnostics.ThrowHelper;

namespace FixPortal.FixAtdl.Xml.Serialization;

/// <summary>
/// Converts XML text values into CLR and FIXatdl types.
/// </summary>
public static class ValueConverter
{
    /// <summary>
    /// Gets the exception context used when reporting conversion failures.
    /// </summary>
    public static readonly string ExceptionContext = typeof(ValueConverter).Name;

    /// <summary>
    /// Converts the supplied text value to the specified target type.
    /// </summary>
    /// <typeparam name="T">The requested target type.</typeparam>
    /// <param name="value">The text value to convert.</param>
    /// <returns>The converted value.</returns>
    public static T ConvertTo<T>(string value)
    {
        return (T)ConvertTo(value, typeof(T));
    }

    /// <summary>
    /// Converts the supplied text value to the specified target type.
    /// </summary>
    /// <param name="value">The text value to convert.</param>
    /// <param name="targetType">The requested target type.</param>
    /// <returns>The converted value.</returns>
    public static object ConvertTo(string value, Type targetType)
    {
        // A Nullable<T> target (e.g. int?) has a FullName of "System.Nullable`1[[...]]" that matches
        // none of the cases below and would fall through to InternalErrorException. Unwrap to the
        // underlying type so the conversion is driven by T.
        targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        switch (targetType.FullName)
        {
            case "System.String":
                return value;

            case "System.Char":
                if (value.Length == 1)
                {
                    return Convert.ToChar(value);
                }
                else
                {
                    throw ThrowHelper.New<InvalidFieldValueException>(ExceptionContext, ErrorMessages.InvalidCharValue, value);
                }

            case "System.Boolean":
                try
                {
                    return bool.Parse(value);
                }
                catch (FormatException ex)
                {
                    throw ThrowHelper.New<InvalidFieldValueException>(ExceptionContext, ex, ErrorMessages.DataConversionError1, value, targetType.Name);
                }

            case "System.Int32":
                return ParseOrThrow(value, targetType, v => Convert.ToInt32(v, CultureInfo.InvariantCulture));

            case "System.Decimal":
                return ParseOrThrow(value, targetType, v => Convert.ToDecimal(v, CultureInfo.InvariantCulture));

            case "System.DateTime":
                {

                    if (!FixDateTime.TryParse(value, CultureInfo.InvariantCulture, out DateTime result))
                    {
                        throw ThrowHelper.New<InvalidFieldValueException>(ExceptionContext, ErrorMessages.InvalidDateOrTimeValue, value);
                    }

                    return result;
                }

            case "FixPortal.FixAtdl.Fix.FixTag":
                return ParseOrThrow(value, targetType, v => new FixTag(Convert.ToInt32(v, CultureInfo.InvariantCulture)));

            default:
                if (targetType.FullName!.StartsWith("FixPortal.FixAtdl.Model.Controls.InitValue", StringComparison.Ordinal))
                {
                    return value;
                }
                else
                {
                    throw ThrowHelper.New<InternalErrorException>(ExceptionContext, InternalErrors.UnrecognisedAttributeType, targetType.FullName!);
                }
        }
    }

    private static object ParseOrThrow(string value, Type targetType, Func<string, object> convert)
    {
        try
        {
            return convert(value);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            throw ThrowHelper.New<InvalidFieldValueException>(ExceptionContext, ex, ErrorMessages.DataConversionError1, value, targetType.Name);
        }
    }
}
