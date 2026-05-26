// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Xml.Linq;

namespace FixPortal.FixAtdl.Xml.Serialization;

/// <summary>
/// Describes an element definition whose inner generic type is selected from an XML attribute.
/// </summary>
public class GenericTypeElementDefinition : ElementDefinition
{
    /// <summary>
    /// Gets the XML attribute that specifies the inner generic type.
    /// </summary>
    public XName AttributeForInnerType { get; private set; }

    /// <summary>
    /// Gets the namespace prefix used when resolving inner type names.
    /// </summary>
    public string InnerTypeNamespace { get; private set; }

    /// <summary>
    /// Gets the attribute mappings to apply for each resolved inner type.
    /// </summary>
    public Dictionary<Type, ElementAttribute[]> InnerTypeToAttributesMap { get; private set; }

    /// <summary>
    /// Initializes a new <see cref="GenericTypeElementDefinition"/>.
    /// </summary>
    /// <param name="elementName">The XML element name.</param>
    /// <param name="outerType">The outer generic CLR type.</param>
    /// <param name="attributeForInnerType">The attribute that identifies the inner type.</param>
    /// <param name="innerTypeNamespace">The namespace prefix used to resolve inner type names.</param>
    /// <param name="constructorParameters">The constructor parameter mappings.</param>
    /// <param name="commonAttributes">The attribute mappings common to all inner types.</param>
    /// <param name="attributeDictionary">The inner-type-specific attribute mappings.</param>
    /// <param name="children">The child element mappings.</param>
    public GenericTypeElementDefinition(XName elementName, Type outerType, XName attributeForInnerType,
        string innerTypeNamespace, ConstructorParameter[] constructorParameters, ElementAttribute[] commonAttributes,
        Dictionary<Type, ElementAttribute[]> attributeDictionary, ChildElementDefinition[] children)
        : base(elementName, outerType, constructorParameters, commonAttributes, children)
    {
        AttributeForInnerType = attributeForInnerType;
        InnerTypeNamespace = innerTypeNamespace;
        InnerTypeToAttributesMap = attributeDictionary;
    }
}
