// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

namespace FixPortal.FixAtdl.Model.Types;

/// <summary>
/// 'float field representing a price offset, which can be mathematically added to a "Price". Note the number of decimal
/// places may vary and some fields such as LastForwardPoints may be negative.'
/// </summary>
/// <remarks>
/// The FIXatdl 1.1 Errata (20101221 p.32, Parameter/@minValue default table) assigns this type a default
/// <c>minValue</c> of 0, which the constructor applies: negative offsets are rejected unless the parameter
/// declares an explicit negative bound such as <c>minValue="-1"</c>.
/// </remarks>
public class PriceOffset_t : Float_t
{
    /// <summary>Initializes the FIXatdl default minimum of zero.</summary>
    public PriceOffset_t()
    {
        MinValue = 0;
    }
}
