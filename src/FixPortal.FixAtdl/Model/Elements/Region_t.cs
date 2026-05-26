// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Enumerations;

namespace FixPortal.FixAtdl.Model.Elements;

/// <summary>
/// Represents a region inclusion or exclusion entry and its associated countries.
/// </summary>
public class Region_t
{
    /// <summary>
    /// Gets or sets the region name.
    /// </summary>
    public Region Name { get; set; }

    /// <summary>
    /// Gets or sets whether the region is included or excluded.
    /// </summary>
    public Inclusion_t Inclusion { get; set; }

    /// <summary>
    /// Gets the countries associated with the region entry.
    /// </summary>
    public CountryCollection Countries
    {
        get
        {
            // Lazy initialize as we can't use 'this' in constructor.
            field ??= new CountryCollection(this);

            return field;
        }
    } = null!;
}
