// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;
using System.Xml.Linq;

namespace FixPortal.FixAtdl.Xml.Serialization;

/// <summary>
/// Represents a placeholder element definition used for recursive type graphs.
/// </summary>
public class RecursiveTypeElementDefinition : ElementDefinition
{
    /// <summary>
    /// Initializes a new <see cref="RecursiveTypeElementDefinition"/>.
    /// </summary>
    public RecursiveTypeElementDefinition()
        : base(null, null, null)
    {
    }

    /// <summary>
    /// Gets a value that is not supported for recursive placeholder definitions.
    /// </summary>
    public new XName ElementName
    {
        get => throw new NotSupportedException(); set => throw new NotSupportedException();
    }

    /// <summary>
    /// Gets a value that is not supported for recursive placeholder definitions.
    /// </summary>
    public new Type TargetType
    {
        get => throw new NotSupportedException(); set => throw new NotSupportedException();
    }

    /// <summary>
    /// Gets a value that is not supported for recursive placeholder definitions.
    /// </summary>
    public new ElementAttribute[] Attributes
    {
        get => throw new NotSupportedException(); set => throw new NotSupportedException();
    }

    /// <summary>
    /// Gets a value that is not supported for recursive placeholder definitions.
    /// </summary>
    public new ConstructorParameter[] ConstructorParameters
    {
        get => throw new NotSupportedException(); set => throw new NotSupportedException();
    }

    /// <summary>
    /// Gets a value that is not supported for recursive placeholder definitions.
    /// </summary>
    public new ChildElementDefinition[] ChildElements
    {
        get => throw new NotSupportedException(); set => throw new NotSupportedException();
    }

    /// <summary>
    /// Gets a value that is not supported for recursive placeholder definitions.
    /// </summary>
    public new CacheElementValueInstruction CacheElementValueInstruction
    {
        get => throw new NotSupportedException(); set => throw new NotSupportedException();
    }
}

