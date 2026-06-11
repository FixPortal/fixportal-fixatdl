// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

namespace FixPortal.FixAtdl.Diagnostics.Exceptions;

/// <summary>
/// The exception that is thrown when a value is not supplied but is required, either by the FIXatdl schema, or by setting the
/// 'use' attribute to 'required'.
/// </summary>
public class MissingMandatoryValueException : FixAtdlException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingMandatoryValueException"/> class.
    /// </summary>
    /// <param name="message"></param>
    public MissingMandatoryValueException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MissingMandatoryValueException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The inner exception.</param>
    public MissingMandatoryValueException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

