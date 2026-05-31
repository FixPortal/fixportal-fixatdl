// FP Enhancement: 2026-05-31 — NodaTime-backed holder for Clock_t initValue (batch 5, C1/C2).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Diagnostics;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Resources;
using NodaTime;
using NodaTime.Text;

namespace FixPortal.FixAtdl.Model.Controls;

/// <summary>
/// Parse-layer holder for a <see cref="Clock_t"/> <c>initValue</c>. A clock initValue is either a
/// time-of-day (e.g. <c>08:00:00</c>) or a full local date-and-time (e.g. <c>20260601-09:30:00</c>),
/// expressed in the control's <c>localMktTz</c> zone. Holding it as a NodaTime <see cref="LocalTime"/>
/// or <see cref="LocalDateTime"/> — rather than a BCL <see cref="System.DateTime"/> — avoids injecting a
/// spurious "today" date into a time-only value (the C2 contamination) and keeps the value zone-agnostic
/// until <see cref="Clock_t"/> resolves it against the market zone.
/// </summary>
/// <remarks>
/// The single-<see cref="string"/> constructor is required: the reflective deserializer routes the
/// <c>initValue</c> attribute here via the <c>ValueConverter</c> "InitValue*" escape hatch, which passes
/// the raw XML text to a one-arg-string constructor. The type name MUST begin with <c>InitValue</c>.
/// </remarks>
public sealed class InitValueClock
{
    private const string ExceptionContext = "InitValueClock";

    // Time-only is tried before date+time; the format sets are disjoint (a time-only string never
    // matches the date+time pattern and vice-versa), so order is for clarity only. NodaTime year token
    // is 'uuuu' (absolute year), not BCL 'yyyy'.
    private static readonly LocalTimePattern[] TimePatterns =
    [
        LocalTimePattern.CreateWithInvariantCulture("HH:mm:ss.fff"),
        LocalTimePattern.CreateWithInvariantCulture("HH:mm:ss"),
    ];

    private static readonly LocalDateTimePattern[] DateTimePatterns =
    [
        LocalDateTimePattern.CreateWithInvariantCulture("uuuuMMdd-HH:mm:ss.fff"),
        LocalDateTimePattern.CreateWithInvariantCulture("uuuuMMdd-HH:mm:ss"),
    ];

    /// <summary>
    /// Parses the supplied raw initValue text into a time-of-day or a local date-and-time.
    /// </summary>
    /// <param name="raw">The raw <c>initValue</c> attribute text.</param>
    /// <exception cref="InvalidFieldValueException">The text matches no supported FIX time/date-time format.</exception>
    public InitValueClock(string raw)
    {
        Raw = raw;

        foreach (LocalTimePattern pattern in TimePatterns)
        {
            ParseResult<LocalTime> result = pattern.Parse(raw);
            if (result.Success)
            {
                TimeOfDay = result.Value;
                return;
            }
        }

        foreach (LocalDateTimePattern pattern in DateTimePatterns)
        {
            ParseResult<LocalDateTime> result = pattern.Parse(raw);
            if (result.Success)
            {
                DateTime = result.Value;
                return;
            }
        }

        throw ThrowHelper.New<InvalidFieldValueException>(ExceptionContext, ErrorMessages.InvalidDateOrTimeValue, raw);
    }

    /// <summary>The raw initValue text, retained for diagnostics.</summary>
    public string Raw { get; }

    /// <summary>The time-of-day, when the initValue was supplied time-only; otherwise null.</summary>
    public LocalTime? TimeOfDay { get; }

    /// <summary>The local date-and-time, when the initValue carried a date; otherwise null.</summary>
    public LocalDateTime? DateTime { get; }

    /// <summary>True when the initValue was a bare time-of-day (no date component).</summary>
    public bool IsTimeOnly => TimeOfDay.HasValue;
}
