// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Model.Controls.Support;

namespace FixPortal.FixAtdl.Model.Controls;

/// <summary>
/// Represents the RadioButton_t control element within FIXatdl.
/// </summary>
public class RadioButton_t : BinaryControlBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="RadioButton_t"/> using the supplied ID.
    /// </summary>
    /// <param name="id">ID for this control.</param>
    public RadioButton_t(string id)
        : base(id)
    {
    }

    /// <summary>Identifies a common group name used by a set of RadioButton_t among which only one radio button 
    /// may be selected at a time.  Applicable when xsi:type is RadioButton_t.</summary>
    public string RadioGroup { get; set; } = null!;
}
