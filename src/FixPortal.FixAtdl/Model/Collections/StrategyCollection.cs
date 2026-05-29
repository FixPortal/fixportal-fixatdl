// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Collections.ObjectModel;
using FixPortal.FixAtdl.Model.Elements;

namespace FixPortal.FixAtdl.Model.Collections;

/// <summary>
/// Represents the collection of strategies owned by a <see cref="Strategies_t"/>.
/// </summary>
public class StrategyCollection : KeyedCollection<string, Strategy_t>
{
    private readonly Strategies_t _owner;

    /// <summary>
    /// Initializes a new <see cref="StrategyCollection"/>.
    /// </summary>
    /// <param name="owner">The owning strategies container.</param>
    public StrategyCollection(Strategies_t owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Gets the key for the specified strategy item.
    /// </summary>
    /// <param name="strategy">The strategy item.</param>
    /// <returns>The strategy name.</returns>
    protected override string GetKeyForItem(Strategy_t strategy)
    {
        return strategy.Name;
    }

    /// <summary>
    /// Inserts a strategy, assigning its parent container. Implemented as an override of the
    /// KeyedCollection virtual hook (rather than a <c>new Add</c>) so the parent-wiring cannot be
    /// bypassed by base-typed access or a collection initializer.
    /// </summary>
    /// <param name="index">The insertion index.</param>
    /// <param name="item">The strategy to insert.</param>
    protected override void InsertItem(int index, Strategy_t item)
    {
        item.Parent = _owner;

        base.InsertItem(index, item);
    }

    /// <inheritdoc />
    protected override void SetItem(int index, Strategy_t item)
    {
        item.Parent = _owner;

        base.SetItem(index, item);
    }
}
