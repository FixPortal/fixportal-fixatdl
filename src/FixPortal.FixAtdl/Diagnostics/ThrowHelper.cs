// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Globalization;
using System.Reflection;
using FixPortal.FixAtdl.Diagnostics.Exceptions;

namespace FixPortal.FixAtdl.Diagnostics;

/// <summary>
/// Static helper class for generating new instances of exceptions using the provided parameters
/// </summary>
public static class ThrowHelper
{
    // Format the message only when arguments are supplied. With zero args, string.Format would still
    // parse the format string for placeholders and throw FormatException on literal braces (e.g.
    // "{NULL}", "Nullable{Int32}", XML payloads), corrupting the error-reporting path (F1c).
    private static string FormatMessage(string format, object?[] args)
        => args.Length > 0 ? string.Format(CultureInfo.InvariantCulture, format, args) : format;

    /// <summary>
    /// Creates an exception of the specified type and initializes it using the values supplied.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">The source.</param>
    /// <param name="message">The message.</param>
    /// <returns>A new exception of the specified type.</returns>
    public static T New<T>(object? source, string message) where T : Exception
    {
        T ex = CreateException<T>(source, message, null);

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
    public static T New<T>(object? source, Exception innerException, string message) where T : Exception
    {
        T ex = CreateException<T>(source, message, innerException, null);

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
    public static T New<T>(object? source, string format, params object?[] args) where T : Exception
    {
        T ex = CreateException<T>(source, FormatMessage(format, args), null);

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
    public static T New<T>(object? source, Exception innerException, string format, params object?[] args) where T : Exception
    {
        T ex = CreateException<T>(source, FormatMessage(format, args), innerException, null);

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
    public static T New<T>(object? source, ExceptionInfo? info, string message) where T : Exception
    {
        T ex = CreateException<T>(source, message, info);

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
    public static T New<T>(object? source, Exception innerException, ExceptionInfo? info, string message) where T : Exception
    {
        T ex = CreateException<T>(source, message, innerException, info);

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
    public static T New<T>(object? source, ExceptionInfo? info, string format, params object?[] args) where T : Exception
    {
        T ex = CreateException<T>(source, FormatMessage(format, args), info);

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
    public static T New<T>(object? source, Exception innerException, ExceptionInfo? info, string format, params object?[] args) where T : Exception
    {
        T ex = CreateException<T>(source, FormatMessage(format, args), innerException, info);

        return ex;
    }

    /// <summary>
    /// Creates an exception of type <typeparamref name="T"/> carrying an explicit parameter name, for the
    /// ArgumentException family whose (string, string) constructor is (paramName, message). Use this
    /// instead of <see cref="New{T}(object?, string)"/> when the offending parameter name is known, so the
    /// exception reports it rather than the synthetic default "Value".
    /// </summary>
    public static T NewWithParamName<T>(object? source, string paramName, string message) where T : Exception
    {
        return CreateException<T>(source, message, null, paramName);
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
        // Format ONCE. Callers of this params overload supply every argument the template needs
        // (including ex.Message where it is referenced). Routing the formatted result through another
        // formatting overload — as the previous implementation did — string.Formats it a SECOND time,
        // which throws FormatException on literal braces ({NULL}, XML) or silently substitutes a stray
        // {0}/{1} (F1a / F1b). The no-args case skips formatting so a brace-bearing literal is safe (F1c).
        string message = FormatMessage(format, args);

        Exception newException = BuildRethrown(source, ex, null, message);

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
        // Single-arg convention: the template references the argument as {0} and ex.Message as {1}.
        // Formatted exactly once here; BuildRethrown does no further formatting.
        string message = string.Format(CultureInfo.InvariantCulture, format, arg, ex.Message);

        Exception newException = BuildRethrown(source, ex, info, message);

        return newException;
    }

    // Builds a replacement exception of the same runtime type as 'ex' from an ALREADY-FORMATTED
    // message (no further string.Format). If that type has no (string, Exception) constructor the
    // original exception is preserved rather than throwing a NullReferenceException off GetConstructor (F2).
    private static Exception BuildRethrown(object? source, Exception ex, ExceptionInfo? info, string message)
    {
        ConstructorInfo? classConstructor = ex.GetType().GetConstructor([typeof(string), typeof(Exception)]);

        if (classConstructor == null)
        {
            return ex;
        }

        Exception exception = (Exception)classConstructor.Invoke([message, ex]);

        exception.Source = source?.ToString();

        info?.PopulateExceptionData(exception.Data);

        return exception;
    }

    // Workaround limitation in C# 3.0/4.0 - can't create an instance of a generic type with parameters using new T().
    private static T CreateException<T>(object? source, string message, ExceptionInfo? info, string paramName = "Value") where T : Exception
    {
        Type classType = typeof(T);

        switch (classType.Name)
        {
            // Special treatment is needed for ArgumentOutOfRangeException and ArgumentNullException because the constructor that takes
            // a single string for these types makes its own message.
            case "ArgumentOutOfRangeException":
            case "ArgumentNullException":
                {
                    ConstructorInfo classConstructor = classType.GetConstructor([typeof(string), typeof(string)])
                        ?? throw new InternalErrorException($"Exception type '{classType.FullName}' has no (string, string) constructor required by ThrowHelper. Message: {message}");
                    T exception = (T)classConstructor.Invoke([paramName, message]);
                    exception.Source = source?.ToString();
                    info?.PopulateExceptionData(exception.Data);

                    return exception;
                }

            default:
                {
                    ConstructorInfo classConstructor = classType.GetConstructor([typeof(string)])
                        ?? throw new InternalErrorException($"Exception type '{classType.FullName}' has no (string) constructor required by ThrowHelper. Message: {message}");
                    T exception = (T)classConstructor.Invoke([message]);
                    exception.Source = source?.ToString();
                    info?.PopulateExceptionData(exception.Data);

                    return exception;
                }
        }
    }

    // Workaround limitation in C# 3.0/4.0 - can't create an instance of a generic type with parameters using new T().
    private static T CreateException<T>(object? source, string message, Exception innerException, ExceptionInfo? info) where T : Exception
    {
        Type classType = typeof(T);

        ConstructorInfo classConstructor = classType.GetConstructor([typeof(string), typeof(Exception)])
            ?? throw new InternalErrorException($"Exception type '{classType.FullName}' has no (string, Exception) constructor required by ThrowHelper. Message: {message}");

        T exception = (T)classConstructor.Invoke([message, innerException]);

        exception.Source = source?.ToString();

        info?.PopulateExceptionData(exception.Data);

        return exception;
    }
}
