// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;
using FixPortal.FixAtdl.Diagnostics;
using FixPortal.FixAtdl.Resources;

namespace FixPortal.FixAtdl.Model.Elements;

public class ListItem_t : IComparable
{
    public string EnumId { get; set; } = null!;
    public string UiRep { get; set; } = null!;
    public bool IsSelected { get; set; }

    public int CompareTo(object? obj)
    {
        if (obj is string)
        {
            return string.Compare(EnumId, obj as string, StringComparison.Ordinal);
        }
        else
        {
            throw ThrowHelper.New<InvalidOperationException>(this, ErrorMessages.CompareValueFailure, "ListItem_t", obj?.GetType().FullName ?? "(null)");
        }
    }

    public override string ToString()
    {
        return UiRep;
    }
}
