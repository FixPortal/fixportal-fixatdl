// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

namespace FixPortal.FixAtdl.Diagnostics.Exceptions;

/// <summary>
/// The exception that is thrown when a FIX message cannot be parsed.
/// </summary>
public class FixParseException : FixAtdlException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FixParseException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception. </param>
    public FixParseException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FixParseException"/> class with a specified error message and a
    /// reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception. </param>
    /// <param name="innerException">The exception that is the cause of the current exception. If the innerException 
    /// parameter is not a null reference (Nothing in Visual Basic), the current exception is raised in a catch block 
    /// that handles the inner exception. </param>
    public FixParseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

}

