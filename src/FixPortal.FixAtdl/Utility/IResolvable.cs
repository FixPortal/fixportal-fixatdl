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
#pragma warning disable S3246 // Generic type parameters should be co/contravariant when possible
// TValueSource cannot be made variant. It appears only as ISimpleDictionary<TValueSource> on the Resolve
// parameter; ISimpleDictionary<out T> is itself covariant, so feeding TValueSource into that covariant
// slot as a *method input* puts it in a contravariant position. Marking it 'out' fails CS1961 (must be
// contravariantly valid), and 'in' fails CS1961 in reverse (the covariant ISimpleDictionary forbids it).
// Neither modifier compiles, so the rule is unsatisfiable here (batch 5, Sonar disposition).
public interface IResolvable<THost, TValueSource>
{
    /// <summary>
    /// Resolves deferred references against the supplied host and source collection.
    /// </summary>
    /// <param name="host">The host object providing resolution context.</param>
    /// <param name="sourceCollection">The source collection used to resolve referenced values.</param>
    void Resolve(THost host, ISimpleDictionary<TValueSource> sourceCollection);
}
#pragma warning restore S3246
