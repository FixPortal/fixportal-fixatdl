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

    // The NumberStyles for parsing a FIX decimal string. This is NumberStyles.Number minus
    // AllowThousands: a FIX decimal wire value cannot legally carry a thousands separator, so accepting one
    // means reading "1,5" as 15 — silently, and by a factor of ten. The WPF adapter already rejects it, and
    // Edit_t's FIX-field conversion already stated its styles explicitly for this reason (R21); this
    // constant is that decision made once for every decimal parse in the model.
    internal const NumberStyles FixDecimalStyles =
        NumberStyles.AllowLeadingWhite
        | NumberStyles.AllowTrailingWhite
        | NumberStyles.AllowLeadingSign
        | NumberStyles.AllowDecimalPoint;
}
