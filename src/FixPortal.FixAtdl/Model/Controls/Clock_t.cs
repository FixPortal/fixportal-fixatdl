// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
// FP Enhancement: 2026-05-31 — apply localMktTz via NodaTime; emit UTC at the wire boundary (batch 5, C1).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Globalization;
using FixPortal.FixAtdl.Diagnostics;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Controls.Support;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Types.Support;
using FixPortal.FixAtdl.Resources;
using NodaTime;
using NodaTime.TimeZones;

namespace FixPortal.FixAtdl.Model.Controls;

/// <summary>
/// Represents the Clock_t control element within FIXatdl.
/// </summary>
/// <remarks>
/// A clock value is expressed in the market-local zone given by <see cref="LocalMktTz"/> and feeds a
/// UTCTimestamp_t FIX field, whose wire value must be UTC. This control is therefore the local→UTC bridge
/// (the only place that knows the market zone). It stores the resolved value as a NodaTime
/// <see cref="Instant"/> (a UTC point-in-time): <see cref="GetCurrentValue"/> returns the local-market
/// representation for display, while <see cref="ToDateTime(IParameter, IFormatProvider)"/> returns the UTC
/// instant for the wire. The BCL↔NodaTime seam is confined to this control and <see cref="InitValueClock"/>.
/// </remarks>
public class Clock_t : InitializableControl<InitValueClock?>
{
    private Instant? _value;

    /// <summary>
    /// Initializes a new instance of <see cref="Clock_t"/> using the supplied ID.
    /// </summary>
    /// <param name="id">ID for this control.</param>
    public Clock_t(string id)
        : base(id)
    {
    }

    /// <summary>The IANA/Olson zone in which initValue is represented. Required when initValue is supplied.
    /// Applicable when xsi:type is Clock_t. Null when not supplied in the ATDL.</summary>
    public string? LocalMktTz { get; set; }

    /// <summary>Defines the treatment of initValue time. 0: use initValue; 1: use current time if initValue time has passed.
    /// The default value is 0.</summary>
    public int? InitValueMode { get; set; }

    /// <summary>
    /// The clock used to read "now" (for <see cref="InitValueMode"/> == 1 and to determine the market's
    /// current date for a time-only initValue). Defaults to the system clock; assign a NodaTime FakeClock
    /// in tests. Set after reflective construction.
    /// </summary>
    public IClock Clock { get; set; } = SystemClock.Instance;

    /// <summary>
    /// The time-zone provider used to resolve <see cref="LocalMktTz"/>. Defaults to the TZDB provider.
    /// </summary>
    public IDateTimeZoneProvider TimeZoneProvider { get; set; } = DateTimeZoneProviders.Tzdb;

    #region InitializableControl<T> Overrides

    /// <summary>
    /// Attempts to load the supplied FIX field value (a UTC timestamp) into this control.
    /// </summary>
    /// <param name="value">Value to set this control to.</param>
    /// <returns>true if the supplied value could set this control; false otherwise.</returns>
    protected override bool LoadDefaultFromFixValue(string value)
    {
        bool parsed = FixDateTime.TryParse(value, CultureInfo.InvariantCulture, out DateTime result);

        _value = parsed ? ToInstant(result) : null;

        return parsed;
    }

    /// <summary>
    /// Loads this control from <see cref="InitializableControl{T}.InitValue"/>, converting the
    /// market-local time to a UTC instant via <see cref="LocalMktTz"/>. If no initValue was supplied the
    /// control value is left null.
    /// </summary>
    protected override void LoadDefaultFromInitValue()
    {
        // Surface an invalid initValueMode (only null/0/1 are defined) before anything else, rather than
        // silently treating anything that is not 1 as 0 (#4).
        if (InitValueMode is not (null or 0 or 1))
        {
            throw ThrowHelper.New<InvalidFieldValueException>(this, ErrorMessages.InitControlValueError,
                Id, string.Format(CultureInfo.InvariantCulture, "initValueMode '{0}' is invalid; expected 0 or 1", InitValueMode));
        }

        if (InitValue == null)
        {
            _value = null;
            return;
        }

        // FIXatdl requires localMktTz whenever initValue is supplied on a Clock_t. Without it the local→UTC
        // conversion is undefined; fail fast rather than emit a wrong instant (C1).
        if (string.IsNullOrEmpty(LocalMktTz))
        {
            throw ThrowHelper.New<InvalidFieldValueException>(this, ErrorMessages.InitControlValueError,
                Id, "localMktTz is required when initValue is supplied on a Clock_t control");
        }

        DateTimeZone? zone = TimeZoneProvider.GetZoneOrNull(LocalMktTz);

        if (zone == null)
        {
            throw ThrowHelper.New<InvalidFieldValueException>(this, ErrorMessages.InitControlValueError,
                Id, string.Format(CultureInfo.InvariantCulture, "localMktTz '{0}' is not a recognised IANA time zone", LocalMktTz));
        }

        Instant nowInstant = Clock.GetCurrentInstant();

        LocalDateTime localDt;
        if (InitValue.IsTimeOnly)
        {
            LocalDate marketToday = nowInstant.InZone(zone).Date;
            localDt = marketToday.At(InitValue.TimeOfDay!.Value);
        }
        else
        {
            localDt = InitValue.DateTime!.Value;
        }

        // LenientResolver maps DST gaps forward and overlaps to the earlier offset, so resolution never
        // throws on a spring-forward / fall-back wall-clock time.
        Instant initInstant = zone.ResolveLocal(localDt, Resolvers.LenientResolver).ToInstant();

        // initValueMode 1: use "now" if the initValue instant has already passed. Comparison is on instants.
        _value = (InitValueMode == 1 && nowInstant > initInstant) ? nowInstant : initInstant;
    }

    #endregion

    #region Control_t Overrides

    /// <summary>
    /// Sets the value of this control using the value of the supplied parameter.
    /// </summary>
    /// <param name="parameter">Parameter to set this control's value from.</param>
    public override void SetValueFromParameter(IParameter parameter)
    {
        IControlConvertible value = parameter.GetValueForControl();

        DateTime? dateTime = value.ToDateTime();

        _value = dateTime == null ? null : ToInstant(dateTime.Value);
    }

    /// <summary>
    /// Sets the value of this control; either via a DateTime, or using the FIXatdl '{NULL}' value. This method
    /// is either called indirectly from the user interface, or by a StateRule.
    /// </summary>
    /// <param name="newValue">Either a valid DateTime or null (meaning do not send this value over FIX).
    /// May also contain the FIXatdl '{NULL}' value as a string.</param>
    public override void SetValue(object newValue)
    {
        bool isString = newValue is string;
        bool isDateTime = newValue is DateTime;

        if (isString)
        {
            string? value = newValue as string;

            if (value == Atdl.NullValue)
            {
                _value = null;
            }
            else if (FixDateTime.TryParse(value, CultureInfo.InvariantCulture, out DateTime parsed))
            {
                // Accept a serialized timestamp so the control can round-trip its own ToString output,
                // not just {NULL} (#3).
                _value = ToInstant(parsed);
            }
            else
            {
                throw ThrowHelper.New<InvalidFieldValueException>(this, ErrorMessages.InitControlValueError,
                    Id, string.Format(CultureInfo.InvariantCulture, "'{0}' is not a valid value for this control", value));
            }
        }
        else
        {
            _value = isDateTime
                ? ToInstant((DateTime)newValue)
                : newValue == null
                    ? null
                    : throw ThrowHelper.New<InternalErrorException>(this, InternalErrors.UnexpectedArgumentType,
                        newValue.GetType().FullName, "System.String, System.DateTime");
        }
    }

    /// <summary>
    /// Resets this control to a null value.
    /// </summary>
    public override void Reset()
    {
        _value = null;
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent nullable boolean value.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <returns>One of true, false or null which is equivalent to the value of this instance.</returns>
    public override bool? ToBoolean(IParameter targetParameter)
    {
        throw ThrowHelper.New<InvalidCastException>(this, ErrorMessages.UnsupportedControlValueConversion, _value, "Boolean", Id);
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent nullable decimal value using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <param name="provider">An <see cref="IFormatProvider"/> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>A nullable decimal equivalent to the value of this instance.</returns>
    public override decimal? ToDecimal(IParameter targetParameter, IFormatProvider provider)
    {
        throw ThrowHelper.New<InvalidCastException>(this, ErrorMessages.UnsupportedControlValueConversion, _value, "Decimal", Id);
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent 32-bit signed integer using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <param name="provider">An <see cref="IFormatProvider"/> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>A nullable 32-bit signed integer equivalent to the value of this instance.</returns>
    public override int? ToInt32(IParameter targetParameter, IFormatProvider provider)
    {
        throw ThrowHelper.New<InvalidCastException>(this, ErrorMessages.UnsupportedControlValueConversion, _value, "Int32", Id);
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent 32-bit unsigned integer using the specified culture-specific formatting information.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <param name="provider">An <see cref="IFormatProvider"/> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>A nullable 32-bit unsigned integer equivalent to the value of this instance.</returns>
    public override uint? ToUInt32(IParameter targetParameter, IFormatProvider provider)
    {
        throw ThrowHelper.New<InvalidCastException>(this, ErrorMessages.UnsupportedControlValueConversion, _value, "UInt32", Id);
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent char value.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <returns>A nullable char value equivalent to the value of this instance. May be null.</returns>
    public override char? ToChar(IParameter targetParameter)
    {
        throw ThrowHelper.New<InvalidCastException>(this, ErrorMessages.UnsupportedControlValueConversion, _value, "Char", Id);
    }

    /// <summary>
    /// Converts the value of this instance to an equivalent string value (the UTC wire representation, YYYYMMDD-HH:MM:SS).
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <returns>A string value equivalent to the value of this instance. May be null.</returns>
    public override string ToString(IParameter targetParameter)
    {
        if (_value == null)
        {
            return null!;
        }

        DateTime utc = _value.Value.ToDateTimeUtc();

        // Emit milliseconds only when present, so whole-second values keep the compact seconds form
        // while sub-second precision is no longer silently dropped (batch 5, Phase-A follow-up).
        string format = utc.Millisecond == 0 ? FixDateTimeFormat.FixDateTime : FixDateTimeFormat.FixDateTimeMs;

        return utc.ToString(format, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converts the value of this instance to the equivalent UTC <see cref="DateTime"/> for the FIX wire.
    /// </summary>
    /// <param name="targetParameter">Target parameter for this conversion.</param>
    /// <param name="provider">An <see cref="IFormatProvider"/> interface implementation that supplies culture-specific formatting information.</param>
    /// <returns>The UTC instant (Kind = Utc), or null.</returns>
    public override DateTime? ToDateTime(IParameter targetParameter, IFormatProvider provider)
    {
        return _value?.ToDateTimeUtc();
    }

    /// <summary>
    /// Indicates whether the control has enumerated state.
    /// </summary>
    public override bool HasEnumeratedState => false;

    #endregion

    #region IValueProvider Members

    /// <summary>
    /// Gets the current value of this control (the LOCAL-market representation for display / Edits), for
    /// use in Edits as part of StateRules.
    /// </summary>
    /// <remarks>
    /// The returned value is the local-market wall-clock representation intended for DISPLAY and Edit-rule
    /// evaluation only. It must NOT be fed back into <see cref="SetValue"/>, which interprets an inbound
    /// DateTime as UTC — round-tripping it that way would shift the instant by the zone offset.
    /// </remarks>
    /// <returns>Either a valid DateTime (local-market wall-clock when <see cref="LocalMktTz"/> is set,
    /// otherwise UTC) or null.</returns>
    public override object GetCurrentValue()
    {
        if (_value == null)
        {
            return null!;
        }

        if (!string.IsNullOrEmpty(LocalMktTz))
        {
            DateTimeZone? zone = TimeZoneProvider.GetZoneOrNull(LocalMktTz);

            if (zone != null)
            {
                return _value.Value.InZone(zone).ToDateTimeUnspecified();
            }
        }

        return _value.Value.ToDateTimeUtc();
    }

    #endregion

    /// <summary>
    /// Converts an inbound BCL <see cref="DateTime"/> (from a FIX wire value or a UI/StateRule set) to a
    /// NodaTime <see cref="Instant"/>. These values are UTC by convention; a Local value is converted and an
    /// Unspecified value is taken to be UTC.
    /// </summary>
    private static Instant ToInstant(DateTime dateTime) => dateTime.Kind switch
    {
        DateTimeKind.Utc => Instant.FromDateTimeUtc(dateTime),
        DateTimeKind.Local => Instant.FromDateTimeUtc(dateTime.ToUniversalTime()),
        _ => Instant.FromDateTimeUtc(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)),
    };
}
