#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

namespace FixPortal.FixAtdl.Model.Types;

/// <summary>
/// 'float field capable of storing either a whole number (no decimal places) of "shares" (securities denominated in 
/// whole units) or a decimal value containing decimal places for non-share quantity asset classes (securities 
/// denominated in fractional units).'
/// </summary>
public class Qty_t : Float_t
{
}

