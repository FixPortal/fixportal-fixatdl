// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Collections.ObjectModel;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Utility;

namespace FixPortal.FixAtdl.Model.Collections;

/// <summary>
/// Collection class of <see cref="StrategyEdit_t">StrategyEdit</see>s.
/// </summary>
public class StrategyEditCollection : Collection<StrategyEdit_t>
{
    /// <summary>
    /// Validates all the <see cref="StrategyEdit_t">StrategyEdit</see>s in this collection.
    /// </summary>
    /// <param name="additionalValues">Any additional FIX field values that may be required in the Edit evaluation.</param>
    /// <param name="shortCircuit">If true, this method returns as soon as any error is found; if false, all StrategyEdits
    /// are evaluated before the method returns.</param>
    public bool EvaluateAll(FixFieldValueProvider additionalValues, bool shortCircuit)
    {
        bool valid = true;

        foreach (StrategyEdit_t strategyEdit in Items)
        {
            strategyEdit.Evaluate(additionalValues);

            if (!strategyEdit.CurrentState)
            {
                if (shortCircuit)
                {
                    return false;
                }

                valid = false;
            }
        }

        return valid;
    }

    /// <summary>
    /// Inserts a strategy edit into the collection.
    /// </summary>
    /// <param name="index">The insertion index.</param>
    /// <param name="item">The strategy edit to insert.</param>
    protected override void InsertItem(int index, StrategyEdit_t item)
    {
        base.InsertItem(index, item);
    }

    /// <summary>
    /// Resolves all interdependencies e.g. edits to edit refs, control values to edits, etc.  Called once
    /// all strategies have been loaded as there may be dependencies on EditRefs at the global level.
    /// </summary>
    /// <param name="owningStrategy">The strategy that owns the strategy edits.</param>
    public void ResolveAll(Strategy_t owningStrategy)
    {
        foreach (StrategyEdit_t strategyEdit in this)
        {
            (strategyEdit as IResolvable<Strategy_t, IParameter>).Resolve(owningStrategy, owningStrategy.Parameters);
        }
    }
}
