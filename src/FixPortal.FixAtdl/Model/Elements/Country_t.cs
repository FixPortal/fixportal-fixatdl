#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Model.Reference;

namespace FixPortal.FixAtdl.Model.Elements;

public class Country_t
{
    public IsoCountryCode CountryCode { get; set; }

    public Inclusion_t Inclusion { get; set; }
}

