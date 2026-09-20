// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

namespace FixPortal.FixAtdl.Model.Enumerations;

/// <summary>
/// Fixatdl Use type.
/// </summary>
public enum Use_t
{
    /// <summary>
    /// Parameter is mandatory.
    /// </summary>
    Required,

    /// <summary>
    /// Parameter is optional.
    /// </summary>
    Optional,
}
