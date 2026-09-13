// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

namespace FixPortal.FixAtdl.Fix;

/// <summary>
/// Provides definitions of the different date and time formats supported by FIX in .NET DateTime.ToString()-
/// compatible format.
/// </summary>
public static class FixDateTimeFormat
{
    /// <summary>Date and time (no milliseconds).</summary>
    public static readonly string FixDateTime = "yyyyMMdd-HH:mm:ss";

    /// <summary>Date and time (with milliseconds).</summary>
    public static readonly string FixDateTimeMs = "yyyyMMdd-HH:mm:ss.fff";

    /// <summary>Time only (no milliseconds).</summary>
    public static readonly string FixTimeOnly = "HH:mm:ss";

    /// <summary>Time only (with milliseconds).</summary>
    public static readonly string FixTimeOnlyMs = "HH:mm:ss.fff";

    /// <summary>Date only.</summary>
    public static readonly string FixDateOnly = "yyyyMMdd";

    /// <summary>Date and time with appended time zone information.</summary>
    public static readonly string FixDateTimeWithTz = "yyyyMMdd-HH:mm:ssK";

    /// <summary>Date and time with appended time zone information.</summary>
    public static readonly string FixTimeOnlyWithTz = "HH:mm:ssK";

    /// <summary>Time only with an optional-seconds UTC designator.</summary>
    public static readonly string FixTimeOnlyMinutesWithUtcDesignator = "HH:mm'Z'";

    /// <summary>Time only with an optional-seconds bare-hour timezone offset.</summary>
    public static readonly string FixTimeOnlyMinutesWithHourOffset = "HH:mmzz";

    /// <summary>Time only with an optional-seconds timezone offset including minutes.</summary>
    public static readonly string FixTimeOnlyMinutesWithMinuteOffset = "HH:mmK";

    /// <summary>Time only with fractional seconds and no offset. Internal: needed by the time-only
    /// classification (R04) so a truncated high-precision fraction still matches a format; the public
    /// parse path reaches the same spellings through its loose fallback.</summary>
    internal static readonly string FixTimeOnlyFractional = "HH:mm:ss.FFFFFFF";

    /// <summary>Time only with fractional seconds and an appended bare-hour timezone offset.</summary>
    public static readonly string FixTimeOnlyFractionalWithHourOffset = "HH:mm:ss.FFFFFFFzz";

    /// <summary>Time only with fractional seconds and an appended timezone offset including minutes.</summary>
    public static readonly string FixTimeOnlyFractionalWithMinuteOffset = "HH:mm:ss.FFFFFFFK";

    /// <summary>Time only with whole seconds and an appended bare-hour timezone offset.</summary>
    public static readonly string FixTimeOnlyWithHourOffset = "HH:mm:sszz";

    /// <summary>Date and time with optional seconds and a UTC designator.</summary>
    public static readonly string FixDateTimeMinutesWithUtcDesignator = "yyyyMMdd-HH:mm'Z'";

    /// <summary>Date and time with optional seconds and a bare-hour timezone offset.</summary>
    public static readonly string FixDateTimeMinutesWithHourOffset = "yyyyMMdd-HH:mmzz";

    /// <summary>Date and time with optional seconds and a timezone offset including minutes.</summary>
    public static readonly string FixDateTimeMinutesWithMinuteOffset = "yyyyMMdd-HH:mmK";

    /// <summary>Date and time with fractional seconds and no offset. Internal: parse-only entry so a
    /// truncated high-precision fraction still matches a format; emission stays on the seconds/ms pair.</summary>
    internal static readonly string FixDateTimeFractional = "yyyyMMdd-HH:mm:ss.FFFFFFF";

    /// <summary>Date and time with fractional seconds and a bare-hour timezone offset.</summary>
    public static readonly string FixDateTimeFractionalWithHourOffset = "yyyyMMdd-HH:mm:ss.FFFFFFFzz";

    /// <summary>Date and time with fractional seconds and a timezone offset including minutes.</summary>
    public static readonly string FixDateTimeFractionalWithMinuteOffset = "yyyyMMdd-HH:mm:ss.FFFFFFFK";

    /// <summary>Date and time with whole seconds and an appended bare-hour timezone offset.</summary>
    public static readonly string FixDateTimeWithHourOffset = "yyyyMMdd-HH:mm:sszz";

    internal static readonly string[] FormatsArray =
    [
        FixDateTime,
        FixDateTimeMs,
        FixTimeOnly,
        FixTimeOnlyMs,
        FixDateOnly,
        FixDateTimeWithTz,
        FixTimeOnlyWithTz,
        FixTimeOnlyMinutesWithUtcDesignator,
        FixTimeOnlyMinutesWithHourOffset,
        FixTimeOnlyMinutesWithMinuteOffset,
        FixTimeOnlyWithHourOffset,
        FixTimeOnlyFractionalWithHourOffset,
        FixTimeOnlyFractionalWithMinuteOffset,
        FixDateTimeMinutesWithUtcDesignator,
        FixDateTimeMinutesWithHourOffset,
        FixDateTimeMinutesWithMinuteOffset,
        FixDateTimeWithHourOffset,
        FixDateTimeFractionalWithHourOffset,
        FixDateTimeFractionalWithMinuteOffset,
    ];

    // The date-less (time-of-day-only) subset of FormatsArray, used to classify a bound / Edit
    // operand / clock value as a time-of-day constraint rather than a full date+time (R04).
    internal static readonly string[] TimeOnlyFormatsArray =
    [
        FixTimeOnly,
        FixTimeOnlyMs,
        FixTimeOnlyFractional,
        FixTimeOnlyWithTz,
        FixTimeOnlyMinutesWithUtcDesignator,
        FixTimeOnlyMinutesWithHourOffset,
        FixTimeOnlyMinutesWithMinuteOffset,
        FixTimeOnlyFractionalWithHourOffset,
        FixTimeOnlyFractionalWithMinuteOffset,
        FixTimeOnlyWithHourOffset,
    ];

    /// <summary>
    /// Gets all the FIX date/time formats.
    /// </summary>
    /// <remarks>The backing array is never handed out: callers receive a read-only wrapper so the
    /// parser's own format table cannot be mutated through this property.</remarks>
    public static System.Collections.Generic.IReadOnlyList<string> AllFormats { get; } = Array.AsReadOnly(FormatsArray);
}
