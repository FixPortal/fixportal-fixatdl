// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Globalization;

namespace FixPortal.FixAtdl.Model;

/// <summary>
/// Provides access to a set of constants useful throughout the model.
/// </summary>
public static class Atdl
{
    /// <summary>
    /// The FIXatdl 'null' value for controls as used in state rules.
    /// </summary>
    public const string NullValue = "{NULL}";

    // Shared styles for decimal parses of FIX/control text. This is NumberStyles.Number minus
    // AllowThousands: a FIX decimal wire value cannot legally carry a thousands separator, so accepting one
    // means reading "1,5" as 15 — silently, and by a factor of ten. Format-specific parsers may still
    // use their own styles when their wire grammar differs.
    internal const NumberStyles FixDecimalStyles =
        NumberStyles.AllowLeadingWhite
        | NumberStyles.AllowTrailingWhite
        | NumberStyles.AllowLeadingSign
        | NumberStyles.AllowDecimalPoint;
}
