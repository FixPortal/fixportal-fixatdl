// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

namespace FixPortal.FixAtdl.Diagnostics.Exceptions;

/// <summary>Provides a base exception class for all FixPortal.FixAtdl custom exceptions.</summary>
// FP Enhancement: 2026-05-23 — removed obsolete SerializationInfo constructor (SYSLIB0051 — binary serialization removed in .NET 10).
public class FixAtdlException : Exception
{
    /// <summary>Initializes a new instance of the FixAtdlException class with a specified error message.</summary>
    public FixAtdlException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the FixAtdlException class with a specified error message and a reference to the inner exception that is the
    /// cause of this exception.</summary>
    public FixAtdlException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

