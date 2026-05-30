// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Model.Controls.Support;

namespace FixPortal.FixAtdl.Model.Controls;

/// <summary>
/// Represents the TextField_t control element within FIXatdl.
/// </summary>
public class TextField_t : TextControlBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="TextField_t"/> using the supplied ID.
    /// </summary>
    /// <param name="id">ID for this control.</param>
    public TextField_t(string id)
        : base(id)
    {
    }
}
