// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Model;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Controls.Support;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Types;

namespace FixPortal.FixAtdl.Fix;

/// <summary>
/// Provides access to initial values for FIXatdl controls based on a set of input FIX fields.
/// </summary>
public class FixFieldValueProvider
{
    private readonly IInitialFixValueProvider? _initialValueProvider;

    /// <summary>
    /// Initializes a new <see cref="FixFieldValueProvider"/> instance using the supplied set of input
    /// values and parameters.
    /// </summary>
    /// <param name="initialValueProvider"></param>
    /// <param name="parameters">Parameters to use.</param>
    public FixFieldValueProvider(IInitialFixValueProvider? initialValueProvider, ParameterCollection? parameters)
    {
        _initialValueProvider = initialValueProvider;
        Parameters = parameters;
    }

    /// <summary>
    /// Gets a static instance of an empty provider.
    /// </summary>
    public static FixFieldValueProvider Empty { get; } = new(null, null);

    /// <summary>
    /// Gets the parameters for this value provider.
    /// </summary>
    public ParameterCollection? Parameters { get; }

    /// <summary>
    /// Gets the FIX values collection for this value provider.
    /// </summary>
    public FixTagValuesCollection FixValues => _initialValueProvider?.InputFixValues ?? FixTagValuesCollection.Empty;

    /// <summary>
    /// Attempts to get the value of the specified FIX field (in FIX_ format), returning the value as a string.
    /// In the case of enumerated fields, the output parameter contains the EnumID, assuming a valid lookup was
    /// possible.
    /// </summary>
    /// <param name="fixField">FIX field value to retrieve, in FIX_ format.</param>
    /// <param name="targetParameterName">Target parameter for this field value.  May be null.</param>
    /// <param name="value">Contains the value of the FIX field if it could successfully be retrieved.</param>
    /// <returns>true if the field could be retrieved; false otherwise.</returns>
    public bool TryGetValue(string fixField, string? targetParameterName, [NotNullWhen(true)] out string? value)
    {
        bool retrieved = TryGetValue(fixField, out var result);

        if (
            retrieved
            && !string.IsNullOrEmpty(targetParameterName)
            && Parameters != null
            && Parameters.Contains(targetParameterName)
        )
        {
            IParameter parameter = Parameters[targetParameterName];

            if (parameter.HasEnumPairs && result is not null)
            {
                string wireValue = result;
                retrieved = parameter is Parameter_t<MultipleCharValue_t> or Parameter_t<MultipleStringValue_t>
                    ? TryGetMultipleEnumIds(parameter, wireValue, out result)
                    : parameter.EnumPairs.TryParseWireValue(wireValue, out result);
            }
            else if (parameter is Parameter_t<Boolean_t> booleanParameter && result is not null)
            {
                // A Boolean_t parameter carries its wire mapping in TrueWireValue/FalseWireValue
                // (defaulting to Y/N) rather than in EnumPairs, so the branch above never runs for
                // it; decode the raw FIX value through the declared mapping, mirroring how the
                // binary controls emit it.
                retrieved = TryTranslateBooleanValue(booleanParameter.Value, result, out result);
            }
            else if (parameter is Parameter_t<Percentage_t> t && result is not null)
            {
                retrieved = ProcessPercentageValue(t, ref result);
            }
        }

        value = result;

        return retrieved;
    }

    /// <summary>
    /// Attempts to get the value of the specified FIX field (in FIX_ format), returning the value as a string.
    /// In the case of enumerated fields, the output parameter contains the EnumID, assuming a valid lookup was
    /// possible.
    /// </summary>
    /// <param name="fixField">FIX field value to retrieve, in FIX_ format.</param>
    /// <param name="value">Contains the value of the FIX field if it could successfully be retrieved.</param>
    /// <returns>true if the field could be retrieved; false otherwise.</returns>
    public bool TryGetValue(string fixField, [NotNullWhen(true)] out string? value)
    {
        bool retrieved = false;
        string? result = null;

        if (_initialValueProvider?.InputFixValues is { } inputFixValues)
        {
            retrieved = inputFixValues.TryGetValue(fixField, out result);
        }

        value = retrieved ? result : null;

        return retrieved;
    }

    private static bool ProcessPercentageValue(Parameter_t<Percentage_t> parameter, ref string? value)
    {
        bool adjustmentNeeded = parameter.Value.MultiplyBy100 != true;

        if (!adjustmentNeeded)
        {
            return true;
        }

        // Explicit styles: the FIX numeric alphabet carries neither a thousands separator nor an
        // exponent, so "1,234.5" and "1E2" must fail rather than read as 123450 and 100 (R21).
        // Atdl.FixDecimalStyles is that alphabet stated once for every decimal parse in the model.
        if (!decimal.TryParse(value, Atdl.FixDecimalStyles, CultureInfo.InvariantCulture, out decimal decimalValue))
        {
            value = null;
            return false;
        }

        try
        {
            // Full decimal precision here; the parameter's own Precision governs rounding on the
            // way back out (Percentage_t.ConvertToWireValueFormat), not this display conversion
            // (R28). 28 '#' keeps every representable decimal place while still stripping the
            // trailing zeros a plain ToString would keep from the multiplication (0.5 -> "50",
            // not "50.0"); decimal.Normalize would risk scientific notation, so it is not used.
            value = (decimalValue * 100).ToString("0.############################", CultureInfo.InvariantCulture);
            return true;
        }
        catch (OverflowException)
        {
            // A Try-style method must not throw: an unrepresentable scale-up is a failed lookup.
            value = null;
            return false;
        }
    }

    // Translates a raw FIX boolean value into the standard Y/N spelling the binary controls accept,
    // honouring the parameter's declared TrueWireValue/FalseWireValue mapping; {NULL} (and a null
    // stored programmatically) passes through so the control reads it as "unset". Returns false for
    // a value the mapping does not recognise so initialisation falls back to initValue.
    private static bool TryTranslateBooleanValue(Boolean_t booleanType, string? wireValue, out string? value)
    {
        bool? parsed;
        try
        {
            parsed = booleanType.ParseWireValue(wireValue!);
        }
        catch (InvalidFieldValueException)
        {
            value = null;
            return false;
        }

        value = parsed switch
        {
            true => "Y",
            false => "N",
            null => Atdl.NullValue,
        };

        return true;
    }

    private static bool TryGetMultipleEnumIds(IParameter parameter, string wireValue, out string? value)
    {
        value = null;
        if (string.IsNullOrEmpty(wireValue))
        {
            return false;
        }
        try
        {
            var state = EnumState.FromWireValue(parameter.EnumPairs, wireValue);
            if (
                parameter
                is Parameter_t<MultipleCharValue_t> { Value.InvertOnWire: true }
                    or Parameter_t<MultipleStringValue_t> { Value.InvertOnWire: true }
            )
            {
                state.InvertSelection();
            }
            value = string.Join(" ", parameter.EnumPairs.EnumIds.Where(id => state[id]));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
