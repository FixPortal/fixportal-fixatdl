// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;
using System.Globalization;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Resources;
using ThrowHelper = FixPortal.FixAtdl.Diagnostics.ThrowHelper;

namespace FixPortal.FixAtdl.Xml.Serialization;

public class ValueConverter
{
    public static readonly string ExceptionContext = typeof(ValueConverter).Name;

    public static T ConvertTo<T>(string value)
    {
        return (T)ConvertTo(value, typeof(T));
    }

    public static object ConvertTo(string value, System.Type targetType)
    {
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
                return bool.Parse(value);

            case "System.Int32":
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);

            case "System.Decimal":
                return Convert.ToDecimal(value, CultureInfo.InvariantCulture);

            case "System.DateTime":
                {

                    if (!FixDateTime.TryParse(value, CultureInfo.InvariantCulture, out DateTime result))
                    {
                        throw ThrowHelper.New<InvalidFieldValueException>(ExceptionContext, ErrorMessages.InvalidDateOrTimeValue, value);
                    }

                    return result;
                }

            case "FixPortal.FixAtdl.Fix.FixTag":
                return new FixTag(Convert.ToInt32(value, CultureInfo.InvariantCulture));

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
}
