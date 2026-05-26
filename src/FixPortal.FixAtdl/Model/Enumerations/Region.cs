// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

namespace FixPortal.FixAtdl.Model.Enumerations;

/// <summary>
/// Enumeration for the regions within FIXatdl.
/// </summary>
[Flags]
public enum Region
{
    /// <summary>No region.</summary>
    None = 0,

    /// <summary>The Americas region.</summary>
    TheAmericas = 1,

    /// <summary>The Europe, Middle East and Africa region.</summary>
    EuropeMiddleEastAfrica = 2,

    /// <summary>The Asia Pacific and Japan region.</summary>
    AsiaPacificJapan = 4,

    /// <summary>All regions.</summary>
    All = AsiaPacificJapan | EuropeMiddleEastAfrica | TheAmericas
}

