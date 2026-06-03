// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Collections.ObjectModel;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Resources;
using ThrowHelper = FixPortal.FixAtdl.Diagnostics.ThrowHelper;

namespace FixPortal.FixAtdl.Model.Collections;

/// <summary>
/// Collection used to represent a set of EnumPairs.
/// </summary>
public class EnumPairCollection : KeyedCollection<string, EnumPair_t>
{
    /// <summary>
    /// Gets the key for the specified enum pair item.
    /// </summary>
    /// <param name="item">The enum pair item.</param>
    /// <returns>The enum identifier.</returns>
    protected override string GetKeyForItem(EnumPair_t item)
    {
        return item.EnumId;
    }

    /// <summary>
    /// Gets the full set of EnumIds.
    /// </summary>
    /// <value>The enum ids.</value>
    public string[] EnumIds => [.. from item in Items select item.EnumId];

    /// <summary>
    /// Gets a value indicating whether this instance has values.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this instance has values; otherwise, <c>false</c>.
    /// </value>
    public bool HasValues => Count > 0;

    /// <summary>
    /// Gets the wire value from enum id.
    /// </summary>
    /// <param name="enumId">The enum id.</param>
    /// <returns></returns>
    public string GetWireValueFromEnumId(string enumId)
    {
        // The KeyedCollection indexer throws a raw KeyNotFoundException on a miss (it never returns
        // null), so the old null-check was dead. Check membership first and surface the domain error.
        if (!Contains(enumId))
        {
            throw ThrowHelper.New<InvalidOperationException>(this, ErrorMessages.EnumerationNotFound, enumId);
        }

        string wireValue = this[enumId].WireValue;
        if (string.IsNullOrEmpty(wireValue))
        {
            throw ThrowHelper.New<InvalidOperationException>(this, "EnumPair '{0}' has a null or empty wireValue.", enumId);
        }

        return wireValue;
    }

    /// <summary>
    /// Tries to parse the supplied wire value.
    /// </summary>
    /// <param name="wireValue">The wire value.</param>
    /// <param name="enumId">The enum id.</param>
    /// <returns>true if the values could be parsed; false otherwise.</returns>
    public bool TryParseWireValue(string wireValue, out string? enumId)
    {
        enumId = null;
        string testValue = wireValue ?? Atdl.NullValue;

        foreach (EnumPair_t enumPair in this)
        {
            if (enumPair.WireValue == testValue)
            {
                enumId = enumPair.EnumId;

                return true;
            }
        }

        return false;
    }
}
