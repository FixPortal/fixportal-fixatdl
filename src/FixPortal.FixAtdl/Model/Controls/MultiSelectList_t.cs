// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Model.Controls.Support;

namespace FixPortal.FixAtdl.Model.Controls;

/// <summary>
/// Represents the MultiSelectList_t control element within FIXatdl.
/// </summary>
public class MultiSelectList_t : ListControlBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="MultiSelectList_t"/> using the supplied ID.
    /// </summary>
    /// <param name="id">ID for this control.</param>
    public MultiSelectList_t(string id)
        : base(id)
    {
    }
}
