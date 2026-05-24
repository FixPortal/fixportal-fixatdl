#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Model.Collections;

namespace FixPortal.FixAtdl.Utility;

public interface IResolvable<Thost, Tvaluesource>
{
    void Resolve(Thost host, ISimpleDictionary<Tvaluesource> sourceCollection);
}

