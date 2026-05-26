// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;

namespace FixPortal.FixAtdl.Xml.Serialization;

/// <summary>
/// Identifies where a constructor argument value is sourced from during deserialization.
/// </summary>
public enum SourceType
{
    /// <summary>
    /// The value is read from an XML attribute.
    /// </summary>
    ElementAttribute,

    /// <summary>
    /// The value is taken from the parent object.
    /// </summary>
    ParentObject,

    /// <summary>
    /// The value is taken from a previously cached object.
    /// </summary>
    NamedPredecessor
}

/// <summary>
/// Describes a constructor argument required to create a target object during deserialization.
/// </summary>
public class ConstructorParameter
{
    /// <summary>
    /// Gets the constructor argument type.
    /// </summary>
    public Type Type { get; private set; }

    /// <summary>
    /// Gets the source kind for the constructor argument.
    /// </summary>
    public SourceType SourceType { get; private set; }

    /// <summary>
    /// Gets the attribute name or cache key used to obtain the argument value.
    /// </summary>
    public string Source { get; private set; }

    /// <summary>
    /// Initializes a new <see cref="ConstructorParameter"/>.
    /// </summary>
    /// <param name="type">The constructor argument type.</param>
    /// <param name="sourceType">The source kind for the argument value.</param>
    /// <param name="source">The attribute name or cache key used to obtain the value.</param>
    public ConstructorParameter(Type type, SourceType sourceType, string source)
    {
        Type = type;
        SourceType = sourceType;
        Source = source;
    }
}
