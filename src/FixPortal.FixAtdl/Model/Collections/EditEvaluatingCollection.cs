// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Collections.ObjectModel;
using FixPortal.FixAtdl.Diagnostics;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Resources;
using FixPortal.FixAtdl.Utility;

namespace FixPortal.FixAtdl.Model.Collections;

/// <summary>
/// Collection used to store typed instances of Edit_t, either for validating parameters via StrategyEdit, or 
/// for implementing StateRules using control values.  This collection also provides the ability to evaluate the Edits that
/// it contains.
/// </summary>
/// <typeparam name="T"></typeparam>
public class EditEvaluatingCollection<T> : Collection<IEdit<T>>, IResolvable<Strategy_t, T>
{
    /// <summary>
    /// Logic operator for this collection of Edits.
    /// </summary>
    public LogicOperator_t? LogicOperator { get; set; }

    /// <summary>
    /// Current state of this collection of Edits.
    /// </summary>
    public bool CurrentState { get; private set; }

    /// <summary>
    /// Gets the set of sources for the data to be evaluated as part of this collection of Edits.
    /// </summary>
    public HashSet<string> Sources { get; } = [];

    /// <summary>
    /// Inserts an item, recording its evaluation sources. Implemented as an override of the
    /// Collection&lt;T&gt; virtual hook (rather than a <c>new Add</c>) so the source-tracking side
    /// effect cannot be bypassed by base-typed access or a collection initializer.
    /// </summary>
    /// <param name="index">The insertion index.</param>
    /// <param name="item">The item.</param>
    protected override void InsertItem(int index, IEdit<T> item)
    {
        base.InsertItem(index, item);

        foreach (string source in item.Sources)
        {
            Sources.Add(source);
        }
    }

    /// <summary>
    /// Evaluates this instance based on current field values and any additional FIX field values that this
    /// EditEvaluatingCollection references.
    /// </summary>
    /// <param name="additionalValues">Any additional FIX field values that may be required in the Edit evaluation.</param>
    public void Evaluate(FixFieldValueProvider additionalValues)
    {
        if (LogicOperator == null)
        {
            throw ThrowHelper.New<InvalidOperationException>(this, ErrorMessages.MissingLogicalOperatorOnSetOfEdits);
        }

        bool shortCircuit = false;
        bool newState = LogicOperator == LogicOperator_t.And;
        int xorCount = 0;

        foreach (IEdit<T> item in Items)
        {
            if (shortCircuit)
            {
                break;
            }

            item.Evaluate(additionalValues);

            switch (LogicOperator)
            {
                case LogicOperator_t.And:
                    newState &= item.CurrentState;
                    if (!newState)
                    {
                        shortCircuit = true;
                    }

                    break;

                case LogicOperator_t.Or:
                    newState |= item.CurrentState;
                    if (newState)
                    {
                        shortCircuit = true;
                    }

                    break;

                case LogicOperator_t.Not:
                    // The schema permits a single operand for NOT. Defensively, evaluate NOT as
                    // "no operand is true" so a (schema-invalid) multi-operand NOT is deterministic
                    // instead of "last operand wins"; this collapses to !operand for one operand.
                    if (item.CurrentState)
                    {
                        newState = false;
                        shortCircuit = true;
                    }
                    else
                    {
                        newState = true;
                    }

                    break;

                // From the spec: "As a convention we define XOR as 'one and only one', which means it evaluates to true when one
                // and only one of its operands is true. If none or more than one of its operands is true then XOR is false."
                case LogicOperator_t.Xor:
                    if (item.CurrentState)
                    {
                        xorCount++;
                    }

                    newState = xorCount == 1;
                    break;
            }
        }

        CurrentState = newState;
    }

    #region IResolvable<Strategy_t, T> Members

    // TODO: Unbind needed?
    void IResolvable<Strategy_t, T>.Resolve(Strategy_t strategy, ISimpleDictionary<T> sourceCollection)
    {
        foreach (IEdit<T> item in Items)
        {
            // Add accepts any IEdit<T>; only resolve those that are resolvable rather than forcing
            // the cast with ! and risking an NRE on a non-resolvable edit.
            if (item is IResolvable<Strategy_t, T> resolvable)
            {
                resolvable.Resolve(strategy, sourceCollection);
            }
        }
    }

    #endregion IResolvable<Strategy_t> Members
}
