// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Reflection;
using System.Runtime.ExceptionServices;

namespace FixPortal.FixAtdl.Utility;

/// <summary>
/// Provides reflection-based helpers for working with the FIXatdl model.
/// </summary>
public static class ModelUtils
{
    private static readonly Dictionary<
        (Type DeclaredVisitorType, Type ConcreteVisitorType, Type TargetType),
        MethodInfo?
    > _methodInfoCache = [];

    /// <summary>
    /// Invokes a matching <c>Visit</c> overload on the supplied visitor for the target object.
    /// </summary>
    /// <param name="visitorType">The declared visitor type used as part of the cache key.</param>
    /// <param name="visitor">The visitor instance.</param>
    /// <param name="target">The target object to visit.</param>
    /// <returns><see langword="true"/> if a matching <c>Visit</c> method was found and invoked; otherwise, <see langword="false"/>.</returns>
    public static bool VisitHelper(Type visitorType, object visitor, object target)
    {
        Type concreteVisitorType = visitor.GetType();
        Type targetParamType = target.GetType();

        // Key the cache by Type identity, not Type.FullName strings. Both key parts matter:
        // the CONCRETE visitor type alongside the declared visitorType, because two implementations
        // of the same visitor interface would otherwise share one entry (F3); and Type identity
        // rather than FullName, because same-named types from different assemblies would still
        // collide on a string key (F1/X). Either collision makes a later call invoke another type's
        // MethodInfo, throwing a TargetException.
        var cacheKey = (visitorType, concreteVisitorType, targetParamType);

        MethodInfo? methodInfo;

        lock (_methodInfoCache)
        {
            if (!_methodInfoCache.TryGetValue(cacheKey, out methodInfo))
            {
                Type[] types = [targetParamType];

                methodInfo = concreteVisitorType.GetMethod("Visit", types);

                // Cache the miss too (as null), so a permanently-absent Visit overload is only
                // reflected-over once instead of re-scanning under the lock on every call.
                _methodInfoCache[cacheKey] = methodInfo;
            }

            if (methodInfo == null)
            {
                return false;
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
}
