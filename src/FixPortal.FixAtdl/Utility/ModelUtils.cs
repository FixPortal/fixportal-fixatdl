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

namespace FixPortal.FixAtdl.Utility;

public static class ModelUtils
{
    private static readonly IEnumerable<Type> _types;
    private static readonly Dictionary<string, MethodInfo> _methodInfoCache = [];

    static ModelUtils()
    {
        _types = from t in Assembly.GetExecutingAssembly().GetTypes()
                 where t.IsClass && t.Namespace == "FixPortal.FixAtdl.Model.Types" && !t.IsAbstract
                 select t;
    }

    public static bool VisitHelper(Type visitorType, object visitor, object target)
    {
        Type targetParamType = target.GetType();
        string searchString = string.Format(CultureInfo.InvariantCulture, "{0}:{1}", visitorType.FullName, targetParamType.FullName);

        MethodInfo? methodInfo = null;

        lock (_methodInfoCache)
        {
            if (!_methodInfoCache.TryGetValue(searchString, out methodInfo))
            {
                Type[] types = [targetParamType];

                methodInfo = visitor.GetType().GetMethod("Visit", types);

                if (methodInfo == null)
                {
                    return false;
                }

                _methodInfoCache.Add(searchString, methodInfo);
            }
        }

        methodInfo.Invoke(visitor, [target]);

        return true;
    }

    // TODO: Move this somewhere better.
    public static System.Type? GetTypeFromName(string typeName)
    {
        return _types.FirstOrDefault(t => t.Name == typeName);
    }
}
