#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Collections.ObjectModel;
using FixPortal.FixAtdl.Model.Elements;

namespace FixPortal.FixAtdl.Model.Collections;

public class StrategyCollection : KeyedCollection<string, Strategy_t>
{
    private readonly Strategies_t _owner;

    public StrategyCollection(Strategies_t owner)
    {
        _owner = owner;
    }

    protected override string GetKeyForItem(Strategy_t strategy)
    {
        return strategy.Name;
    }

    public new void Add(Strategy_t item)
    {
        item.Parent = _owner;

        base.Add(item);
    }
}

