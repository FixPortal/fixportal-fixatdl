#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;

namespace Atdl4net.Diagnostics.Exceptions
{
    /// <summary>
    /// The exception that is thrown when a value fails validation, either through a constraint on a parameter or throw a
    /// StrategyEdit validation rule.
    /// </summary>
    // FP Enhancement: 2026-05-23 — removed obsolete SerializationInfo constructor (SYSLIB0051 — binary serialization removed in .NET 10).
    public class ValidationException : Atdl4netException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class.
        /// </summary>
        /// <param name="message"></param>
        public ValidationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

    }
}
