// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;
using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FixPortal.FixAtdl.Diagnostics;

/// <summary>
/// Static helper class for generating new instances of exceptions using the provided parameters
/// </summary>
public static class ThrowHelper
{
    // FP Enhancement: 2026-05-23 — TODO wire injected logger when refactoring class to accept ILogger.
    private static readonly ILogger _log = NullLogger.Instance;

    /// <summary>
    /// Creates an exception of the specified type and initializes it using the values supplied.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">The source.</param>
    /// <param name="message">The message.</param>
    /// <returns>A new exception of the specified type.</returns>
    public static T New<T>(object? source, string message) where T : System.Exception
    {
        T ex = CreateException<T>(source, message, null);

        _log.LogError(ex, "Exception created by ThrowHelper");

        return ex;
    }

    /// <summary>
    /// Creates an exception of the specified type and initializes it using the values supplied.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">The source.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="message">The message.</param>
    /// <returns>A new exception of the specified type.</returns>
    public static T New<T>(object? source, Exception innerException, string message) where T : System.Exception
    {
        T ex = CreateException<T>(source, message, innerException, null);

        _log.LogError(ex, "Exception created by ThrowHelper");

        return ex;
    }

    /// <summary>
    /// Creates an exception of the specified type and initializes it using the values supplied.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">The source.</param>
    /// <param name="format">The format.</param>
    /// <param name="args">The args.</param>
    /// <returns>A new exception of the specified type.</returns>
    // FP Enhancement: 2026-05-23 — params object[] args -> params object?[] args to support nullable callers.
    public static T New<T>(object? source, string format, params object?[] args) where T : System.Exception
    {
        T ex = CreateException<T>(source, string.Format(CultureInfo.InvariantCulture, format, args!), null);

        _log.LogError(ex, "Exception created by ThrowHelper");

        return ex;
    }

    /// <summary>
    /// Creates an exception of the specified type and initializes it using the values supplied.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">The source.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="format">The format.</param>
    /// <param name="args">The args.</param>
    /// <returns>A new exception of the specified type.</returns>
    public static T New<T>(object? source, Exception innerException, string format, params object?[] args) where T : System.Exception
    {
        T ex = CreateException<T>(source, string.Format(CultureInfo.InvariantCulture, format, args!), innerException, null);

        _log.LogError(ex, "Exception created by ThrowHelper");

        return ex;
    }

    /// <summary>
    /// Creates an exception of the specified type and initializes it using the values supplied.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">The source.</param>
    /// <param name="info">The info.</param>
    /// <param name="message">The message.</param>
    /// <returns>A new exception of the specified type.</returns>
    public static T New<T>(object? source, ExceptionInfo? info, string message) where T : System.Exception
    {
        T ex = CreateException<T>(source, message, info);

        _log.LogError(ex, "Exception created by ThrowHelper");

        return ex;
    }

    /// <summary>
    /// Creates an exception of the specified type and initializes it using the values supplied.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">The source.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="info">The info.</param>
    /// <param name="message">The message.</param>
    /// <returns>A new exception of the specified type.</returns>
    public static T New<T>(object? source, Exception innerException, ExceptionInfo? info, string message) where T : System.Exception
    {
        T ex = CreateException<T>(source, message, innerException, info);

        _log.LogError(ex, "Exception created by ThrowHelper");

        return ex;
    }

    /// <summary>
    /// Creates an exception of the specified type and initializes it using the values supplied.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">The source.</param>
    /// <param name="info">The info.</param>
    /// <param name="format">The format.</param>
    /// <param name="args">The args.</param>
    /// <returns>A new exception of the specified type.</returns>
    public static T New<T>(object? source, ExceptionInfo? info, string format, params object?[] args) where T : System.Exception
    {
        T ex = CreateException<T>(source, string.Format(CultureInfo.InvariantCulture, format, args!), info);

        _log.LogError(ex, "Exception created by ThrowHelper");

        return ex;
    }

    /// <summary>
    /// Creates an exception of the specified type and initializes it using the values supplied.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">The source.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="info">The info.</param>
    /// <param name="format">The format.</param>
    /// <param name="args">The args.</param>
    /// <returns>A new exception of the specified type.</returns>
    public static T New<T>(object? source, Exception innerException, ExceptionInfo? info, string format, params object?[] args) where T : System.Exception
    {
        T ex = CreateException<T>(source, string.Format(CultureInfo.InvariantCulture, format, args!), innerException, info);

        _log.LogError(ex, "Exception created by ThrowHelper");

        return ex;
    }

    /// <summary>
    /// Wraps the supplied exception in a new exception of the same type as that supplied, in order to get a
    /// decent error message back to the end-user.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="ex">The ex.</param>
    /// <param name="format">The format.</param>
    /// <param name="args">An array of zero or more arguments.</param>
    /// <returns>A new exception of the same type as the supplied exception.</returns>
    public static Exception Rethrow(object? source, Exception ex, string format, params object[] args)
    {
        Exception newException = Rethrow(source, ex, string.Format(CultureInfo.InvariantCulture, format, args), new object());

        _log.LogError(newException, "Exception rethrown by ThrowHelper");

        return newException;
    }

    /// <summary>
    /// Wraps the supplied exception in a new exception of the same type as that supplied, in order to get a
    /// decent error message back to the end-user.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="ex">The ex.</param>
    /// <param name="format">The format.</param>
    /// <param name="arg">The arg.</param>
    /// <returns>A new exception of the same type as the supplied exception.</returns>
    public static Exception Rethrow(object? source, Exception ex, string format, object arg)
    {
        Exception newException = Rethrow(source, ex, null, format, arg);

        _log.LogError(newException, "Exception rethrown by ThrowHelper");

        return newException;
    }

    /// <summary>
    /// Wraps the supplied exception in a new exception of the same type as that supplied, in order to get a
    /// decent error message back to the end-user.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="ex">The ex.</param>
    /// <param name="info">The info.</param>
    /// <param name="format">The format.</param>
    /// <param name="arg">The arg.</param>
    /// <returns>A new exception of the same type as the supplied exception.</returns>
    public static Exception Rethrow(object? source, Exception ex, ExceptionInfo? info, string format, object arg)
    {
        Type classType = ex.GetType();

        ConstructorInfo classConstructor = classType.GetConstructor([typeof(string), typeof(Exception)])!;

        Exception exception = (Exception)classConstructor.Invoke([string.Format(CultureInfo.InvariantCulture, format, arg, ex.Message), ex]);

        exception.Source = source?.ToString();

        info?.PopulateExceptionData(exception.Data);

        return exception;
    }

    // Workaround limitation in C# 3.0/4.0 - can't create an instance of a generic type with parameters using new T().
    private static T CreateException<T>(object? source, string message, ExceptionInfo? info) where T : System.Exception
    {
        Type classType = typeof(T);

        switch (classType.Name)
        {
            // Special treatment is needed for ArgumentOutOfRangeException and ArgumentNullException because the constructor that takes
            // a single string for these types makes its own message.
            case "ArgumentOutOfRangeException":
            case "ArgumentNullException":
                {
                    ConstructorInfo classConstructor = classType.GetConstructor([typeof(string), typeof(string)])!;
                    T exception = (T)classConstructor.Invoke(["Value", message]);
                    exception.Source = source?.ToString();
                    info?.PopulateExceptionData(exception.Data);

                    return exception;
                }

            default:
                {
                    ConstructorInfo classConstructor = classType.GetConstructor([typeof(string)])!;
                    T exception = (T)classConstructor.Invoke([message]);
                    exception.Source = source?.ToString();
                    info?.PopulateExceptionData(exception.Data);

                    return exception;
                }
        }
    }

    // Workaround limitation in C# 3.0/4.0 - can't create an instance of a generic type with parameters using new T().
    private static T CreateException<T>(object? source, string message, Exception innerException, ExceptionInfo? info) where T : System.Exception
    {
        Type classType = typeof(T);

        ConstructorInfo classConstructor = classType.GetConstructor([typeof(string), typeof(Exception)])!;

        T exception = (T)classConstructor.Invoke([message, innerException]);

        exception.Source = source?.ToString();

        info?.PopulateExceptionData(exception.Data);

        return exception;
    }
}
