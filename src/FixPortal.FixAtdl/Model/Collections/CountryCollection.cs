// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Collections;
using FixPortal.FixAtdl.Diagnostics;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Model.Reference;
using FixPortal.FixAtdl.Resources;

namespace FixPortal.FixAtdl.Model.Collections;

/// <summary>
/// Collection for storing instances of Country_t, enforcing that each country belongs to the region
/// the collection was created for.
/// </summary>
/// <remarks>This type encapsulates a <see cref="HashSet{T}"/> rather than inheriting it: a public
/// <c>new Add</c> shadowing HashSet.Add was bypassable via base-typed access, ICollection.Add,
/// UnionWith or a collection initializer, all of which skipped the region check.</remarks>
public class CountryCollection : IEnumerable<Country_t>
{
    private readonly Region _region;
    private readonly HashSet<Country_t> _countries = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="CountryCollection"/> class.
    /// </summary>
    /// <param name="region">The region.</param>
    public CountryCollection(Region_t region)
    {
        _region = region.Name;
    }

    /// <summary>
    /// Gets the number of countries in the collection.
    /// </summary>
    public int Count => _countries.Count;

    /// <summary>
    /// Determines whether the collection contains the specified country.
    /// </summary>
    /// <param name="item">The country to locate.</param>
    /// <returns><see langword="true"/> if present; otherwise, <see langword="false"/>.</returns>
    public bool Contains(Country_t item) => _countries.Contains(item);

    /// <summary>
    /// Adds the specified item, validating that it belongs in the region specified when this collection was created.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns><see langword="true"/> if the country was added; <see langword="false"/> if it was already present.</returns>
    public bool Add(Country_t item)
    {
        if (!IsCountryInRegion(item))
        {
            throw ThrowHelper.New<ArgumentException>(this, ErrorMessages.InvalidAttemptToAddCountryToRegion,
                Enum.GetName(item.CountryCode), Enum.GetName(_region));
        }

        // Surface the add/duplicate result rather than silently swallowing it.
        return _countries.Add(item);
    }

    /// <inheritdoc />
    public IEnumerator<Country_t> GetEnumerator() => _countries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // Region is a [Flags] enum (e.g. Region.All = APJ | EMEA | TheAmericas), so a composite region
    // must be tested with HasFlag against each constituent rather than matched as a single value —
    // the previous single-value switch fell through to "reject everything" for any composite region.
    private bool IsCountryInRegion(Country_t item)
    {
        bool countryOkay = false;

        if (_region.HasFlag(Region.AsiaPacificJapan))
        {
            countryOkay |= Regions.AsiaPacificJapanCountries.Contains(item.CountryCode);
        }

        if (_region.HasFlag(Region.EuropeMiddleEastAfrica))
        {
            countryOkay |= Regions.EuropeMiddleEastAfricaCountries.Contains(item.CountryCode);
        }

        if (_region.HasFlag(Region.TheAmericas))
        {
            countryOkay |= Regions.TheAmericasCountries.Contains(item.CountryCode);
        }

        return countryOkay;
    }
}
