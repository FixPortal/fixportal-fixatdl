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
    private CountryCollection _countries = null!;

    public Region Name { get; set; }
    public Inclusion_t Inclusion { get; set; }

    public CountryCollection Countries
    {
        get
        {
            // Lazy initialize as we can't use 'this' in constructor.
            if (_countries == null)
                _countries = new CountryCollection(this);

            return _countries;
        }
    }
}

