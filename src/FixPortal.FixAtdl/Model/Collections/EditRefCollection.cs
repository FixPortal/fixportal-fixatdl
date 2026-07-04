// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Collections.ObjectModel;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Validation;

namespace FixPortal.FixAtdl.Model.Collections;

/// <summary>
/// Collection used to store typed instances of EditRef_t.
/// </summary>
/// <typeparam name="T">Type.</typeparam>
public class EditRefCollection<T> : KeyedCollection<string, EditRef_t<T>> where T : class, IValueProvider
{
    private readonly EditEvaluatingCollection<T>? _evaluatingCollection;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditRefCollection{T}"/> class.
    /// </summary>
    public EditRefCollection()
        : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EditRefCollection{T}"/> class.
    /// </summary>
    /// <param name="evaluatingCollection">The evaluating collection.</param>
    public EditRefCollection(EditEvaluatingCollection<T>? evaluatingCollection)
    {
        _evaluatingCollection = evaluatingCollection;
    }

    /// <summary>
    /// Inserts an item, also registering it with the associated evaluating collection. Implemented
    /// as an override of the KeyedCollection virtual hook (rather than a <c>new Add</c>) so the
    /// registration cannot be bypassed by base-typed access or an initializer.
    /// </summary>
    /// <param name="index">The insertion index.</param>
    /// <param name="item">The item.</param>
    protected override void InsertItem(int index, EditRef_t<T> item)
    {
        // Insert into the keyed base FIRST: if the keyed insert throws (e.g. duplicate Id), the
        // evaluating collection is not left holding an item the keyed collection rejected (M2).
        base.InsertItem(index, item);

        _evaluatingCollection?.Add(item);
    }

    /// <summary>
    /// Removes an item, also unregistering it from the associated evaluating collection.
    /// </summary>
    /// <param name="index">The index of the item to remove.</param>
    protected override void RemoveItem(int index)
    {
        EditRef_t<T> item = Items[index];

        base.RemoveItem(index);

        _evaluatingCollection?.Remove(item);
    }

    /// <summary>
    /// Replaces an item, keeping the associated evaluating collection in sync.
    /// </summary>
    /// <param name="index">The index of the item to replace.</param>
    /// <param name="item">The replacement item.</param>
    protected override void SetItem(int index, EditRef_t<T> item)
    {
        EditRef_t<T> oldItem = Items[index];

        base.SetItem(index, item);

        if (_evaluatingCollection != null)
        {
            int mirroredIndex = _evaluatingCollection.IndexOf(oldItem);

            if (mirroredIndex >= 0)
            {
                _evaluatingCollection[mirroredIndex] = item;
            }
            else
            {
                _evaluatingCollection.Add(item);
            }
        }
    }

    /// <summary>
    /// Clears the collection, also removing the mirrored entries from the associated evaluating
    /// collection (which may hold other, non-mirrored edits and so cannot simply be cleared wholesale).
    /// </summary>
    protected override void ClearItems()
    {
        if (_evaluatingCollection != null)
        {
            foreach (EditRef_t<T> item in Items)
            {
                _evaluatingCollection.Remove(item);
            }
        }

        base.ClearItems();
    }

    /// <summary>
    /// Determines whether an EditRef with the specified ID is in the collection.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <returns>
    /// 	<c>true</c> if [has edit ref] [the specified id]; otherwise, <c>false</c>.
    /// </returns>
    public bool HasEditRef(string id)
    {
        return Contains(id);
    }

    /// <summary>
    /// Gets the key for items in this collection, i.e., the Edit_t ID.
    /// </summary>
    /// <param name="item">EditRef_t.</param>
    /// <returns>Edit_t ID.</returns>
    protected override string GetKeyForItem(EditRef_t<T> item)
    {
        return item.Id;
    }
}

