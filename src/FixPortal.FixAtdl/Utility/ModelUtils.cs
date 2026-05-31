// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace FixPortal.FixAtdl.Utility;

/// <summary>
/// Provides reflection-based helpers for working with the FIXatdl model.
/// </summary>
public static class ModelUtils
{
    private static readonly Type[] _types;
    private static readonly Dictionary<string, MethodInfo> _methodInfoCache = [];

    static ModelUtils()
    {
        Type[] allTypes;

        try
        {
            allTypes = Assembly.GetExecutingAssembly().GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // One unloadable type would otherwise bubble a TypeInitializationException out of this
            // static constructor and brick every consumer of ModelUtils for the process lifetime.
            allTypes = [.. ex.Types.Where(t => t != null).Cast<Type>()];
        }

        // Materialise once so GetTypeFromName does not re-run the LINQ predicate on every call.
        _types = [.. allTypes.Where(t => t.IsClass && t.Namespace == "FixPortal.FixAtdl.Model.Types" && !t.IsAbstract)];
    }

    /// <summary>
    /// Invokes a matching <c>Visit</c> overload on the supplied visitor for the target object.
    /// </summary>
    /// <param name="visitorType">The declared visitor type used as part of the cache key.</param>
    /// <param name="visitor">The visitor instance.</param>
    /// <param name="target">The target object to visit.</param>
    /// <returns><see langword="true"/> if a matching <c>Visit</c> method was found and invoked; otherwise, <see langword="false"/>.</returns>
    public static bool VisitHelper(Type visitorType, object visitor, object target)
    {
        Type targetParamType = target.GetType();

        // Include the CONCRETE visitor type in the key, not just the declared visitorType: two
        // implementations of the same visitor interface would otherwise collide on one cache entry and
        // the second would invoke the first's MethodInfo, throwing a TargetException (F3).
        string searchString = string.Format(CultureInfo.InvariantCulture, "{0}:{1}:{2}", visitorType.FullName, visitor.GetType().FullName, targetParamType.FullName);

        MethodInfo? methodInfo;

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

                // Indexer rather than Add: idempotent if the same key is computed twice.
                _methodInfoCache[searchString] = methodInfo;
            }
        }

        try
        {
            methodInfo.Invoke(visitor, [target]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            // Surface the visitor's own exception (preserving its stack) rather than wrapping it in a
            // TargetInvocationException from the reflective Invoke (G-C).
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }

        return true;
    }

    // GetTypeFromName lives here by design: ModelUtils owns the _types cache it reads.
    /// <summary>
    /// Gets a FIXatdl model type by its CLR type name.
    /// </summary>
    /// <param name="typeName">The CLR type name to look up.</param>
    /// <returns>The matching type, or <see langword="null"/> if no type matches.</returns>
    public static Type? GetTypeFromName(string typeName)
    {
        return _types.FirstOrDefault(t => t.Name == typeName);
    }
}
