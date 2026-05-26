// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Xml.Linq;

namespace FixPortal.FixAtdl.Xml.Serialization;

/// <summary>
/// Describes a container XML element that wraps a child element definition.
/// </summary>
public class ContainerElementDefinition : ElementDefinition
{
    /// <summary>
    /// Gets the definition of the child element contained within this wrapper element.
    /// </summary>
    public ElementDefinition ChildDefinition { get; private set; }

    /// <summary>
    /// Initializes a new <see cref="ContainerElementDefinition"/>.
    /// </summary>
    /// <param name="elementName">The XML element name of the container.</param>
    /// <param name="childDefinition">The child element definition within the container.</param>
    public ContainerElementDefinition(XName elementName, ElementDefinition childDefinition)
        : base(elementName, null, null, null, null, null)
    {
        ChildDefinition = childDefinition;
    }
}
