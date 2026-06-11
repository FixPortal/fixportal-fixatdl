// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

namespace FixPortal.FixAtdl.Diagnostics.Exceptions;

/// <summary>
/// The exception that is thrown when trying to process a strategy that is internally inconsistent, such as having ListItems but no EnumPairs.
/// </summary>
public class InconsistentStrategyException : FixAtdlException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InconsistentStrategyException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception. </param>
    public InconsistentStrategyException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InconsistentStrategyException"/> class with a specified error message and a
    /// reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception. </param>
    /// <param name="innerException">The exception that is the cause of the current exception. If the innerException 
    /// parameter is not a null reference (Nothing in Visual Basic), the current exception is raised in a catch block 
    /// that handles the inner exception. </param>
    public InconsistentStrategyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

}

