// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Globalization;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FixPortal.FixAtdl.Fix;

/// <summary>
/// Provides access to initial values for FIXatdl controls based on a set of input FIX fields.
/// </summary>
public class FixFieldValueProvider
{
    // FP Enhancement: 2026-05-23 — TODO wire injected logger when refactoring class to accept ILogger.
    private readonly NullLogger _log = NullLogger.Instance;
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
    public bool TryGetValue(string fixField, string targetParameterName, out string value)
    {
        bool retrieved = TryGetValue(fixField, out var result);

        if (retrieved && !string.IsNullOrEmpty(targetParameterName) && Parameters != null && Parameters.Contains(targetParameterName))
        {
            IParameter parameter = Parameters[targetParameterName];

            if (parameter.HasEnumPairs)
            {
                string wireValue = result;

                if (_log.IsEnabled(LogLevel.Debug))
                {
                    _log.LogDebug("Attempting to find EnumID for FIX field {FixField} using parameter {ParameterName} with wire value '{WireValue}'",
                        fixField, targetParameterName, wireValue);
                }

                retrieved = parameter.EnumPairs.TryParseWireValue(wireValue, out result);
            }
            else if (parameter is Parameter_t<Percentage_t> t)
            {
                retrieved = ProcessPercentageValue(t, ref result);
            }

            if (_log.IsEnabled(LogLevel.Debug))
            {
                _log.LogDebug("FIX enumerated value lookup for field {FixField} returning {Retrieved}; value = '{Value}'", fixField,
                    retrieved, retrieved ? result : "N/A");
            }
        }

        value = result!;

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
    public bool TryGetValue(string fixField, out string value)
    {
        bool retrieved = false;
        string? result = null;

        if (_initialValueProvider != null && _initialValueProvider.InputFixValues != null)
        {
            retrieved = _initialValueProvider.InputFixValues.TryGetValue(fixField, out result);

            if (_log.IsEnabled(LogLevel.Debug))
            {
                _log.LogDebug("FIX value lookup for field {FixField} returning {Retrieved}; value = '{Value}'", fixField,
                    retrieved, retrieved ? result : "N/A");
            }
        }

        value = retrieved ? result! : null!;

        return retrieved;
    }

    private static bool ProcessPercentageValue(Parameter_t<Percentage_t> parameter, ref string value)
    {
        bool adjustmentNeeded = parameter.Value.MultiplyBy100 != true;

        if (!adjustmentNeeded)
        {
            return true;
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal decimalValue))
        {
            value = (decimalValue * 100).ToString("0.####", CultureInfo.InvariantCulture);
            return true;
        }

        value = null!;
        return false;
    }
}
