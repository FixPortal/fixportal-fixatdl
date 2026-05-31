// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Globalization;
using FixPortal.FixAtdl.Validation;

namespace FixPortal.FixAtdl.Model.Types.Support;

/// <summary>
/// Base class for all date and time related UTC-prefixed FIXatdl types.
/// </summary>
public abstract class UTCDateTimeTypeBase : DateTimeTypeBase
{
    #region AtdlReferenceType<string> Overrides

    /// <summary>
    /// Validates the supplied value in terms of the parameters constraints (e.g., MinValue, MaxValue, etc.).
    /// </summary>
    /// <param name="value">Value to validate, may be null in which case no validation is applied.</param>
    /// <param name="isRequired">Set to true to check that this parameter is non-null.</param>
    /// <returns>ValidationResult indicating whether the supplied value is valid.</returns>
    /// <remarks>DateTime.MaxValue (a date and time at the end of the year 9999) is used to indicate an invalid date or time.</remarks>
    protected override ValidationResult ValidateValue(DateTime? value, bool isRequired)
    {
        return base.ValidateValue(GetAdjustedValue(value), isRequired);
    }

    /// <summary>
    /// <see cref="DateTimeStyles"/> applied when parsing a UTC-family wire value. The value is assumed to
    /// be UTC when no offset is present (FIX UTCTimestamp is by definition UTC) and any explicit offset is
    /// normalised to UTC (<see cref="DateTimeStyles.AdjustToUniversal"/>), so the result is canonically
    /// <see cref="DateTimeKind.Utc"/> and host-offset-independent. Replaces the former hardcoded
    /// AssumeUniversal-only parse, which produced a Kind=Local value (M1).
    /// </summary>
    protected override DateTimeStyles WireParseStyles =>
        DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;

    /// <summary>
    /// Converts the supplied value to a string, as might be used on the FIX wire.
    /// </summary>
    /// <param name="value">Value to convert, may be null.</param>
    /// <returns>If input value is not null, returns value converted to a string; null otherwise.</returns>
    protected override string ConvertToWireValueFormat(DateTime? value)
    {
        string format = GetDateTimeFormatStrings()[0];
        DateTime? adjustedValue = GetAdjustedValue(value);

        return adjustedValue != null ? ((DateTime)adjustedValue).ToString(format, CultureInfo.InvariantCulture) : null!;
    }

    #endregion

    private static DateTime? GetAdjustedValue(DateTime? value)
    {
        if (value == null)
        {
            return null;
        }

        DateTime dateTime = value.Value;

        // Adjust according to the source Kind. Values parsed from the wire arrive as Utc (AssumeUniversal)
        // and need no change. Values set from a control via ConvertToNativeType arrive as Unspecified and
        // represent a UTC-prefixed type, so they are taken to already be UTC rather than shifted by the
        // host offset (the previous unconditional ToUniversalTime() corrupted those on a non-UTC host).
        // A genuinely Local value is still converted.
        return dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
        };
    }
}
