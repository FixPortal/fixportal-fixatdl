// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Model.Controls.Support;
using FixPortal.FixAtdl.Model.Enumerations;

namespace FixPortal.FixAtdl.Model.Controls;

/// <summary>
/// Represents the CheckBoxList_t control element within FIXatdl.
/// </summary>
public class CheckBoxList_t : ListControlBase, IOrientableControl
{
    /// <summary>
    /// Initializes a new instance of <see cref="CheckBoxList_t"/> using the supplied ID.
    /// </summary>
    /// <param name="id">ID for this control.</param>
    public CheckBoxList_t(string id)
        : base(id)
    {
    }

    #region IOrientableControl Members

    /// <summary>Must be “HORIZONTAL” or “VERTICAL”. Declares the orientation of the radio buttons within a RadioButtonList
    ///  or the checkboxes within a CheckBoxList.  Applicable when xsi:type is RadioButtonList_t or CheckBoxList_t.</summary>
    public Orientation_t? Orientation { get; set; }

    #endregion
}
