// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

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

    /// <summary>
    /// The resolved referenced edit. Accessing this before <see cref="IResolvable{Strategy_t,T}.Resolve"/>
    /// has linked the EditRef surfaces a clear diagnostic rather than a bare NullReferenceException.
    /// </summary>
    private Edit_t<T> ReferencedEdit => _referencedEdit
        ?? throw ThrowHelper.New<InternalErrorException>(this, $"EditRef '{Id}' has not been resolved; Resolve must be called before the referenced edit is accessed.");

    /// <summary>
    /// Initializes a new <see cref="EditRef_t{T}"/>.
    /// </summary>
    /// <param name="id">The identifier of the referenced edit.</param>
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
        ReferencedEdit.Evaluate(FixFieldValueProvider.Empty);
    }

    /// <summary>
    /// Evaluates this EditRef based on the current field values and any additional FIX field values that this
    /// EditRef references.
    /// </summary>
    /// <param name="additionalValues">Any additional FIX field values that may be required in the Edit evaluation.</param>
    /// <inheritdoc />
    public void Evaluate(FixFieldValueProvider additionalValues)
    {
        ReferencedEdit.Evaluate(additionalValues);
    }

    #region IEdit_t Members

    /// <inheritdoc />
    public string Field
    {
        get => ReferencedEdit.Field; set => ReferencedEdit.Field = value;
    }

    /// <inheritdoc />
    public string Field2
    {
        get => ReferencedEdit.Field2; set => ReferencedEdit.Field2 = value;
    }

    /// <inheritdoc />
    public Operator_t? Operator
    {
        get => ReferencedEdit.Operator; set => ReferencedEdit.Operator = value;
    }

    /// <inheritdoc />
    public LogicOperator_t? LogicOperator
    {
        get => ReferencedEdit.LogicOperator; set => ReferencedEdit.LogicOperator = value;
    }

    /// <inheritdoc />
    public string Value
    {
        get => ReferencedEdit.Value; set => ReferencedEdit.Value = value;
    }

    /// <inheritdoc />
    public object FieldValue => ReferencedEdit.FieldValue;

    /// <inheritdoc />
    public object Field2Value => ReferencedEdit.Field2Value;

    /// <inheritdoc />
    public bool CurrentState => ReferencedEdit.CurrentState;

    /// <inheritdoc />
    public EditEvaluatingCollection<T> Edits => ReferencedEdit.Edits;

    /// <inheritdoc />
    public HashSet<string> Sources => ReferencedEdit.Sources;

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
