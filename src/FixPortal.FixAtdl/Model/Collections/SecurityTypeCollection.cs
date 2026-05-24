#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Collections.ObjectModel;
using FixPortal.FixAtdl.Model.Elements;

namespace FixPortal.FixAtdl.Model.Collections;

public class SecurityTypeCollection : KeyedCollection<string, SecurityType_t>
{
    protected override string GetKeyForItem(SecurityType_t item)
    {
        return item.Name;
    }
}

