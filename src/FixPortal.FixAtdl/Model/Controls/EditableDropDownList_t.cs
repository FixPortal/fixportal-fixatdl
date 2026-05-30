// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Model.Controls.Support;

namespace FixPortal.FixAtdl.Model.Controls;

/// <summary>
/// Represents the EditableDropDownList_t control element within FIXatdl.
/// </summary>
public class EditableDropDownList_t : ListControlBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="EditableDropDownList_t"/> using the supplied ID.
    /// </summary>
    /// <param name="id">ID for this control.</param>
    public EditableDropDownList_t(string id)
        : base(id)
    {
    }

    /// <summary>
    /// Indicates whether the EnumState value for this control can be set to a value other than one of the enumerated
    /// values.
    /// </summary>
    protected override bool IsNonEnumValueAllowed => true;
}
