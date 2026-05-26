// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

namespace FixPortal.FixAtdl.Model.Elements;

/// <summary>
/// Represents a selectable enumerated value for a list-based control.
/// </summary>
public class ListItem_t
{
    /// <summary>
    /// Gets or sets the FIXatdl enum identifier for the item.
    /// </summary>
    public string EnumId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the UI representation shown for the item.
    /// </summary>
    public string UiRep { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the item is currently selected.
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    /// Returns the UI representation of the item.
    /// </summary>
    /// <returns>The UI representation.</returns>
    public override string ToString()
    {
        return UiRep;
    }
}
