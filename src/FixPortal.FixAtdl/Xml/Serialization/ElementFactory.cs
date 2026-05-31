// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Globalization;
using System.Reflection;
using System.Xml.Linq;
using FixPortal.FixAtdl.Diagnostics;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ThrowHelper = FixPortal.FixAtdl.Diagnostics.ThrowHelper;

namespace FixPortal.FixAtdl.Xml.Serialization;

/// <summary>
/// Creates CLR objects from FIXatdl XML according to an <see cref="ElementDefinition"/>.
/// </summary>
public class ElementFactory : INotifyClassDeserialized
{
    private readonly ILogger<ElementFactory> _log;

    private const string ExceptionContext = "ElementFactory";

    private readonly Type _notifyCreationOfType;
    private readonly ElementDefinition _elementDefinition;

    // NamedPredecessor cache for a single deserialization pass. Cleared at the start of each
    // DeserializeElement call so reuse of a factory cannot leak cached objects across parses.
    // Not thread-safe: a factory instance must be used by one thread / one parse at a time.
    private readonly Dictionary<string, object> _elementValueCache = [];

    /// <summary>
    /// Initializes a new <see cref="ElementFactory"/>.
    /// </summary>
    /// <param name="elementDefinition">The root element definition used for deserialization.</param>
    /// <param name="notifyCreationOfType">The type whose creation should raise <see cref="ClassDeserialized"/>.</param>
    /// <param name="loggerFactory">Optional logger factory; when null, no logging is produced.</param>
    public ElementFactory(ElementDefinition elementDefinition, Type notifyCreationOfType, ILoggerFactory? loggerFactory = null)
    {
        _log = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<ElementFactory>();

        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("ElementFactory created; root ElementName='{ElementName}'.", elementDefinition.ElementName);
        }

        _elementDefinition = elementDefinition;
        _notifyCreationOfType = notifyCreationOfType;
    }

    /// <summary>
    /// Deserializes the supplied XML element into a CLR object graph.
    /// </summary>
    /// <param name="element">The XML element to deserialize.</param>
    /// <returns>The deserialized object graph.</returns>
    public object DeserializeElement(XElement element)
    {
        if (_log.IsEnabled(LogLevel.Debug))
        {
            string xml = element.ToString();
            _log.LogDebug("DeserializeElement called; first 50 characters of XML='{XmlSnippet}'.", xml[..Math.Min(50, xml.Length)]);
        }

        // Start each pass with an empty NamedPredecessor cache so a reused factory cannot carry
        // cached objects across parses.
        _elementValueCache.Clear();

        return CreateObject(_elementDefinition, element, null);
    }

    private object CreateObject(ElementDefinition definition, XElement sourceElement, object? parentObject)
    {
        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("CreateObject(ElementDefinition, XElement) called; ElementName='{ElementName}.'", definition.ElementName);
        }


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
        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("CreateObject(GenericTypeElementDefinition, XElement) called; ElementName='{ElementName}'.", genericTypeDefinition.ElementName);
        }


        GetConstructorParameters(genericTypeDefinition, sourceElement, parentObject, out Type[] constructorParameterTypes, out object[] constructorParameterValues);


        string? innerTypeName = ReadAttribute(sourceElement.Attributes(), genericTypeDefinition.AttributeForInnerType, typeof(string)) as string;

        if (string.IsNullOrEmpty(innerTypeName))
        {
            throw ThrowHelper.New<MissingMandatoryValueException>(this, new ExceptionInfo(sourceElement), ErrorMessages.MissingMandatoryAttribute,
                genericTypeDefinition.AttributeForInnerType.LocalName, genericTypeDefinition.ElementName!.LocalName);
        }

        // If the inner-type name is in an XML namespace, remove it (mirrors the MultiType overload).
        if (innerTypeName.Contains(':') && innerTypeName.IndexOf(':') < innerTypeName.Length - 1)
        {
            innerTypeName = innerTypeName[(innerTypeName.IndexOf(':') + 1)..];
        }

        Type? innerType = string.IsNullOrEmpty(genericTypeDefinition.InnerTypeNamespace)
            ? Type.GetType(innerTypeName)
            : Type.GetType(string.Format(CultureInfo.InvariantCulture, "{0}.{1}", genericTypeDefinition.InnerTypeNamespace, innerTypeName));

        if (innerType == null)
        {
            throw ThrowHelper.New<InvalidFieldValueException>(this, new ExceptionInfo(sourceElement), ErrorMessages.UnrecognisedTypeError, innerTypeName,
                genericTypeDefinition.AttributeForInnerType.LocalName, genericTypeDefinition.ElementName!.LocalName);
        }

        // SECURITY: gate on the allow-list (InnerTypeToAttributesMap) BEFORE constructing the type.
        // Constructing a namespace-pinned, ctor-matching but un-mapped type from untrusted ATDL could
        // trigger its constructor / type-initializer side effects before rejection.
        if (!genericTypeDefinition.InnerTypeToAttributesMap.TryGetValue(innerType, out ElementAttribute[]? innerTypeAttributes))
        {
            throw ThrowHelper.New<InvalidFieldValueException>(this, new ExceptionInfo(sourceElement), ErrorMessages.UnrecognisedTypeError, innerTypeName,
                genericTypeDefinition.AttributeForInnerType.LocalName, genericTypeDefinition.ElementName!.LocalName);
        }

        object newObject = CreateRawObject(genericTypeDefinition.TargetType!, [innerType], constructorParameterTypes, constructorParameterValues);

        IEnumerable<XAttribute> attributes = sourceElement.Attributes();

        try
        {
            ProcessAttributes(newObject.GetType(), genericTypeDefinition.Attributes!, attributes, newObject);
            ProcessAttributes(newObject.GetType(), innerTypeAttributes, attributes, newObject);
        }
        catch (FixAtdlException ex)
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
        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("CreateObject(MultiTypeElementDefinition, XElement) called; ElementName='{ElementName}'.", multiTypeDefinition.ElementName);
        }


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

        Type? targetType = string.IsNullOrEmpty(multiTypeDefinition.TypeNamespace)
            ? Type.GetType(typeName)
            : Type.GetType(string.Format(CultureInfo.InvariantCulture, "{0}.{1}", multiTypeDefinition.TypeNamespace, typeName));

        if (targetType == null)
        {
            throw ThrowHelper.New<InvalidFieldValueException>(this, new ExceptionInfo(sourceElement), ErrorMessages.UnrecognisedTypeError, typeName,
                multiTypeDefinition.AttributeForType.LocalName, multiTypeDefinition.ElementName!.LocalName);
        }

        // SECURITY: gate on the allow-list (TypeToAttributesMap) BEFORE constructing the type, so an
        // un-mapped (but namespace-pinned, ctor-matching) type from untrusted ATDL cannot trigger its
        // constructor / type-initializer side effects before being rejected.
        if (!multiTypeDefinition.TypeToAttributesMap.TryGetValue(targetType, out ElementAttribute[]? typeAttributes))
        {
            throw ThrowHelper.New<InvalidFieldValueException>(this, new ExceptionInfo(sourceElement), ErrorMessages.UnrecognisedTypeError, typeName,
                multiTypeDefinition.AttributeForType.LocalName, multiTypeDefinition.ElementName!.LocalName);
        }

        object newObject = CreateRawObject(targetType, constructorParameterTypes, constructorParameterValues);

        IEnumerable<XAttribute> attributes = sourceElement.Attributes();

        try
        {
            ProcessAttributes(newObject.GetType(), multiTypeDefinition.Attributes!, attributes, newObject);
            ProcessAttributes(newObject.GetType(), typeAttributes, attributes, newObject);
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

    private object CreateRawObject(Type outerType, Type[] innerTypes, Type[] argTypes, params object[] args)
    {
        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("CreateObject(Type, Type[], Type[], params object[]) called (creating generic type); Outer type={OuterType}.", outerType.FullName);
        }

        Type specificType = outerType.MakeGenericType(innerTypes);

        ConstructorInfo? classConstructor = specificType.GetConstructor(argTypes);

        if (classConstructor == null)
        {
            throw ThrowHelper.New<InternalErrorException>(ExceptionContext, InternalErrors.NoConstructorFoundForSpecifiedArgumentTypes, outerType.FullName!);
        }

        return classConstructor.Invoke(args);
    }

    private object CreateRawObject(Type targetType, Type[] argTypes, params object[] args)
    {
        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("CreateObject(Type, Type[], params object[]) called; Type={TargetType}.", targetType.FullName);
        }

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
        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("GetConstructorParameters called; ElementName='{ElementName}'.", elementDefinition.ElementName);
        }

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
                            else
                            {
                                // A cache miss would otherwise leave a null constructor argument that is
                                // passed positionally into the ctor (NRE / half-initialised object with no
                                // schema context). Surface a located error instead — typically triggered by
                                // malformed or out-of-order XML.
                                throw ThrowHelper.New<FixAtdlException>(this, new ExceptionInfo(sourceElement),
                                    $"The named predecessor '{elementDefinition.ConstructorParameters[n].Source}' required by element '{elementDefinition.ElementName}' was not found; the source XML may be malformed or its elements out of order.");
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
        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("ProcessAttributes called; Target type={TargetType}.", targetType.FullName);
        }

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
                SetIndirectPropertyValue(targetType, attrDefn, target, value);
            }
            else
            {
                SetDirectPropertyValue(targetType, attrDefn, target, value);
            }
        }
    }

    private void SetIndirectPropertyValue(Type targetType, ElementAttribute attrDefn, object target, object value)
    {
        string[] names = attrDefn.Property.Split('.');

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

    private void SetDirectPropertyValue(Type targetType, ElementAttribute attrDefn, object target, object value)
    {
        PropertyInfo property = targetType.GetProperty(attrDefn.Property)!;

        if (property == null)
        {
            throw ThrowHelper.New<InvalidPropertyOnObjectException>(this, ErrorMessages.PropertyNotFoundOnObject, attrDefn.Property, targetType.Name);
        }

        SetPropertyValue(property, target, value);
    }

    private void ProcessChildren(ElementDefinition definition, XElement sourceElement, object target)
    {
        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("ProcessChildren called; ElementName='{ElementName}'", definition.ElementName);
        }

        // We have to reflect the target type as we can't rely on the Definition to contain it (e.g. MultiTypeElementDefinition).
        Type targetType = target.GetType();


        foreach (ChildElementDefinition childDefinition in definition.ChildElements!)
        {
            bool isRecursiveDefinition = childDefinition.ElementDefinition is RecursiveTypeElementDefinition;
            bool hasContainerElement = !isRecursiveDefinition && childDefinition.ContainerElementName != null;
            ElementDefinition targetDefinition = isRecursiveDefinition ? definition : childDefinition.ElementDefinition;

            foreach (XElement childElement in GetMatchingChildElements(childDefinition, targetDefinition, hasContainerElement, sourceElement))
            {
                ProcessMatchingChild(definition, childDefinition, targetDefinition, targetType, target, childElement);
            }
        }
    }

    private static IEnumerable<XElement> GetMatchingChildElements(ChildElementDefinition childDefinition, ElementDefinition targetDefinition,
        bool hasContainerElement, XElement sourceElement)
    {
        if (hasContainerElement)
        {
            XElement? containerElement = (from e in sourceElement.Elements(childDefinition.ContainerElementName) select e).FirstOrDefault();

            if (containerElement == null)
            {
                return [];
            }

            return from e in containerElement.Elements(childDefinition.ElementDefinition.ElementName) select e;
        }

        return from e in sourceElement.Elements(targetDefinition.ElementName) select e;
    }

    private void ProcessMatchingChild(ElementDefinition definition, ChildElementDefinition childDefinition, ElementDefinition targetDefinition,
        Type targetType, object target, XElement childElement)
    {
        object childObject = targetDefinition switch
        {
            GenericTypeElementDefinition genericDefinition => CreateObject(genericDefinition, childElement, target),
            MultiTypeElementDefinition multiDefinition => CreateObject(multiDefinition, childElement, target),
            _ => CreateObject(targetDefinition, childElement, target)
        };

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

    private void ProcessChildProperty(ChildElementDefinition childDefinition, PropertyInfo property, Type targetType, object target, object childObject)
    {
        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("ProcessChildProperty called; ElementName='{ElementName}', Property={Property}.", childDefinition.ElementDefinition.ElementName, property.Name);
        }

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
        else if (childDefinition.ContainerMethod is string methodName)
        {
            containerMethod = methodName;
        }
        else
        {
            // ContainerMethod is object-typed; guard the non-string/non-StandardContainerMethod case
            // so a misconfigured definition surfaces a clear error rather than GetMethod(null) throwing
            // a context-less ArgumentNullException.
            throw ThrowHelper.New<InternalErrorException>(ExceptionContext,
                $"ContainerMethod for element '{childDefinition.ElementDefinition.ElementName}' is neither a StandardContainerMethod nor a string.");
        }

        MethodInfo targetMethod = property.PropertyType.GetMethod(containerMethod, [targetType])!;

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

    private object ReadAttribute(IEnumerable<XAttribute> attributes, XName attributeName, Type type)
    {
        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("ReadAttribute(IEnumerable<XAttribute>, XName, Type) called; Attribute name='{AttributeName}'", attributeName);
        }

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
            catch (Exception ex) when (ex is FormatException or OverflowException)
            {
                throw ThrowHelper.New<InvalidFieldValueException>(ExceptionContext, ex, ErrorMessages.DataConversionError2,
                    attribute.Value, type.Name, attributeName.LocalName);
            }
        }
    }

    private object ReadAttribute(IEnumerable<XAttribute> attributes, XName attributeName, Type enumType, Dictionary<string, Enum> enumValues)
    {
        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("ReadAttribute(IEnumerable<XAttribute>, XName, Type, Dictionary<string, Enum>) called; Attribute name='{AttributeName}'", attributeName);
        }

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

    private void SetPropertyValue(PropertyInfo property, object target, object value)
    {
        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("SetPropertyValue called; Target object type={TargetType}, property={Property}, value='{Value}'.", target.GetType().FullName, property.Name, value);
        }

        try
        {
            // Assign directly when the value is already an instance of the property type (exact or a
            // derived/assignable type); only fall back to the single-arg converting-ctor path when a
            // genuine conversion is required. The previous exact-type-only check pushed assignable
            // values through CreateRawObject, which fails for any property type lacking such a ctor.
            if (property.PropertyType.IsInstanceOfType(value))
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

    /// <summary>
    /// Occurs when an object of the configured notification type has been deserialized.
    /// </summary>
    public event EventHandler<ClassDeserializedEventArgs>? ClassDeserialized;

    #endregion
}
