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
/// Indicates whether an XML attribute is required during deserialization.
/// </summary>
public enum Required
{
    /// <summary>
    /// The attribute must be present.
    /// </summary>
    Mandatory,

    /// <summary>
    /// The attribute may be omitted.
    /// </summary>
    Optional
}

/// <summary>
/// Describes how an XML attribute maps to a target property.
/// </summary>
public class ElementAttribute
{
    /// <summary>
    /// Gets the XML attribute name.
    /// </summary>
    public XName XmlName { get; private set; }

    /// <summary>
    /// Gets the target property name.
    /// </summary>
    public string Property { get; private set; }

    /// <summary>
    /// Gets the target CLR type.
    /// </summary>
    public Type Type { get; private set; }

    /// <summary>
    /// Gets the mapping of XML text values to enum values.
    /// </summary>
    public Dictionary<string, Enum> EnumValues { get; private set; } = null!;

    /// <summary>
    /// Gets whether the attribute is required.
    /// </summary>
    public Required Required { get; private set; }

    /// <summary>
    /// Initializes a new <see cref="ElementAttribute"/> for a non-enum attribute.
    /// </summary>
    /// <param name="xmlName">The XML attribute name.</param>
    /// <param name="property">The target property name.</param>
    /// <param name="type">The target CLR type.</param>
    /// <param name="required">Whether the attribute is required.</param>
    public ElementAttribute(string xmlName, string property, Type type, Required required)
    {
        XmlName = xmlName;
        Property = property;
        Required = required;
        Type = type;
    }

    /// <summary>
    /// Initializes a new <see cref="ElementAttribute"/> for an enum-backed attribute.
    /// </summary>
    /// <param name="xmlName">The XML attribute name.</param>
    /// <param name="property">The target property name.</param>
    /// <param name="enumDefinition">The enum definition used to parse XML values.</param>
    /// <param name="required">Whether the attribute is required.</param>
    public ElementAttribute(string xmlName, string property, EnumDefinition enumDefinition, Required required)
    {
        XmlName = xmlName;
        Property = property;
        Required = required;
        Type = enumDefinition.EnumType;
        EnumValues = enumDefinition.TextValues;
    }
}
