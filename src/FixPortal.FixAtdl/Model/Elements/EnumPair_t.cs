// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

namespace FixPortal.FixAtdl.Model.Elements;

/// <summary>
/// Represents a FIXatdl EnumPair_t.<br/>
/// Defines a legal value of a parameter in the form of a wire value. A Parameter element will have an EnumPair element for each
/// enumerated value which the parameter can take.
/// </summary>
public class EnumPair_t
{
    /// <summary>A unique identifier of an enumPair element per parameter.</summary>
    public string EnumId { get; set; } = null!;

    /// <summary>The corresponding value that is used to populate the FIX message.</summary>
    public string WireValue { get; set; } = null!;

    /// <summary>
    /// Optional vendor extension attribute: an integer ordering hint for this enum pair. Standard
    /// FIXatdl 1.1 <c>EnumPair</c> carries only <c>enumID</c> and <c>wireValue</c>; <c>index</c> is a
    /// captured extension for lossless fidelity and does NOT affect the FIX wire value. Null when the
    /// source ATDL did not supply an <c>index</c>.
    /// </summary>
    public int? Index { get; set; }
}

