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
/// Represents a FIX/FIXatdl MonthYear value.
/// </summary>
public struct MonthYear : IComparable
{
    private const string ExceptionContext = "MonthYear";

    private ushort Year;
    private ushort Month;
    private ushort? Day;
    private ushort? Week;

    /// <summary>
    /// Provides the string representation of this MonthYear instance.
    /// </summary>
    /// <returns>MonthYear as a string.</returns>
    public override readonly string ToString()
    {
        string suffix = Week != null ? string.Format(CultureInfo.InvariantCulture, "w{0}", Week) : Day != null ? string.Format(CultureInfo.InvariantCulture, "{0:00}", Day) : string.Empty;

        return string.Format(CultureInfo.InvariantCulture, "{0:0000}{1:00}{2}", Year, Month, suffix);
    }

    /// <summary>
    /// Compares two MonthYear values for equality.
    /// </summary>
    /// <param name="lhs">Left hand side value.</param>
    /// <param name="rhs">Right hand side value.</param>
    /// <returns>True if the day, month and year values of the two operands are the same; false otherwise.</returns>
    public static bool operator ==(MonthYear lhs, MonthYear rhs) => lhs.Year == rhs.Year && lhs.Month == rhs.Month && lhs.Day == rhs.Day && lhs.Week == rhs.Week;

    /// <summary>
    /// Compares two MonthYear values for inequality.
    /// </summary>
    /// <param name="lhs">Left hand side value.</param>
    /// <param name="rhs">Right hand side value.</param>
    /// <returns>True if any of the day, month and year values of the two operands are not the same; false otherwise.</returns>
    public static bool operator !=(MonthYear lhs, MonthYear rhs) => !(lhs == rhs);

    /// <summary>
    /// Compares the supplied object for equality with this MonthYear instance.
    /// </summary>
    /// <param name="obj">Object to compare this instance with.</param>
    /// <returns>True if the supplied object is a MonthYear, and the day, month and year values of the two are the same; false otherwise.</returns>
    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        return (MonthYear)obj == this;
    }

    /// <summary>
    /// Serves as a hash function for this type.  Overridden because Equals(object) is overridden.
    /// </summary>
    /// <returns>A hash code for the current Object.</returns>
    /// <remarks>The value 251 is used here because it is a prime number, helpful for generating unique hash values.</remarks>
    public override readonly int GetHashCode()
    {
        unchecked // No issue with int overflow
        {
            int hashCode = (Year * 251 + Month) * 251;

            hashCode = Day != null ? (hashCode + (ushort)Day) * 251 : hashCode * 251;
            hashCode = Week != null ? (hashCode + (ushort)Week) * 251 : hashCode * 251;

            return hashCode;
        }
    }

    /// <summary>
    /// Compares one MonthYear value to see whether it is less than or equal to a second MonthYear value.
    /// </summary>
    /// <param name="lhs">Left hand side value.</param>
    /// <param name="rhs">Right hand side value.</param>
    /// <returns>True if the left hand operand occurs before or at the same time as the right hand operand; false otherwise.</returns>
    public static bool operator <=(MonthYear lhs, MonthYear rhs) => Compare(lhs, rhs) <= 0;

    /// <summary>
    /// Compares one MonthYear value to see whether it is greater than or equal to a second MonthYear value.
    /// </summary>
    /// <param name="lhs">Left hand side value.</param>
    /// <param name="rhs">Right hand side value.</param>
    /// <returns>True if the left hand operand occurs at the same time or after the right hand operand; false otherwise.</returns>
    public static bool operator >=(MonthYear lhs, MonthYear rhs) => Compare(lhs, rhs) >= 0;

    /// <summary>
    /// Compares one MonthYear value to see whether it is earlier than a second MonthYear value.
    /// </summary>
    /// <param name="lhs">Left hand side value.</param>
    /// <param name="rhs">Right hand side value.</param>
    /// <returns>True if the left hand operand occurs before the right hand operand; otherwise, false.</returns>
    public static bool operator <(MonthYear lhs, MonthYear rhs) => Compare(lhs, rhs) < 0;

    /// <summary>
    /// Compares one MonthYear value to see whether it is later than a second MonthYear value.
    /// </summary>
    /// <param name="lhs">Left hand side value.</param>
    /// <param name="rhs">Right hand side value.</param>
    /// <returns>True if the left hand operand occurs after the right hand operand; otherwise, false.</returns>
    public static bool operator >(MonthYear lhs, MonthYear rhs) => Compare(lhs, rhs) > 0;

    /// <summary>
    /// Attempts to parse the supplied string into a MonthYear value.
    /// </summary>
    /// <param name="value">String representation of MonthYear value to parse.</param>
    /// <returns>The MonthYear value that corresponds to the supplied string.</returns>
    /// <exception cref="ArgumentException">Thrown if the supplied string did not represent a valid MonthYear value.</exception>
    public static MonthYear Parse(string value)
    {
        MonthYear result = new();

        bool suffixValid = false;

        // Note: This is probably better done with a regex at some point.
        if (value.Length == 8)
        {
            string suffix = value.Substring(6, 2);

            if (suffix[0] == 'w')
            {
                result.Week = ValidateRange(suffix[1].ToString(), 1, 5);
            }
            else
            {
                result.Day = ValidateRange(suffix, 1, 31);
            }

            suffixValid = true;
        }

        if (value.Length == 6 || value.Length == 8 && suffixValid)
        {
            result.Year = ValidateRange(value[..4], 0, 9999);
            result.Month = ValidateRange(value.Substring(4, 2), 1, 12);

            // Reject calendar-impossible days (e.g. 20260230 = 30 Feb). Year 0 has no Gregorian
            // calendar, so DateTime.DaysInMonth cannot be consulted and the day check is skipped.
            if (result.Day != null && result.Year >= 1 && result.Day.Value > DateTime.DaysInMonth(result.Year, result.Month))
            {
                throw ThrowHelper.New<ArgumentException>(ExceptionContext, ErrorMessages.InvalidMonthYearValue, value);
            }

            return result;
        }

        throw ThrowHelper.New<ArgumentException>(ExceptionContext, ErrorMessages.InvalidMonthYearValue, value);
    }

    private static ushort ValidateRange(string value, int lowerBound, int upperBound)
    {
        try
        {
            ushort numValue = Convert.ToUInt16(value, CultureInfo.InvariantCulture);

            if (numValue >= lowerBound && numValue <= upperBound)
            {
                return numValue;
            }

            throw ThrowHelper.New<ArgumentException>(ExceptionContext, ErrorMessages.InvalidMonthYearValue, value);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            throw ThrowHelper.New<ArgumentException>(ExceptionContext, ex, ErrorMessages.InvalidMonthYearValue, value);
        }
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

        if (obj is not MonthYear rhs)
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

    private static int Compare(MonthYear lhs, MonthYear rhs)
    {
        if (lhs.Year != rhs.Year)
        {
            return lhs.Year.CompareTo(rhs.Year);
        }

        if (lhs.Month != rhs.Month)
        {
            return lhs.Month.CompareTo(rhs.Month);
        }

        // Same year and month: order by approximate intra-month position so that mixed suffixes
        // (day-qualified vs week-qualified, or a suffix vs no suffix) compare deterministically
        // instead of throwing NotSupportedException when reached via the </>=/<= operators that
        // MonthYear_t.ValidateValue uses for Min/Max range checks.
        return lhs.IntraMonthOrdinal().CompareTo(rhs.IntraMonthOrdinal());
    }

    /// <summary>
    /// Approximate position within the month used solely to give mixed-suffix comparisons a
    /// deterministic total order: no suffix sorts first (whole month), a day maps to its 1-31
    /// value, and a week maps to an approximate day (w1..w5 -> 7..35).
    /// </summary>
    private readonly int IntraMonthOrdinal()
    {
        if (Day != null)
        {
            return Day.Value;
        }

        if (Week != null)
        {
            return Week.Value * 7;
        }

        return 0;
    }
}
