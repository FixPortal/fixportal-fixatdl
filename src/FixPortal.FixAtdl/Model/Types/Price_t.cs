// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

namespace FixPortal.FixAtdl.Model.Types;

/// <summary>
/// 'float field representing a price. Note the number of decimal places may vary. For certain asset classes prices
/// may be negative values. For example, prices for options strategies can be negative under certain market conditions.
/// Refer to Volume 7: FIX Usage by Product for asset classes that support negative price values.'
/// </summary>
/// <remarks>
/// The FIXatdl 1.1 Errata (20101221 p.32, Parameter/@minValue default table) assigns this type a default
/// <c>minValue</c> of 0, which the constructor applies: negative prices are rejected unless the parameter
/// declares an explicit negative bound such as <c>minValue="-1"</c>.
/// </remarks>
public class Price_t : Float_t
{
    /// <summary>Initializes the FIXatdl default minimum of zero.</summary>
    public Price_t()
    {
        MinValue = 0;
    }
}
