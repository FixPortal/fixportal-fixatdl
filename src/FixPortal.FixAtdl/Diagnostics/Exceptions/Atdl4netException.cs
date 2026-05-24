#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;

namespace FixPortal.FixAtdl.Diagnostics.Exceptions;

/// <summary>Provides a base exception class for all Atdl4net custom exceptions.</summary>
// FP Enhancement: 2026-05-23 — removed obsolete SerializationInfo constructor (SYSLIB0051 — binary serialization removed in .NET 10).
public class Atdl4netException : System.Exception
{
    /// <summary>Initializes a new instance of the Atdl4netException class with a specified error message.</summary>
    public Atdl4netException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the Atdl4netException class with a specified error message and a reference to the inner exception that is the
    /// cause of this exception.</summary>
    public Atdl4netException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

