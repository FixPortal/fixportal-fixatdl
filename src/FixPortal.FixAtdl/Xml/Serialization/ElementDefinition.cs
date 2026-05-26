// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Xml.Linq;

namespace FixPortal.FixAtdl.Xml.Serialization;

/// <summary>
/// Describes how an XML element maps to a target CLR type during deserialization.
/// </summary>
public class ElementDefinition
{
    /// <summary>
    /// Gets or sets the XML element name.
    /// </summary>
    public XName? ElementName { get; set; }

    /// <summary>
    /// Gets the target CLR type created for this element.
    /// </summary>
    public Type? TargetType { get; private set; }

    /// <summary>
    /// Gets the attribute mappings for the element.
    /// </summary>
    public ElementAttribute[]? Attributes { get; private set; }

    /// <summary>
    /// Gets the constructor parameters used to create the target object.
    /// </summary>
    public ConstructorParameter[]? ConstructorParameters { get; private set; }

    /// <summary>
    /// Gets the child element mappings for the element.
    /// </summary>
    public ChildElementDefinition[]? ChildElements { get; private set; }

    /// <summary>
    /// Gets the optional instruction for caching the created element value.
    /// </summary>
    public CacheElementValueInstruction? CacheElementValueInstruction { get; private set; }

    /// <summary>
    /// Initializes a new <see cref="ElementDefinition"/> with attribute mappings only.
    /// </summary>
    /// <param name="elementName">The XML element name.</param>
    /// <param name="targetType">The target CLR type.</param>
    /// <param name="attributes">The attribute mappings.</param>
    public ElementDefinition(XName? elementName, Type? targetType, ElementAttribute[]? attributes)
        : this(elementName, targetType, null, attributes, [], null)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="ElementDefinition"/> with a single child mapping.
    /// </summary>
    /// <param name="elementName">The XML element name.</param>
    /// <param name="targetType">The target CLR type.</param>
    /// <param name="attributes">The attribute mappings.</param>
    /// <param name="child">The child element mapping.</param>
    public ElementDefinition(XName elementName, Type targetType, ElementAttribute[] attributes, ChildElementDefinition child)
        : this(elementName, targetType, null, attributes, [child], null)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="ElementDefinition"/> with child mappings.
    /// </summary>
    /// <param name="elementName">The XML element name.</param>
    /// <param name="targetType">The target CLR type.</param>
    /// <param name="attributes">The attribute mappings.</param>
    /// <param name="children">The child element mappings.</param>
    public ElementDefinition(XName elementName, Type targetType, ElementAttribute[] attributes, ChildElementDefinition[] children)
        : this(elementName, targetType, null, attributes, children, null)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="ElementDefinition"/> with child mappings and element-value caching.
    /// </summary>
    /// <param name="elementName">The XML element name.</param>
    /// <param name="targetType">The target CLR type.</param>
    /// <param name="attributes">The attribute mappings.</param>
    /// <param name="children">The child element mappings.</param>
    /// <param name="cacheInstruction">The caching instruction.</param>
    public ElementDefinition(XName elementName, Type targetType, ElementAttribute[] attributes, ChildElementDefinition[] children,
        CacheElementValueInstruction cacheInstruction)
        : this(elementName, targetType, null, attributes, children, cacheInstruction)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="ElementDefinition"/> with constructor parameters and attribute mappings.
    /// </summary>
    /// <param name="elementName">The XML element name.</param>
    /// <param name="targetType">The target CLR type.</param>
    /// <param name="constructorParameters">The constructor parameter mappings.</param>
    /// <param name="attributes">The attribute mappings.</param>
    public ElementDefinition(XName elementName, Type targetType, ConstructorParameter[] constructorParameters, ElementAttribute[] attributes)
        : this(elementName, targetType, constructorParameters, attributes, [], null)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="ElementDefinition"/> with constructor parameters and a single child mapping.
    /// </summary>
    /// <param name="elementName">The XML element name.</param>
    /// <param name="targetType">The target CLR type.</param>
    /// <param name="constructorParameters">The constructor parameter mappings.</param>
    /// <param name="attributes">The attribute mappings.</param>
    /// <param name="child">The child element mapping.</param>
    public ElementDefinition(XName elementName, Type targetType, ConstructorParameter[] constructorParameters, ElementAttribute[] attributes,
        ChildElementDefinition child)
        : this(elementName, targetType, constructorParameters, attributes, [child], null)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="ElementDefinition"/> with constructor parameters and child mappings.
    /// </summary>
    /// <param name="elementName">The XML element name.</param>
    /// <param name="targetType">The target CLR type.</param>
    /// <param name="constructorParameters">The constructor parameter mappings.</param>
    /// <param name="attributes">The attribute mappings.</param>
    /// <param name="children">The child element mappings.</param>
    public ElementDefinition(XName elementName, Type targetType, ConstructorParameter[] constructorParameters,
        ElementAttribute[] attributes, ChildElementDefinition[] children)
        : this(elementName, targetType, constructorParameters, attributes, children, null)
    {
    }

    /// <summary>
    /// Initializes a new <see cref="ElementDefinition"/> with the supplied mappings.
    /// </summary>
    /// <param name="elementName">The XML element name.</param>
    /// <param name="targetType">The target CLR type.</param>
    /// <param name="constructorParameters">The constructor parameter mappings.</param>
    /// <param name="attributes">The attribute mappings.</param>
    /// <param name="children">The child element mappings.</param>
    /// <param name="cacheInstruction">The optional caching instruction.</param>
    public ElementDefinition(XName? elementName, Type? targetType, ConstructorParameter[]? constructorParameters,
        ElementAttribute[]? attributes, ChildElementDefinition[]? children, CacheElementValueInstruction? cacheInstruction)
    {
        ElementName = elementName;
        TargetType = targetType;
        ConstructorParameters = constructorParameters;
        Attributes = attributes;
        ChildElements = children;
        CacheElementValueInstruction = cacheInstruction;
    }
}
