// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Model.Collections;

namespace FixPortal.FixAtdl.Utility;

/// <summary>
/// Provides deferred resolution of references once the model graph is fully loaded.
/// </summary>
/// <typeparam name="THost">The host object used for resolution.</typeparam>
/// <typeparam name="TValueSource">The source item type available during resolution.</typeparam>
public interface IResolvable<THost, TValueSource>
{
    /// <summary>
    /// Resolves deferred references against the supplied host and source collection.
    /// </summary>
    /// <param name="host">The host object providing resolution context.</param>
    /// <param name="sourceCollection">The source collection used to resolve referenced values.</param>
    void Resolve(THost host, ISimpleDictionary<TValueSource> sourceCollection);
}
