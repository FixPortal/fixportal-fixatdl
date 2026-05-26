// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Model.Reference;

namespace FixPortal.FixAtdl.Model.Elements;

/// <summary>
/// Represents a country inclusion or exclusion entry.
/// </summary>
public class Country_t
{
    /// <summary>
    /// Gets or sets the ISO country code.
    /// </summary>
    public IsoCountryCode CountryCode { get; set; }

    /// <summary>
    /// Gets or sets whether the country is included or excluded.
    /// </summary>
    public Inclusion_t Inclusion { get; set; }
}
