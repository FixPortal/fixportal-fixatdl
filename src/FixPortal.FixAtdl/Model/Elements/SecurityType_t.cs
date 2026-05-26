// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Model.Enumerations;

namespace FixPortal.FixAtdl.Model.Elements;

/// <summary>
/// Represents a security-type inclusion or exclusion entry.
/// </summary>
public class SecurityType_t
{
    /// <summary>
    /// Gets or sets the security type name.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets whether the security type is included or excluded.
    /// </summary>
    public Inclusion_t Inclusion { get; set; }
}
