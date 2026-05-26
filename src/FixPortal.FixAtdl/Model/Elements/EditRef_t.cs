// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Collections.Generic;
using FixPortal.FixAtdl.Diagnostics;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Resources;
using FixPortal.FixAtdl.Utility;
using FixPortal.FixAtdl.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FixPortal.FixAtdl.Model.Elements;

/// <summary>
/// Represents a FIXatdl EditRef_t.
/// </summary>
public class EditRef_t<T> : IEdit<T>, IResolvable<Strategy_t, T> where T : class, IValueProvider
{
    // Use FixPortal.FixAtdl.Validation namespace rather than FixPortal.FixAtdl.Model.Elements for debugging purposes
    // FP Enhancement: 2026-05-23 — TODO wire injected logger when refactoring class to accept ILogger.
    private static readonly NullLogger _log = NullLogger.Instance;

    private Edit_t<T> _referencedEdit = null!;

    public EditRef_t(string id)
    {
        Id = id;
    }

    /// <summary>
    /// Refers to an ID of a previously defined edit element. The edit element may be defined at the strategy level or at the strategies level.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Provides a string representation of this EditRef_t, primarily for debugging purposes.
    /// </summary>
    /// <returns>String representation of this EditRef_t.</returns>
    public override string ToString()
    {
        return _referencedEdit != null ? _referencedEdit.ToString() : string.Empty;
    }

    /// <summary>
    /// Evaluates this EditRef based on the current field values.
    /// </summary>
    public void Evaluate()
    {
        _referencedEdit.Evaluate(FixFieldValueProvider.Empty);
    }

    /// <summary>
    /// Evaluates this EditRef based on the current field values and any additional FIX field values that this
    /// EditRef references.
    /// </summary>
    /// <param name="additionalValues">Any additional FIX field values that may be required in the Edit evaluation.</param>
    public void Evaluate(FixFieldValueProvider additionalValues)
    {
        _referencedEdit.Evaluate(additionalValues);
    }

    #region IEdit_t Members

    public string Field
    {
        get => _referencedEdit.Field; set => _referencedEdit.Field = value;
    }

    public string Field2
    {
        get => _referencedEdit.Field2; set => _referencedEdit.Field2 = value;
    }

    public Operator_t? Operator
    {
        get => _referencedEdit.Operator; set => _referencedEdit.Operator = value;
    }

    public LogicOperator_t? LogicOperator
    {
        get => _referencedEdit.LogicOperator; set => _referencedEdit.LogicOperator = value;
    }

    public string Value
    {
        get => _referencedEdit.Value; set => _referencedEdit.Value = value;
    }

    public object FieldValue => _referencedEdit.FieldValue;

    public object Field2Value => _referencedEdit.Field2Value;

    public bool CurrentState => _referencedEdit.CurrentState;

    public EditEvaluatingCollection<T> Edits => _referencedEdit.Edits;

    /// <summary>
    /// Gets the set of sources for the data to be evaluated as part of this StrategyEdit.
    /// </summary>
    public HashSet<string> Sources => _referencedEdit.Sources;

    #endregion

    #region IResolvable<Strategy_t> Members

    void IResolvable<Strategy_t, T>.Resolve(Strategy_t strategy, ISimpleDictionary<T> sourceCollection)
    {
        if (strategy.Edits.Contains(Id))
        {
            _referencedEdit = strategy.Edits.Clone<T>(Id);

            if (_log.IsEnabled(LogLevel.Debug))
            {
                _log.LogDebug("EditRef Id {Arg0} linked to new Edit_t resolved from Strategy '{Arg1}'", Id, strategy.Name);
            }
        }
        else
        {
            Strategies_t strategies = strategy.Parent;

            if (strategies != null && strategies.Edits.Contains(Id))
            {
                _referencedEdit = strategies.Edits.Clone<T>(Id);

                if (_log.IsEnabled(LogLevel.Debug))
                {
                    _log.LogDebug("EditRef Id {Arg0} linked to new Edit_t resolved resolved from Strategies level", Id);
                }
            }
            else
            {
                throw ThrowHelper.New<ReferencedObjectNotFoundException>(this, ErrorMessages.EditRefResolutionFailure, Id);
            }
        }

        (_referencedEdit as IResolvable<Strategy_t, T>).Resolve(strategy, sourceCollection);
    }

    #endregion IBinIResolvabledable<Strategy_t> Members
}
