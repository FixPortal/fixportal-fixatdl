// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Model.Controls.Support;

namespace FixPortal.FixAtdl.Model.Controls;

/// <summary>
/// Represents the CheckBox_t control element within FIXatdl.
/// </summary>
public class CheckBox_t : BinaryControlBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="CheckBox_t"/> using the supplied ID.
    /// </summary>
    /// <param name="id">ID for this control.</param>
    public CheckBox_t(string id)
        : base(id)
    {
    }
}
