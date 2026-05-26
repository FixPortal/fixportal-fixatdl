// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Elements;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FixPortal.FixAtdl.Validation;

/// <summary>
/// Used to store the validation state for a control.
/// </summary>
public class ControlValidationState
{
    // FP Enhancement: 2026-05-23 — TODO wire injected logger when refactoring class to accept ILogger.
    private readonly NullLogger _log = NullLogger.Instance;
    private ValidationResult _parameterValidationResult = null!;
    private readonly string _controlId;
    private readonly List<StrategyEdit_t> _strategyEdits = [];

    /// <summary>
    /// Initializes a new <see cref="ControlValidationState"/>.
    /// </summary>
    /// <param name="controlId"></param>
    public ControlValidationState(string controlId)
    {
        _controlId = controlId;
    }

    /// <summary>
    /// Gets the current state of this set of StrategyEdits.
    /// </summary>
    /// <remarks>The state cannot be cached within this class as it depends on the states of each
    /// StrategyEdit, which may have changed since our Evaluate method was called.</remarks>
    public bool CurrentState
    {
        get
        {
            bool state = ControlValidationResult == null || ControlValidationResult.IsValid;

            state &= _parameterValidationResult == null || _parameterValidationResult.IsValid;

            foreach (StrategyEdit_t strategyEdit in _strategyEdits)
            {
                state &= strategyEdit.CurrentState;
            }

            return state;
        }
    }

    /// <summary>
    /// Used to hold the results obtained from the control set/validation operation.
    /// </summary>
    public ValidationResult ControlValidationResult { get; set; } = null!;

    /// <summary>
    /// Used to hold the results obtained from the parameter set and validation operation.
    /// </summary>
    public ValidationResult ParameterValidationResult { set => _parameterValidationResult = value; }

    /// <summary>
    /// Adds the supplied StrategyEdit_t to this <see cref="ControlValidationState"/>.
    /// </summary>
    /// <param name="strategyEdit"><see cref="StrategyEdit_t"/> to add to this <see cref="ControlValidationState"/>.</param>
    public void Add(StrategyEdit_t strategyEdit)
    {
        _strategyEdits.Add(strategyEdit);
    }

    /// <summary>
    /// Removes the supplied StrategyEdit_t from this <see cref="ControlValidationState"/>.
    /// </summary>
    /// <param name="strategyEdit"><see cref="StrategyEdit_t"/> to remove from this <see cref="ControlValidationState"/>.</param>
    public void Remove(StrategyEdit_t strategyEdit)
    {
        _strategyEdits.Remove(strategyEdit);
    }

    /// <summary>
    /// Evaluates all the <see cref="StrategyEdit_t"/>s for this control.
    /// </summary>
    /// <param name="additionalValues">Any additional FIX field values that may be required in the Edit evaluation.</param>
    /// <remarks>See <see cref="CurrentState"/> for an explanation of why we don't cache the state locally within the class.</remarks>
    public void Evaluate(FixFieldValueProvider additionalValues)
    {
        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("Evaluating ValidationState for control {ControlId}, CurrentState = {CurrentState}", _controlId, CurrentState);
        }

        bool state = ControlValidationResult == null || ControlValidationResult.IsValid;

        // Evaluating the StrategyEdits may give us meaningless information if the parameter value
        // didn't validate, but we go ahead and do it anyway because failing to do leaves us in an
        // indeterminate state from this value change.
        state &= _parameterValidationResult == null || _parameterValidationResult.IsValid;

        foreach (StrategyEdit_t strategyEdit in _strategyEdits)
        {
            strategyEdit.Evaluate(additionalValues);
            state &= strategyEdit.CurrentState;
        }

        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("Evaluated ValidationState for control {ControlId}, CurrentState = {CurrentState}", _controlId, state);
        }
    }

    /// <summary>
    /// Gets the error messages for all <see cref="StrategyEdit_t"/>s that evaluate to false.
    /// </summary>
    public string ErrorText
    {
        get
        {
            StringBuilder sb = new();

            IEnumerable<StrategyEdit_t> strategyEditsInError = from s in _strategyEdits where !s.CurrentState select s;

            int count = strategyEditsInError.Count();
            bool parameterIsInvalid = _parameterValidationResult != null && !_parameterValidationResult.IsValid;

            if (ControlValidationResult != null && !ControlValidationResult.IsValid)
            {
                sb.Append(ControlValidationResult.ErrorText);

                if (count > 0 || parameterIsInvalid)
                {
                    sb.AppendLine();
                }
            }

            if (parameterIsInvalid)
            {
                sb.Append(_parameterValidationResult!.ErrorText);

                if (count > 0)
                {
                    sb.AppendLine();
                }
            }

            foreach (StrategyEdit_t strategyEdit in from s in _strategyEdits where !s.CurrentState select s)
            {
                sb.Append(strategyEdit.ErrorMessage);

                if (--count > 0)
                {
                    sb.AppendLine();
                }
            }

            if (_log.IsEnabled(LogLevel.Debug))
            {
                _log.LogDebug("ValidationState for control {ControlId} = '{ValidationState}'", _controlId, sb);
            }

            return sb.ToString();
        }
    }
}
