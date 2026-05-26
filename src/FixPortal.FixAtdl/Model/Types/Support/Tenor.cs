// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;
using System.Globalization;
using FixPortal.FixAtdl.Resources;
using ThrowHelper = FixPortal.FixAtdl.Diagnostics.ThrowHelper;

namespace FixPortal.FixAtdl.Model.Types.Support;

/// <summary>
/// Represents the Tenor type within FIX/FIXatdl.
/// </summary>
public struct Tenor : IComparable
{
    private enum TenorTypeValue
    {
        Invalid = 0,
        Day,
        Week,
        Month,
        Year
    }

    private const string ExceptionContext = "Tenor";

    private int Offset;
    private TenorTypeValue TenorType;

    public static bool operator <=(Tenor lhs, Tenor rhs)
    {
        if (lhs.TenorType != rhs.TenorType)
        {
            throw ThrowHelper.New<NotSupportedException>(ExceptionContext, ErrorMessages.UnsupportedComparisonOperation, lhs, rhs);
        }

        return lhs.Offset <= rhs.Offset;
    }

    public static bool operator >=(Tenor lhs, Tenor rhs)
    {
        if (lhs.TenorType != rhs.TenorType)
        {
            throw ThrowHelper.New<NotSupportedException>(ExceptionContext, ErrorMessages.UnsupportedComparisonOperation, lhs, rhs);
        }

        return lhs.Offset >= rhs.Offset;
    }

    public static bool operator ==(Tenor lhs, Tenor rhs) => lhs.Offset == rhs.Offset && lhs.TenorType == rhs.TenorType;

    public static bool operator !=(Tenor lhs, Tenor rhs) => lhs.Offset != rhs.Offset || lhs.TenorType != rhs.TenorType;

    public override readonly bool Equals(object? obj)
    {
        if (obj == null || obj is not Tenor)
        {
            return false;
        }

        return this == (Tenor)obj;
    }

    /// <summary>
    /// Serves as a hash function for this type.  Overridden because Equals(object) is overridden.
    /// </summary>
    /// <returns>A hash code for the current Object.</returns>
    /// <remarks>The value 251 is used here because it is a prime number, helpful for generating unique hash values.</remarks>
    public override readonly int GetHashCode()
    {
        return Offset * 251 + (int)TenorType;
    }

    public static Tenor Parse(string value)
    {
        Tenor result = new();

        if (value.Length >= 2)
        {
            switch (value[0])
            {
                case 'D':
                    result.TenorType = TenorTypeValue.Day;
                    break;

                case 'W':
                    result.TenorType = TenorTypeValue.Week;
                    break;

                case 'M':
                    result.TenorType = TenorTypeValue.Month;
                    break;

                case 'Y':
                    result.TenorType = TenorTypeValue.Year;
                    break;
            }

            string number = value[1..];

            try
            {
                result.Offset = Convert.ToInt32(number, CultureInfo.InvariantCulture);

                if (result.TenorType != TenorTypeValue.Invalid)
                {
                    return result;
                }
            }
            catch (FormatException ex)
            {
                throw ThrowHelper.New<ArgumentException>(ExceptionContext, ex, ErrorMessages.InvalidTenorValue, value);
            }
        }

        throw ThrowHelper.New<ArgumentException>(ExceptionContext, ErrorMessages.InvalidTenorValue, value);
    }

    public override readonly string ToString()
    {
        return TenorType switch
        {
            TenorTypeValue.Day => string.Format(CultureInfo.InvariantCulture, "D{0}", Offset),
            TenorTypeValue.Week => string.Format(CultureInfo.InvariantCulture, "W{0}", Offset),
            TenorTypeValue.Month => string.Format(CultureInfo.InvariantCulture, "M{0}", Offset),
            _ => string.Format(CultureInfo.InvariantCulture, "Y{0}", Offset),
        };
    }

    #region IComparable Members

    /// <summary>
    /// Compares the current instance with another object of the same type and returns an integer that indicates 
    /// whether the current instance precedes, follows, or occurs in the same position in the sort order as the 
    /// other object.
    /// </summary>
    /// <param name="obj">An object to compare with this instance.</param>
    /// <returns>A value that indicates the relative order of the objects being compared. The return value has these meanings:
    /// <list type="bullet">
    /// <item><description>Less than zero - this instance precedes obj in the sort order.</description></item>
    /// <item><description>Zero - this instance occurs in the same position in the sort order as obj.</description></item>
    /// <item><description>Greater than zero - this instance follows obj in the sort order.</description></item>
    /// </list></returns>
    public readonly int CompareTo(object? obj)
    {
        // Null references are by definition less than the current instance.
        if (obj == null)
        {
            return 1;
        }

        if (obj is not Tenor)
        {
            throw ThrowHelper.New<ArgumentException>(this, InternalErrors.UnexpectedArgumentType, obj.GetType().FullName!, GetType().FullName!);
        }

        Tenor rhs = (Tenor)obj;

        if (rhs == this)
        {
            return 0;
        }

        return rhs <= this ? 1 : -1;
    }

    #endregion
}
