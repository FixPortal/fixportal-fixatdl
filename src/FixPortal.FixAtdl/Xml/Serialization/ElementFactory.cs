// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using FixPortal.FixAtdl.Diagnostics;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ThrowHelper = FixPortal.FixAtdl.Diagnostics.ThrowHelper;

namespace FixPortal.FixAtdl.Xml.Serialization;

public class ElementFactory : INotifyClassDeserialized
{
    // FP Enhancement: 2026-05-23 — TODO wire injected logger when refactoring class to accept ILogger.
    private static readonly ILogger _log = NullLogger.Instance;

    private const string ExceptionContext = "ElementFactory";

    private readonly Type _notifyCreationOfType;
    private readonly ElementDefinition _elementDefinition;
    private readonly Dictionary<string, object> _elementValueCache = [];

    public ElementFactory(ElementDefinition elementDefinition, Type notifyCreationOfType)
    {
        _log.LogDebug("ElementFactory created; root ElementName='{ElementName}'.", elementDefinition.ElementName);

        _elementDefinition = elementDefinition;
        _notifyCreationOfType = notifyCreationOfType;
    }

    public object DeserializeElement(XElement element)
    {
        _log.LogDebug("DeserializeElement called; first 50 characters of XML='{XmlSnippet}'.", element.ToString()[..50]);

        return CreateObject(_elementDefinition, element, null);
    }

    private object CreateObject(ElementDefinition definition, XElement sourceElement, object? parentObject)
    {
        _log.LogDebug("CreateObject(ElementDefinition, XElement) called; ElementName='{ElementName}.'", definition.ElementName);


        GetConstructorParameters(definition, sourceElement, parentObject, out Type[] constructorParameterTypes, out object[] constructorParameterValues);

        object newObject = CreateRawObject(definition.TargetType!, constructorParameterTypes, constructorParameterValues);

        if (definition.CacheElementValueInstruction != null)
        {
            _elementValueCache[definition.CacheElementValueInstruction.CacheKey] = newObject;
        }

        IEnumerable<XAttribute> attributes = sourceElement.Attributes();

        try
        {
            ProcessAttributes(definition.TargetType!, definition.Attributes!, attributes, newObject);
        }
        catch (FixAtdlException ex)
        {
            throw ThrowHelper.Rethrow(this, ex, new ExceptionInfo(sourceElement), ErrorMessages.GeneralElementProcessingError, string.Empty);
        }

        ProcessChildren(definition, sourceElement, newObject);

        if (newObject.GetType() == _notifyCreationOfType)
        {
            NotifyClassDeserialized(_notifyCreationOfType, newObject);
        }

        return newObject;
    }

    /// <summary>
    /// Creates and populates a new instance of the generic type given in the supplied GenericTypeElementDefinition,
    /// using the supplied XML element as the source.
    /// </summary>
    /// <param name="genericTypeDefinition">The definition of the generic type to be created.</param>
    /// <param name="sourceElement">The source XML element for this object.</param>
    /// <param name="parentObject">The parent object of this object.</param>
    /// <returns>A new instance of the required type, typically of the form SomeType&lt;&gt;.</returns>
    /// <remarks>The inner type of the target type is specified via an attribute on the supplied input XML element.</remarks>
    /// <exception cref="MissingMandatoryValueException">Thrown when...<ul>
    /// <li></li>
    /// </ul></exception>
    private object CreateObject(GenericTypeElementDefinition genericTypeDefinition, XElement sourceElement, object? parentObject)
    {
        _log.LogDebug("CreateObject(GenericTypeElementDefinition, XElement) called; ElementName='{ElementName}'.", genericTypeDefinition.ElementName);


        GetConstructorParameters(genericTypeDefinition, sourceElement, parentObject, out Type[] constructorParameterTypes, out object[] constructorParameterValues);


        string? innerTypeName = ReadAttribute(sourceElement.Attributes(), genericTypeDefinition.AttributeForInnerType, typeof(string)) as string;

        if (string.IsNullOrEmpty(innerTypeName))
        {
            throw ThrowHelper.New<MissingMandatoryValueException>(this, new ExceptionInfo(sourceElement), ErrorMessages.MissingMandatoryAttribute,
                genericTypeDefinition.AttributeForInnerType.LocalName, genericTypeDefinition.ElementName!.LocalName);
        }

        Type innerType = string.IsNullOrEmpty(genericTypeDefinition.InnerTypeNamespace)
            ? Type.GetType(innerTypeName)!
            : Type.GetType(string.Format(CultureInfo.InvariantCulture, "{0}.{1}", genericTypeDefinition.InnerTypeNamespace, innerTypeName))!;

        if (innerType == null)
        {
            throw ThrowHelper.New<InvalidFieldValueException>(this, new ExceptionInfo(sourceElement), ErrorMessages.UnrecognisedTypeError, innerTypeName,
                genericTypeDefinition.AttributeForInnerType.LocalName, genericTypeDefinition.ElementName!.LocalName);
        }

        object newObject = CreateRawObject(genericTypeDefinition.TargetType!, [innerType], constructorParameterTypes, constructorParameterValues);

        IEnumerable<XAttribute> attributes = sourceElement.Attributes();

        try
        {
            ProcessAttributes(newObject.GetType(), genericTypeDefinition.Attributes!, attributes, newObject);
            ProcessAttributes(newObject.GetType(), genericTypeDefinition.InnerTypeToAttributesMap[innerType], attributes, newObject);
        }
        catch (MissingMandatoryValueException ex)
        {
            throw ThrowHelper.Rethrow(this, ex, new ExceptionInfo(sourceElement), ErrorMessages.GeneralElementProcessingError, string.Empty);
        }

        ProcessChildren(genericTypeDefinition, sourceElement, newObject);

        if (newObject.GetType() == _notifyCreationOfType)
        {
            NotifyClassDeserialized(_notifyCreationOfType, newObject);
        }

        return newObject;
    }

    private object CreateObject(MultiTypeElementDefinition multiTypeDefinition, XElement sourceElement, object? parentObject)
    {
        _log.LogDebug("CreateObject(MultiTypeElementDefinition, XElement) called; ElementName='{ElementName}'.", multiTypeDefinition.ElementName);


        GetConstructorParameters(multiTypeDefinition, sourceElement, parentObject, out Type[] constructorParameterTypes, out object[] constructorParameterValues);


        string? typeName = ReadAttribute(sourceElement.Attributes(), multiTypeDefinition.AttributeForType, typeof(string)) as string;

        if (string.IsNullOrEmpty(typeName))
        {
            throw ThrowHelper.New<MissingMandatoryValueException>(this, new ExceptionInfo(sourceElement), ErrorMessages.MissingMandatoryAttribute,
                multiTypeDefinition.AttributeForType.LocalName, multiTypeDefinition.ElementName!.LocalName);
        }

        // If the value for the typename is in an XML namespace, remove it.
        if (typeName.Contains(':') && typeName.IndexOf(':') < typeName.Length - 1)
        {
            typeName = typeName[(typeName.IndexOf(':') + 1)..];
        }

        Type targetType = string.IsNullOrEmpty(multiTypeDefinition.TypeNamespace)
            ? Type.GetType(typeName)!
            : Type.GetType(string.Format(CultureInfo.InvariantCulture, "{0}.{1}", multiTypeDefinition.TypeNamespace, typeName))!;

        if (targetType == null)
        {
            throw ThrowHelper.New<InvalidFieldValueException>(this, new ExceptionInfo(sourceElement), ErrorMessages.UnrecognisedTypeError, typeName,
                multiTypeDefinition.AttributeForType.LocalName, multiTypeDefinition.ElementName!.LocalName);
        }

        object newObject = CreateRawObject(targetType, constructorParameterTypes, constructorParameterValues);

        IEnumerable<XAttribute> attributes = sourceElement.Attributes();

        try
        {
            ProcessAttributes(newObject.GetType(), multiTypeDefinition.Attributes!, attributes, newObject);
            ProcessAttributes(newObject.GetType(), multiTypeDefinition.TypeToAttributesMap[targetType], attributes, newObject);
        }
        catch (FixAtdlException ex)
        {
            throw ThrowHelper.Rethrow(this, ex, new ExceptionInfo(sourceElement), ErrorMessages.GeneralElementProcessingError, string.Empty);
        }

        ProcessChildren(multiTypeDefinition, sourceElement, newObject);

        if (newObject.GetType() == _notifyCreationOfType)
        {
            NotifyClassDeserialized(_notifyCreationOfType, newObject);
        }

        return newObject;
    }

    private static object CreateRawObject(Type outerType, Type[] innerTypes, Type[] argTypes, params object[] args)
    {
        _log.LogDebug("CreateObject(Type, Type[], Type[], params object[]) called (creating generic type); Outer type={OuterType}.", outerType.FullName);

        Type specificType = outerType.MakeGenericType(innerTypes);

        ConstructorInfo? classConstructor = specificType.GetConstructor(argTypes);

        if (classConstructor == null)
        {
            throw ThrowHelper.New<InternalErrorException>(ExceptionContext, InternalErrors.NoConstructorFoundForSpecifiedArgumentTypes, outerType.FullName!);
        }

        return classConstructor.Invoke(args);
    }

    private static object CreateRawObject(Type targetType, Type[] argTypes, params object[] args)
    {
        _log.LogDebug("CreateObject(Type, Type[], params object[]) called; Type={TargetType}.", targetType.FullName);

        ConstructorInfo? classConstructor = targetType.GetConstructor(argTypes);

        if (classConstructor == null)
        {
            throw ThrowHelper.New<InternalErrorException>(ExceptionContext, InternalErrors.NoConstructorFoundForSpecifiedArgumentTypes, targetType.FullName!);
        }

        return classConstructor.Invoke(args);
    }

    private void GetConstructorParameters(ElementDefinition elementDefinition, XElement sourceElement, object? parentObject,
        out Type[] constructorParameterTypes, out object[] constructorParameterValues)
    {
        _log.LogDebug("GetConstructorParameters called; ElementName='{ElementName}'.", elementDefinition.ElementName);

        if (elementDefinition.ConstructorParameters != null)
        {
            constructorParameterValues = new object[elementDefinition.ConstructorParameters.Length];
            constructorParameterTypes = new Type[elementDefinition.ConstructorParameters.Length];

            for (int n = 0; n < elementDefinition.ConstructorParameters.Length; n++)
            {
                switch (elementDefinition.ConstructorParameters[n].SourceType)
                {
                    case SourceType.ElementAttribute:
                        constructorParameterValues[n] = ReadAttribute(sourceElement.Attributes(), elementDefinition.ConstructorParameters[n].Source, elementDefinition.ConstructorParameters[n].Type);
                        break;

                    case SourceType.ParentObject:
                        constructorParameterValues[n] = parentObject!;
                        break;

                    case SourceType.NamedPredecessor:
                        {

                            if (_elementValueCache.TryGetValue(elementDefinition.ConstructorParameters[n].Source, out object? value))
                            {
                                constructorParameterValues[n] = value;
                            }
                        }
                        break;
                }

                constructorParameterTypes[n] = elementDefinition.ConstructorParameters[n].Type;
            }
        }
        else
        {
            constructorParameterValues = [];
            constructorParameterTypes = [];
        }
    }

    private void ProcessAttributes(Type targetType, ElementAttribute[] attributeDefinitions, IEnumerable<XAttribute> attributes, object target)
    {
        _log.LogDebug("ProcessAttributes called; Target type={TargetType}.", targetType.FullName);

        foreach (ElementAttribute attrDefn in attributeDefinitions)
        {
            object? value = attrDefn.Type.IsEnum && attrDefn.EnumValues != null
                ? ReadAttribute(attributes, attrDefn.XmlName, attrDefn.Type, attrDefn.EnumValues)
                : ReadAttribute(attributes, attrDefn.XmlName, attrDefn.Type);

            if (attrDefn.Required == Required.Mandatory && value == null)
            {
                throw ThrowHelper.New<MissingMandatoryValueException>(this, ErrorMessages.MissingMandatoryAttribute,
                    attrDefn.XmlName.LocalName, targetType.Name);
            }

            if (value == null)
            {
                continue;
            }

            // Process indirect properties (only one level of indirect is supported).
            if (attrDefn.Property.Contains('.'))
            {
                string[] names = attrDefn.Property.Split(['.']);

                if (names.Length != 2)
                {
                    throw ThrowHelper.New<InternalErrorException>(this, InternalErrors.InvalidPropertyIndirection, attrDefn.Property);
                }

                PropertyInfo outerProperty = targetType.GetProperty(names[0])!;

                if (outerProperty == null)
                {
                    throw ThrowHelper.New<InternalErrorException>(this, InternalErrors.PropertyNotFoundOnObjectInternal, names[0], targetType.FullName!);
                }

                object innerObject = outerProperty.GetValue(target, null)!;

                if (innerObject == null)
                {
                    throw ThrowHelper.New<InternalErrorException>(this, InternalErrors.UnableToRetrievePropertyValueOnObject, attrDefn.Property, targetType.FullName!);
                }

                PropertyInfo property = outerProperty.PropertyType.GetProperty(names[1])!;

                if (property == null)
                {
                    throw ThrowHelper.New<InvalidPropertyOnObjectException>(this, ErrorMessages.PropertyNotFoundOnObject, attrDefn.Property, targetType.Name);
                }

                SetPropertyValue(property, innerObject, value);
            }
            else
            {
                PropertyInfo property = targetType.GetProperty(attrDefn.Property)!;

                if (property == null)
                {
                    throw ThrowHelper.New<InvalidPropertyOnObjectException>(this, ErrorMessages.PropertyNotFoundOnObject, attrDefn.Property, targetType.Name);
                }

                SetPropertyValue(property, target, value);
            }
        }
    }

    private void ProcessChildren(ElementDefinition definition, XElement sourceElement, object target)
    {
        _log.LogDebug("ProcessChildren called; ElementName='{ElementName}'", definition.ElementName);

        // We have to reflect the target type as we can't rely on the Definition to contain it (e.g. MultiTypeElementDefinition).
        Type targetType = target.GetType();


        foreach (ChildElementDefinition childDefinition in definition.ChildElements!)
        {
            bool isRecursiveDefinition = childDefinition.ElementDefinition is RecursiveTypeElementDefinition;
            bool hasContainerElement = !isRecursiveDefinition && childDefinition.ContainerElementName != null;

            IEnumerable<XElement> matchingChildElements;

            ElementDefinition targetDefinition = isRecursiveDefinition ? definition : childDefinition.ElementDefinition;

            if (hasContainerElement)
            {
                XElement? containerElement = (from e in sourceElement.Elements(childDefinition.ContainerElementName) select e).FirstOrDefault();

                if (containerElement == null)
                {
                    return;
                }

                matchingChildElements = from e in containerElement.Elements(childDefinition.ElementDefinition.ElementName) select e;
            }
            else
            {
                matchingChildElements = from e in sourceElement.Elements(targetDefinition.ElementName) select e;
            }

            foreach (XElement childElement in matchingChildElements)
            {
                object childObject = targetDefinition is GenericTypeElementDefinition genericDefinition
                    ? CreateObject(genericDefinition, childElement, target)
                    : targetDefinition is MultiTypeElementDefinition multiDefinition
                        ? CreateObject(multiDefinition, childElement, target)
                        : CreateObject(targetDefinition, childElement, target);

                PropertyInfo property = targetType.GetProperty(childDefinition.ContainerProperty)!;

                if (property == null)
                {
                    throw ThrowHelper.New<InternalErrorException>(this, InternalErrors.PropertyNotFoundOnObjectInternal,
                        childDefinition.ContainerProperty, targetType.FullName!);
                }

                try
                {
                    // For the case of MultiTypeElementDefinition we must use the reflected type
                    ProcessChildProperty(childDefinition, property, targetDefinition.TargetType ?? childObject.GetType(), target, childObject);
                }
                catch (FixAtdlException ex)
                {
                    throw ThrowHelper.Rethrow(this, ex, new ExceptionInfo(childElement), ErrorMessages.GeneralElementProcessingError,
                        definition.ElementName!.LocalName);
                }
                catch (ArgumentException ex)
                {
                    throw ThrowHelper.New<FixAtdlException>(this, ex, new ExceptionInfo(childElement), ErrorMessages.GeneralElementProcessingError,
                        definition.ElementName!.LocalName, ex.Message);
                }
            }
        }
    }

    private void ProcessChildProperty(ChildElementDefinition childDefinition, PropertyInfo property, Type targetType, object target, object childObject)
    {
        _log.LogDebug("ProcessChildProperty called; ElementName='{ElementName}', Property={Property}.", childDefinition.ElementDefinition.ElementName, property.Name);

        string containerMethod;

        if (childDefinition.ContainerMethod is StandardContainerMethod method)
        {
            if (method == StandardContainerMethod.Assign)
            {
                SetPropertyValue(property, target, childObject);

                return;
            }
            else
            {
                containerMethod = Enum.GetName(typeof(StandardContainerMethod), childDefinition.ContainerMethod)!;
            }
        }
        else
        {
            containerMethod = (childDefinition.ContainerMethod as string)!;
        }

        MethodInfo targetMethod = property.PropertyType.GetMethod(containerMethod!, [targetType])!;

        if (targetMethod == null)
        {
            throw ThrowHelper.New<InternalErrorException>(this, InternalErrors.ContainerMethodNotFoundOnObject,
                containerMethod, targetType.FullName!);
        }

        try
        {
            targetMethod.Invoke(property.GetValue(target, null), [childObject]);
        }
        catch (TargetInvocationException ex)
        {
            if (ex.InnerException != null)
            {
                throw ThrowHelper.Rethrow(this, ex.InnerException, ErrorMessages.UnableToInvokeMethodError,
                    string.Format(CultureInfo.InvariantCulture, "the {0} method on the {1} property", containerMethod, property.Name));
            }
            else
            {
                throw;
            }
        }
    }

    private static object ReadAttribute(IEnumerable<XAttribute> attributes, XName attributeName, Type type)
    {
        _log.LogDebug("ReadAttribute(IEnumerable<XAttribute>, XName, Type) called; Attribute name='{AttributeName}'", attributeName);

        XAttribute? attribute = attributes.FirstOrDefault(a => a.Name == attributeName);

        if (attribute == null)
        {
            return null!;
        }

        // NB Most simple enums are dealt with in the other overload of ReadAttribute.
        if (type.IsEnum)
        {
            try
            {
                return Enum.Parse(type, attribute.Value);
            }
            catch (ArgumentException ex)
            {
                throw ThrowHelper.New<InvalidFieldValueException>(ExceptionContext, ex, ErrorMessages.InvalidValueEnumParseFailure, attribute.Value, type.Name);
            }
        }
        else
        {
            try
            {
                return ValueConverter.ConvertTo(attribute.Value, type);
            }
            catch (FormatException ex)
            {
                throw ThrowHelper.New<InvalidFieldValueException>(ExceptionContext, ex, ErrorMessages.DataConversionError2,
                    attribute.Value, type.Name, attributeName.LocalName);
            }
        }
    }

    private static object ReadAttribute(IEnumerable<XAttribute> attributes, XName attributeName, Type enumType, Dictionary<string, Enum> enumValues)
    {
        _log.LogDebug("ReadAttribute(IEnumerable<XAttribute>, XName, Type, Dictionary<string, Enum>) called; Attribute name='{AttributeName}'", attributeName);

        XAttribute? attribute = attributes.FirstOrDefault(a => a.Name == attributeName);

        if (attribute == null)
        {
            return null!;
        }

        if (!enumValues.TryGetValue(attribute.Value, out Enum? enumValue))
        {
            throw ThrowHelper.New<InvalidFieldValueException>(ExceptionContext, ErrorMessages.InvalidValueEnumParseFailure, attribute.Value, enumType.Name);
        }

        return Enum.ToObject(enumType, enumValue);
    }

    private static void SetPropertyValue(PropertyInfo property, object target, object value)
    {
        _log.LogDebug("SetPropertyValue called; Target object type={TargetType}, property={Property}, value='{Value}'.", target.GetType().FullName, property.Name, value);

        try
        {
            if (property.PropertyType == value.GetType())
            {
                property.SetValue(target, value, null);
            }
            else
            {
                object newValue = CreateRawObject(property.PropertyType, [value.GetType()], value);

                property.SetValue(target, newValue, null);
            }
        }
        catch (ArgumentException ex)
        {
            throw ThrowHelper.New<InternalErrorException>(ExceptionContext, ex, InternalErrors.UnableToSetPropertyValueOnObject,
                property.Name, value, target.GetType().FullName!);
        }
    }

    #region INotifyClassDeserialized Members & Support Methods

    private void NotifyClassDeserialized(Type classType, object extraInfo)
    {
        ClassDeserialized?.Invoke(this, new ClassDeserializedEventArgs(classType, extraInfo));
    }

    public event EventHandler<ClassDeserializedEventArgs>? ClassDeserialized;

    #endregion
}
