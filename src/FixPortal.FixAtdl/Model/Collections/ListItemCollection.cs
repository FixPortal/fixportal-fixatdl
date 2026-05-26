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
    /// Adds a list item to the collection.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public new void Add(ListItem_t item)
    {
        try
        {
            base.Add(item);
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
