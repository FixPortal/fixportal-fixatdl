// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Model.Enumerations;

namespace FixPortal.FixAtdl.Model.Controls.Support;

/// <summary>
/// Exposes an orientation setting for controls that support layout direction.
/// </summary>
public interface IOrientableControl
{
    /// <summary>
    /// Gets the control orientation.
    /// </summary>
    Orientation_t? Orientation { get; }
}
