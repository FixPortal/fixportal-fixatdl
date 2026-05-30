// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Model.Controls.Support;

namespace FixPortal.FixAtdl.Model.Controls;

/// <summary>
/// Represents the Slider_t control element within FIXatdl.
/// </summary>
/// <remarks>The FIXatdl 1.1 specification is a little unclear on what a Slider_t can do.  The current Atdl4net implementation supports
/// selecting from a set of options (ListItems) but not selecting a numerical value.</remarks>
public class Slider_t : ListControlBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="Slider_t"/> using the supplied ID.
    /// </summary>
    /// <param name="id">ID for this control.</param>
    public Slider_t(string id)
        : base(id)
    {
    }
}
