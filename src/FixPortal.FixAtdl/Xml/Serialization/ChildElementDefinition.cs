// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Xml.Linq;

namespace FixPortal.FixAtdl.Xml.Serialization;

/// <summary>
/// Describes how a child XML element is attached to a parent object during deserialization.
/// </summary>
public class ChildElementDefinition
{
    /// <summary>
    /// Gets the definition of the child element.
    /// </summary>
    public ElementDefinition ElementDefinition { get; private set; }

    /// <summary>
    /// Gets the optional container element name that wraps the child.
    /// </summary>
    public XName? ContainerElementName { get; private set; }

    /// <summary>
    /// Gets the target property on the parent object.
    /// </summary>
    public string ContainerProperty { get; private set; }

    /// <summary>
    /// Gets the type of the target property on the parent object.
    /// </summary>
    public Type ContainerPropertyType { get; private set; }

    /// <summary>
    /// Gets the method used to add or assign the child object.
    /// </summary>
    public object ContainerMethod { get; private set; }

    /// <summary>
    /// Initializes a new <see cref="ChildElementDefinition"/> for a wrapped child element.
    /// </summary>
    /// <param name="containerElementDefinition">The container element definition.</param>
    /// <param name="containerProperty">The parent property that receives the child.</param>
    /// <param name="containerPropertyType">The type of the parent property.</param>
    /// <param name="containerMethod">The standard container method to use.</param>
    public ChildElementDefinition(ContainerElementDefinition containerElementDefinition, string containerProperty, Type containerPropertyType, StandardContainerMethod containerMethod)
    {
        ContainerElementName = containerElementDefinition.ElementName;
        ElementDefinition = containerElementDefinition.ChildDefinition;
        ContainerProperty = containerProperty;
        ContainerPropertyType = containerPropertyType;
        ContainerMethod = containerMethod;
    }

    /// <summary>
    /// Initializes a new <see cref="ChildElementDefinition"/> for a direct child element.
    /// </summary>
    /// <param name="elementDefinition">The child element definition.</param>
    /// <param name="containerProperty">The parent property that receives the child.</param>
    /// <param name="containerPropertyType">The type of the parent property.</param>
    /// <param name="containerMethod">The standard container method to use.</param>
    public ChildElementDefinition(ElementDefinition elementDefinition, string containerProperty, Type containerPropertyType, StandardContainerMethod containerMethod)
    {
        ElementDefinition = elementDefinition;
        ContainerProperty = containerProperty;
        ContainerPropertyType = containerPropertyType;
        ContainerMethod = containerMethod;
    }

    /// <summary>
    /// Initializes a new <see cref="ChildElementDefinition"/> for a direct child element using a named container method.
    /// </summary>
    /// <param name="elementDefinition">The child element definition.</param>
    /// <param name="containerProperty">The parent property that receives the child.</param>
    /// <param name="containerPropertyType">The type of the parent property.</param>
    /// <param name="containerMethod">The container method name to invoke.</param>
    public ChildElementDefinition(ElementDefinition elementDefinition, string containerProperty, Type containerPropertyType, string containerMethod)
    {
        ElementDefinition = elementDefinition;
        ContainerProperty = containerProperty;
        ContainerPropertyType = containerPropertyType;
        ContainerMethod = containerMethod;
    }
}
