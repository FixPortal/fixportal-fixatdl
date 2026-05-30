// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Model.Controls.Support;

namespace FixPortal.FixAtdl.Model.Controls;

/// <summary>
/// Represents the Label_t control element within FIXatdl.
/// </summary>
public class Label_t : TextControlBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="Label_t"/> using the supplied ID.
    /// </summary>
    /// <param name="id">ID for this control.</param>
    public Label_t(string id)
        : base(id)
    {
    }

    /// <summary>
    /// Loads this control with any supplied InitValue. If InitValue is not supplied, then control value will
    /// be set to the Label value, if that is set, or if neither are set to an empty value.
    /// </summary>
    protected override void LoadDefaultFromInitValue()
    {
        // InitValue takes precedence over Label
        SetValue(InitValue != null ? InitValue : Label);
    }
}
