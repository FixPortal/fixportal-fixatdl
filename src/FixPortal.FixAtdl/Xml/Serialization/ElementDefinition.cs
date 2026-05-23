#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;
using System.Xml.Linq;

namespace Atdl4net.Xml.Serialization
{
    public class ElementDefinition
    {
        public XName? ElementName { get; set; } // FP Enhancement: 2026-05-23 — nullable cleanup deferred to Phase C
        public Type? TargetType { get; private set; } // FP Enhancement: 2026-05-23 — nullable cleanup deferred to Phase C
        public ElementAttribute[]? Attributes { get; private set; } // FP Enhancement: 2026-05-23 — nullable cleanup deferred to Phase C
        public ConstructorParameter[]? ConstructorParameters { get; private set; } // FP Enhancement: 2026-05-23 — nullable cleanup deferred to Phase C
        public ChildElementDefinition[]? ChildElements { get; private set; } // FP Enhancement: 2026-05-23 — nullable cleanup deferred to Phase C
        public CacheElementValueInstruction? CacheElementValueInstruction { get; private set; } // FP Enhancement: 2026-05-23 — nullable cleanup deferred to Phase C

        public ElementDefinition(XName? elementName, Type? targetType, ElementAttribute[]? attributes)
            : this(elementName, targetType, null, attributes, new ChildElementDefinition[] { }, null)
        {
        }

        public ElementDefinition(XName elementName, Type targetType, ElementAttribute[] attributes, ChildElementDefinition child)
            : this(elementName, targetType, null, attributes, new ChildElementDefinition[] { child }, null)
        {
        }

        public ElementDefinition(XName elementName, Type targetType, ElementAttribute[] attributes, ChildElementDefinition[] children)
            : this(elementName, targetType, null, attributes, children, null)
        {
        }

        public ElementDefinition(XName elementName, Type targetType, ElementAttribute[] attributes, ChildElementDefinition[] children,
            CacheElementValueInstruction cacheInstruction)
            : this(elementName, targetType, null, attributes, children, cacheInstruction)
        {
        }

        public ElementDefinition(XName elementName, Type targetType, ConstructorParameter[] constructorParameters, ElementAttribute[] attributes)
            : this(elementName, targetType, constructorParameters, attributes, new ChildElementDefinition[] { }, null)
        {
        }

        public ElementDefinition(XName elementName, Type targetType, ConstructorParameter[] constructorParameters, ElementAttribute[] attributes, 
            ChildElementDefinition child)
            : this(elementName, targetType, constructorParameters, attributes, new ChildElementDefinition[] { child }, null)
        {
        }

        public ElementDefinition(XName elementName, Type targetType, ConstructorParameter[] constructorParameters,
            ElementAttribute[] attributes, ChildElementDefinition[] children)
            : this(elementName, targetType, constructorParameters, attributes, children, null)
        {
        }

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
}