#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FixPortal.FixAtdl.Diagnostics;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Resources;
using FixPortal.FixAtdl.Utility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FixPortal.FixAtdl.Model.Collections;

/// <summary>
/// Collection used to store typed instances of Edit_t, either for validating parameters via StrategyEdit, or 
/// for implementing StateRules using control values.  This collection also provides the ability to evaluate the Edits that
/// it contains.
/// </summary>
/// <typeparam name="T"></typeparam>
public class EditEvaluatingCollection<T> : Collection<IEdit<T>>, IResolvable<Strategy_t, T>
{
    // FP Enhancement: 2026-05-23 — TODO wire injected logger when refactoring class to accept ILogger.
    private readonly ILogger _log = NullLogger.Instance;

    private bool _currentState;
    private readonly HashSet<string> _sources = [];

    /// <summary>
    /// Logic operator for this collection of Edits.
    /// </summary>
    public LogicOperator_t? LogicOperator { get; set; }

    /// <summary>
    /// Current state of this collection of Edits.
    /// </summary>
    public bool CurrentState { get { return _currentState; } }

    /// <summary>
    /// Gets the set of sources for the data to be evaluated as part of this collection of Edits.
    /// </summary>
    public HashSet<string> Sources { get { return _sources; } }

    /// <summary>
    /// Adds the specified item.
    /// </summary>
    /// <param name="item">The item.</param>
    public new void Add(IEdit<T> item)
    {
        base.Add(item);

        foreach (string source in item.Sources)
            _sources.Add(source);

        _log.LogDebug("Edit_t {Edit} added to EditEvaluatingCollection", item.ToString());
    }

    /// <summary>
    /// Evaluates this instance based on current field values and any additional FIX field values that this
    /// EditEvaluatingCollection references.
    /// </summary>
    /// <param name="additionalValues">Any additional FIX field values that may be required in the Edit evaluation.</param>
    public void Evaluate(FixFieldValueProvider additionalValues)
    {
        _log.LogDebug("Evaluating EditEvaluatingCollection with {Count} elements; current state = {CurrentState}", Count, _currentState.ToString().ToLower());

        if (LogicOperator == null)
            throw ThrowHelper.New<InvalidOperationException>(this, ErrorMessages.MissingLogicalOperatorOnSetOfEdits);

        bool shortCircuit = false;
        bool newState = (LogicOperator == LogicOperator_t.And);
        int xorCount = 0;

        foreach (IEdit<T> item in Items)
        {
            if (shortCircuit)
                break;

            item.Evaluate(additionalValues);

            switch (LogicOperator)
            {
                case LogicOperator_t.And:
                    newState &= item.CurrentState;
                    if (!newState)
                        shortCircuit = true;
                    break;

                case LogicOperator_t.Or:
                    newState |= item.CurrentState;
                    if (newState)
                        shortCircuit = true;
                    break;

                case LogicOperator_t.Not:
                    newState = !item.CurrentState;
                    break;

                // From the spec: "As a convention we define XOR as 'one and only one', which means it evaluates to true when one
                // and only one of its operands is true. If none or more than one of its operands is true then XOR is false."
                case LogicOperator_t.Xor:
                    if (item.CurrentState)
                        xorCount++;
                    newState = xorCount == 1;
                    break;
            }

            _log.LogDebug("EditEvaluatingCollection state is now {NewState}", newState.ToString().ToLower());
        }

        _currentState = newState;
    }

    #region IResolvable<Strategy_t, T> Members

    // TODO: Unbind needed?
    void IResolvable<Strategy_t, T>.Resolve(Strategy_t strategy, ISimpleDictionary<T> sourceCollection)
    {
        foreach (IEdit<T> item in Items)
        {
            (item as IResolvable<Strategy_t, T>)!.Resolve(strategy, sourceCollection);
        }
    }

    #endregion IResolvable<Strategy_t> Members
}

