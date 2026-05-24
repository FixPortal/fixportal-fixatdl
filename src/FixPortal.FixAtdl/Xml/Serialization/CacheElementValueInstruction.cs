#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

namespace FixPortal.FixAtdl.Xml.Serialization;

/// <summary>
/// Represents a deserialization instruction that provides a mechanism to cache an element within the input stream,
/// so that it can be used elsewhere in the deserialization process.
/// </summary>
public class CacheElementValueInstruction(string cacheKey)
{
    /// <summary>
    /// Gets the key for this instruction.
    /// </summary>
    public string CacheKey { get; private set; } = cacheKey;
}

