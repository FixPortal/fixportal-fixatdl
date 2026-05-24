#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

namespace FixPortal.FixAtdl.Utility;

internal interface IBindable<T>
{
    void Bind(T target);
}

