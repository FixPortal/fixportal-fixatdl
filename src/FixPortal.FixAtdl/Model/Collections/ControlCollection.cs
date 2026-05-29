// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Collections.ObjectModel;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Utility;

namespace FixPortal.FixAtdl.Model.Collections;

/// <summary>
/// Collection for storing instances of Control_t.  This class is used at the StrategyPanel level.
/// </summary>
/// <remarks>This class maintains an index on each of the Control_t instances that are added to it.  This index is
/// used when laying out controls on StrategyPanels</remarks>
public class ControlCollection : ObservableCollection<Control_t>
{
    private readonly StrategyPanel_t _owner;

    /// <summary>
    /// Initializes a new instance of the <see cref="ControlCollection"/> class.
    /// </summary>
    /// <param name="owner">The owner.</param>
    public ControlCollection(StrategyPanel_t owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Inserts an item, parenting it to the owning panel and refreshing layout indexes. Implemented
    /// as overrides of the ObservableCollection virtual hooks (rather than <c>new Add</c>/<c>new
    /// Remove</c>) so the parent-wiring and index maintenance cannot be bypassed by base-typed
    /// access, an initializer or AddRange.
    /// </summary>
    /// <param name="index">The insertion index.</param>
    /// <param name="item">The item.</param>
    protected override void InsertItem(int index, Control_t item)
    {
        ((IParentable<StrategyPanel_t>)item).Parent = _owner;

        base.InsertItem(index, item);

        RefreshIndexes();
    }

    /// <inheritdoc />
    protected override void SetItem(int index, Control_t item)
    {
        ((IParentable<StrategyPanel_t>)item).Parent = _owner;

        base.SetItem(index, item);

        RefreshIndexes();
    }

    /// <inheritdoc />
    protected override void RemoveItem(int index)
    {
        base.RemoveItem(index);

        RefreshIndexes();
    }

    /// <summary>
    /// Refreshes the indexes.
    /// </summary>
    public void RefreshIndexes()
    {
        int n = 0;

        foreach (Control_t control in Items)
        {
            control.Index = n;

            n++;
        }
    }
}

