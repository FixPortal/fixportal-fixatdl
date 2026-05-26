// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Collections.ObjectModel;
using FixPortal.FixAtdl.Model.Elements;

namespace FixPortal.FixAtdl.Model.Collections;

/// <summary>
/// Represents the collection of security types to which a strategy applies.
/// </summary>
public class SecurityTypeCollection : KeyedCollection<string, SecurityType_t>
{
    /// <summary>
    /// Gets the key for the specified security type item.
    /// </summary>
    /// <param name="item">The security type item.</param>
    /// <returns>The security type name.</returns>
    protected override string GetKeyForItem(SecurityType_t item)
    {
        return item.Name;
    }
}
