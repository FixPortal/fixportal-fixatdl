#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;
using Atdl4net.Diagnostics;
using Atdl4net.Resources;

namespace Atdl4net.Model.Elements;

public class ListItem_t : IComparable
{
    public string EnumId { get; set; }
    public string UiRep { get; set; }
    public bool IsSelected { get; set; }

    public int CompareTo(object? obj)
    {
        if (obj is string)
            return (EnumId).CompareTo(obj as string);
        else
            throw ThrowHelper.New<InvalidOperationException>(this, ErrorMessages.CompareValueFailure, "ListItem_t", obj?.GetType().FullName ?? "(null)");
    }

    public override string ToString()
    {
        return UiRep;
    }
}
