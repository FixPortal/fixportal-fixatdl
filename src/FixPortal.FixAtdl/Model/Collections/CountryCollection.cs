// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;
using System.Collections.Generic;
using FixPortal.FixAtdl.Diagnostics;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Model.Reference;
using FixPortal.FixAtdl.Resources;

namespace FixPortal.FixAtdl.Model.Collections;

/// <summary>
/// Collection for storing instances of Country_t.
/// </summary>
public class CountryCollection : HashSet<Country_t>
{
    private readonly Region _region;

    /// <summary>
    /// Initializes a new instance of the <see cref="CountryCollection"/> class.
    /// </summary>
    /// <param name="region">The region.</param>
    public CountryCollection(Region_t region)
    {
        _region = region.Name;
    }

    /// <summary>
    /// Adds the specified item, validating that it belongs in the region specified when this collection was created.
    /// </summary>
    /// <param name="item">The item.</param>
    public new void Add(Country_t item)
    {
        bool countryOkay = false;

        switch (_region)
        {
            case Region.AsiaPacificJapan:
                countryOkay = Regions.AsiaPacificJapanCountries.Contains(item.CountryCode);
                break;

            case Region.EuropeMiddleEastAfrica:
                countryOkay = Regions.EuropeMiddleEastAfricaCountries.Contains(item.CountryCode);
                break;

            case Region.TheAmericas:
                countryOkay = Regions.TheAmericasCountries.Contains(item.CountryCode);
                break;
        }

        if (!countryOkay)
        {
            throw ThrowHelper.New<ArgumentException>(this, ErrorMessages.InvalidAttemptToAddCountryToRegion,
                Enum.GetName(item.CountryCode), Enum.GetName(_region));
        }

        base.Add(item);
    }
}

