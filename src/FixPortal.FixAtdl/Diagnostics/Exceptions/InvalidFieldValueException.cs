// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

namespace FixPortal.FixAtdl.Diagnostics.Exceptions;

/// <summary>
/// The exception that is thrown when attempting to set a field to an invalid value.
/// </summary>
[Serializable]
public class InvalidFieldValueException : FixAtdlException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidFieldValueException"/> class.
    /// </summary>
    /// <param name="message"></param>
    public InvalidFieldValueException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidFieldValueException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The inner exception.</param>
    public InvalidFieldValueException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

}

