// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Collections.ObjectModel;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Resources;
using ThrowHelper = FixPortal.FixAtdl.Diagnostics.ThrowHelper;

namespace FixPortal.FixAtdl.Model.Collections;

/// <summary>
/// Represents a keyed collection of <see cref="ListItem_t"/> instances.
/// </summary>
public class ListItemCollection : KeyedCollection<string, ListItem_t>
{
    /// <summary>
    /// Copies the supplied items into the collection.
    /// </summary>
    /// <param name="items">The items to copy.</param>
    public void CopyFrom(List<ListItem_t> items)
    {
        foreach (ListItem_t item in items)
        {
            Add(item);
        }
    }

    /// <summary>
    /// Inserts a list item, translating a duplicate-key collision into a domain
    /// <see cref="DuplicateKeyException"/>. Implemented as an override of the KeyedCollection virtual
    /// hook (rather than a <c>new Add</c>) so the translation cannot be bypassed by base-typed access
    /// or a collection initializer.
    /// </summary>
    /// <param name="index">The insertion index.</param>
    /// <param name="item">The item to insert.</param>
    protected override void InsertItem(int index, ListItem_t item)
    {
        try
        {
            base.InsertItem(index, item);
        }
        catch (ArgumentException ex)
        {
            throw ThrowHelper.New<DuplicateKeyException>(this, ex, ErrorMessages.AttemptToAddDuplicateKey,
                item.EnumId, "ListItems");
        }
    }

    /// <summary>
    /// Gets the enum identifiers for all items in the collection.
    /// </summary>
    public string[] EnumIds => [.. from item in Items select item.EnumId];

    /// <summary>
    /// Gets a value indicating whether the collection contains any items.
    /// </summary>
    public bool HasItems => Count > 0;

    /// <summary>
    /// Gets the key for the specified list item.
    /// </summary>
    /// <param name="item">The list item.</param>
    /// <returns>The enum identifier.</returns>
    protected override string GetKeyForItem(ListItem_t item)
    {
        return item.EnumId;
    }
}
