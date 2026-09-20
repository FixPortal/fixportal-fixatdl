// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

namespace FixPortal.FixAtdl.Model.Enumerations;

/// <summary>
/// FIXatdl Inclusion type.
/// </summary>
public enum Inclusion_t
{
    /// <summary>
    /// Include.
    /// </summary>
    Include,

    /// <summary>
    /// Exclude.
    /// </summary>
    Exclude,
}
