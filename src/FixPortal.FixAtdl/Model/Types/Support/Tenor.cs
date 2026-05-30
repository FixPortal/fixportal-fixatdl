// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

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

    /// <summary>
    /// Compares one tenor to see whether it is less than or equal to another tenor.
    /// </summary>
    /// <param name="lhs">Left hand side value.</param>
    /// <param name="rhs">Right hand side value.</param>
    /// <returns>True if <paramref name="lhs"/> is earlier than or equal to <paramref name="rhs"/>; otherwise, false.</returns>
    public static bool operator <=(Tenor lhs, Tenor rhs) => Compare(lhs, rhs) <= 0;

    /// <summary>
    /// Compares one tenor to see whether it is greater than or equal to another tenor.
    /// </summary>
    /// <param name="lhs">Left hand side value.</param>
    /// <param name="rhs">Right hand side value.</param>
    /// <returns>True if <paramref name="lhs"/> is later than or equal to <paramref name="rhs"/>; otherwise, false.</returns>
    public static bool operator >=(Tenor lhs, Tenor rhs) => Compare(lhs, rhs) >= 0;

    /// <summary>
    /// Compares one tenor to see whether it is earlier than another tenor.
    /// </summary>
    /// <param name="lhs">Left hand side value.</param>
    /// <param name="rhs">Right hand side value.</param>
    /// <returns>True if <paramref name="lhs"/> is earlier than <paramref name="rhs"/>; otherwise, false.</returns>
    public static bool operator <(Tenor lhs, Tenor rhs) => Compare(lhs, rhs) < 0;

    /// <summary>
    /// Compares one tenor to see whether it is later than another tenor.
    /// </summary>
    /// <param name="lhs">Left hand side value.</param>
    /// <param name="rhs">Right hand side value.</param>
    /// <returns>True if <paramref name="lhs"/> is later than <paramref name="rhs"/>; otherwise, false.</returns>
    public static bool operator >(Tenor lhs, Tenor rhs) => Compare(lhs, rhs) > 0;

    /// <summary>
    /// Compares two tenor values for equality.
    /// </summary>
    /// <param name="lhs">Left hand side value.</param>
    /// <param name="rhs">Right hand side value.</param>
    /// <returns>True if both the tenor type and offset are equal; otherwise, false.</returns>
    public static bool operator ==(Tenor lhs, Tenor rhs) => lhs.Offset == rhs.Offset && lhs.TenorType == rhs.TenorType;

    /// <summary>
    /// Compares two tenor values for inequality.
    /// </summary>
    /// <param name="lhs">Left hand side value.</param>
    /// <param name="rhs">Right hand side value.</param>
    /// <returns>True if the tenor values differ; otherwise, false.</returns>
    public static bool operator !=(Tenor lhs, Tenor rhs) => lhs.Offset != rhs.Offset || lhs.TenorType != rhs.TenorType;

    /// <summary>
    /// Determines whether the supplied object is equal to this tenor value.
    /// </summary>
    /// <param name="obj">The object to compare with this instance.</param>
    /// <returns><see langword="true"/> if the supplied object is a matching <see cref="Tenor"/>; otherwise, <see langword="false"/>.</returns>
    public override readonly bool Equals(object? obj)
    {
        if (obj is not Tenor tenor)
        {
            return false;
        }

        return this == tenor;
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

    /// <summary>
    /// Parses the supplied FIX tenor string.
    /// </summary>
    /// <param name="value">The tenor string to parse.</param>
    /// <returns>The parsed <see cref="Tenor"/> value.</returns>
    public static Tenor Parse(string value)
    {
        Tenor result = new();

        if (value.Length >= 2)
        {
            result.TenorType = value[0] switch
            {
                'D' => TenorTypeValue.Day,
                'W' => TenorTypeValue.Week,
                'M' => TenorTypeValue.Month,
                'Y' => TenorTypeValue.Year,
                _ => result.TenorType
            };

            string number = value[1..];

            try
            {
                result.Offset = Convert.ToInt32(number, CultureInfo.InvariantCulture);

                if (result.TenorType != TenorTypeValue.Invalid)
                {
                    return result;
                }
            }
            catch (Exception ex) when (ex is FormatException or OverflowException)
            {
                throw ThrowHelper.New<ArgumentException>(ExceptionContext, ex, ErrorMessages.InvalidTenorValue, value);
            }
        }

        throw ThrowHelper.New<ArgumentException>(ExceptionContext, ErrorMessages.InvalidTenorValue, value);
    }

    /// <summary>
    /// Returns the FIX string representation of the tenor.
    /// </summary>
    /// <returns>The tenor encoded as a FIX string.</returns>
    public override readonly string ToString()
    {
        return TenorType switch
        {
            TenorTypeValue.Day => string.Format(CultureInfo.InvariantCulture, "D{0}", Offset),
            TenorTypeValue.Week => string.Format(CultureInfo.InvariantCulture, "W{0}", Offset),
            TenorTypeValue.Month => string.Format(CultureInfo.InvariantCulture, "M{0}", Offset),
            TenorTypeValue.Year => string.Format(CultureInfo.InvariantCulture, "Y{0}", Offset),
            // A default/unparsed Tenor (TenorType=Invalid) must not be silently serialized as the
            // syntactically-valid-but-wrong wire value "Y0"; surface it instead of corrupting the wire.
            _ => throw new InvalidOperationException($"Cannot serialize a Tenor with an invalid tenor type (offset {Offset})."),
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
    public int CompareTo(object? obj)
    {
        // Null references are by definition less than the current instance.
        if (obj == null)
        {
            return 1;
        }

        if (obj is not Tenor rhs)
        {
            throw ThrowHelper.New<ArgumentException>(this, InternalErrors.UnexpectedArgumentType, obj.GetType().FullName!, GetType().FullName!);
        }

        if (rhs == this)
        {
            return 0;
        }

        return Compare(this, rhs);
    }

    #endregion

    private static int Compare(Tenor lhs, Tenor rhs)
    {
        // Same unit: compare offsets exactly (D5 vs D7).
        if (lhs.TenorType == rhs.TenorType)
        {
            return lhs.Offset.CompareTo(rhs.Offset);
        }

        // Different units (e.g. D7 vs M1): order deterministically by approximate duration so that
        // Min/Max range validation in Tenor_t.ValidateValue cannot throw NotSupportedException on a
        // mixed-unit bound. Note equality (operator ==) remains exact (unit + offset).
        return lhs.ApproximateDays().CompareTo(rhs.ApproximateDays());
    }

    /// <summary>
    /// Approximate duration of this tenor expressed in days, used solely to give cross-unit
    /// comparisons a deterministic total order (D=1, W=7, M=30, Y=365).
    /// </summary>
    private readonly double ApproximateDays()
    {
        return TenorType switch
        {
            TenorTypeValue.Day => Offset,
            TenorTypeValue.Week => Offset * 7.0,
            TenorTypeValue.Month => Offset * 30.0,
            TenorTypeValue.Year => Offset * 365.0,
            _ => double.NaN,
        };
    }
}
