// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Collections.ObjectModel;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Model.Reference;

namespace FixPortal.FixAtdl.Model.Collections;

/// <summary>
/// Represents the collection of regions to which a strategy applies.
/// </summary>
public class RegionCollection : KeyedCollection<Region, Region_t>
{
    /// <summary>
    /// Gets the key for the specified region item.
    /// </summary>
    /// <param name="region">The region item.</param>
    /// <returns>The region identifier.</returns>
    protected override Region GetKeyForItem(Region_t region)
    {
        return region.Name;
    }

    /// <summary>
    /// Gets a bitmask of the applicable regions for a given strategy.
    /// </summary>
    /// <returns>A bitmask of applicable Regions.</returns>
    /// <remarks>If no region information is provided, the strategy is assumed applicable to all regions. If the
    /// collection contains only excluded regions, then all regions <em>except</em> those excluded are applicable
    /// (per the FIXatdl inclusion/exclusion semantics); if it contains any included region, that union forms the
    /// base set (Include takes precedence over Exclude) from which any excluded regions are then removed.</remarks>
    public Region GetApplicableRegions()
    {
        if (Count == 0)
        {
            return Region.All;
        }

        Region includes = Region.None;
        Region excludes = Region.None;

        foreach (Region_t region in this)
        {
            if (region.Inclusion == Inclusion_t.Include)
            {
                includes |= region.Name;
            }
            else
            {
                excludes |= region.Name;
            }
        }

        // An exclude-only configuration means "all regions except those excluded", so the base set is every
        // region; otherwise the explicitly-included union is the base set. Excluded regions are then removed
        // from whichever base applies. (Previously only Include entries were ORed in, so an exclude-only
        // configuration incorrectly resolved to Region.None — applicable to nothing.)
        Region baseRegions = includes != Region.None ? includes : Region.All;

        return baseRegions & ~excludes;
    }

    /// <summary>
    /// Determines whether a given strategy is applicable for a specific country.
    /// </summary>
    /// <param name="country">Country to check for.</param>
    /// <returns>true if the strategy is applicable for the specified country; false otherwise.</returns>
    /// <remarks>A country-level entry (a <see cref="Country_t"/> inside a region) is the most specific statement
    /// and overrides the region-level disposition for that country — for example an excluded country inside an
    /// otherwise-included region, or an included country inside an otherwise-excluded region. When no country-level
    /// entry matches, applicability is decided at region granularity.</remarks>
    public bool IsApplicableTo(IsoCountryCode country)
    {
        foreach (Region_t region in this)
        {
            // Filter explicitly (first matching country wins, preserving enumeration order) rather than a
            // foreach+if implicit filter — also clears CodeQL cs/linq-missed-where.
            Country_t? countryEntry = region.Countries.FirstOrDefault(c => c.CountryCode == country);

            if (countryEntry != null)
            {
                return countryEntry.Inclusion == Inclusion_t.Include;
            }
        }

        Region applicableRegions = GetApplicableRegions();
        Region targetRegion = Regions.GetRegionForCountry(country);

        return (applicableRegions & targetRegion) != Region.None;
    }
}
