// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

namespace FixPortal.FixAtdl.Diagnostics.Exceptions;

/// <summary>
/// The exception that is thrown when deserializing a FIXatdl file or stream, indicating that a property has been supplied for a given object,
/// but that object does not support that property.
/// </summary>
public class InvalidPropertyOnObjectException : FixAtdlException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidPropertyOnObjectException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    public InvalidPropertyOnObjectException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidPropertyOnObjectException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The inner exception.</param>
    public InvalidPropertyOnObjectException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

