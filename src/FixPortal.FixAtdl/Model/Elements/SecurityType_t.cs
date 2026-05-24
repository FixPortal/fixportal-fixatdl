#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Model.Enumerations;

namespace FixPortal.FixAtdl.Model.Elements;

public class SecurityType_t
{
    public string Name { get; set; } = null!;

    public Inclusion_t Inclusion { get; set; }
}

