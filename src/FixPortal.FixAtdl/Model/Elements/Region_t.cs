// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Enumerations;

namespace FixPortal.FixAtdl.Model.Elements;

public class Region_t
{
    public Region Name { get; set; }
    public Inclusion_t Inclusion { get; set; }

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

