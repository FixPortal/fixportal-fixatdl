// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

namespace FixPortal.FixAtdl.Diagnostics.Exceptions;

/// <summary>Represents an internal error, i.e., one that should not occur during normal operation of Atdl4net which may indicate a bug.</summary>
public class InternalErrorException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="InternalErrorException"/>.
    /// </summary>
    /// <param name="message">The message.</param>
    public InternalErrorException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="InternalErrorException"/>.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The inner exception.</param>
    public InternalErrorException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

