#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Resources;
using ThrowHelper = FixPortal.FixAtdl.Diagnostics.ThrowHelper;

namespace FixPortal.FixAtdl.Model.Collections;

public class ListItemCollection : KeyedCollection<string, ListItem_t>
{
    public void CopyFrom(List<ListItem_t> items)
    {
        foreach (ListItem_t item in items)
            Add(item);
    }

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

    public string[] EnumIds
    {
        get { return [.. (from item in Items select item.EnumId)]; }
    }

    public bool HasItems
    {
        get { return Count > 0; }
    }

    protected override string GetKeyForItem(ListItem_t item)
    {
        return item.EnumId;
    }
}

