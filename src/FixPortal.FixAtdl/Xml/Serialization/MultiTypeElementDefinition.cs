// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace FixPortal.FixAtdl.Xml.Serialization;

/// <summary>
/// Describes an element definition whose runtime target type is selected from an XML attribute.
/// </summary>
public class MultiTypeElementDefinition : ElementDefinition
{
    /// <summary>
    /// Gets the XML attribute that specifies the runtime type.
    /// </summary>
    public XName AttributeForType { get; private set; }

    /// <summary>
    /// Gets the namespace prefix used when resolving runtime type names.
    /// </summary>
    public string TypeNamespace { get; private set; }

    /// <summary>
    /// Gets the attribute mappings to apply for each resolved runtime type.
    /// </summary>
    public Dictionary<Type, ElementAttribute[]> TypeToAttributesMap { get; private set; }

    /// <summary>
    /// Initializes a new <see cref="MultiTypeElementDefinition"/>.
    /// </summary>
    /// <param name="elementName">The XML element name.</param>
    /// <param name="attributeForType">The attribute that identifies the runtime type.</param>
    /// <param name="typeNamespace">The namespace prefix used to resolve type names.</param>
    /// <param name="constructorParameters">The constructor parameter mappings.</param>
    /// <param name="commonAttributes">The attribute mappings common to all runtime types.</param>
    /// <param name="attributeDictionary">The type-specific attribute mappings.</param>
    /// <param name="children">The child element mappings.</param>
    public MultiTypeElementDefinition(XName elementName, XName attributeForType, string typeNamespace,
        ConstructorParameter[] constructorParameters, ElementAttribute[] commonAttributes,
        Dictionary<Type, ElementAttribute[]> attributeDictionary, ChildElementDefinition[] children)
        : base(elementName, (Type?)null, constructorParameters, commonAttributes, children, null)
    {
        AttributeForType = attributeForType;
        TypeNamespace = typeNamespace;
        TypeToAttributesMap = attributeDictionary;
    }
}
