// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

namespace FixPortal.FixAtdl.Diagnostics.Exceptions;

/// <summary>
/// Thrown if an error occurs when trying to render a strategy.  The InnerException should provide more insight into the
/// underlying cause, if the Message value provides insufficient detail.
/// </summary>
[Serializable]
public class RenderingException : FixAtdlException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RenderingException"/> class.
    /// </summary>
    /// <param name="message"></param>
    public RenderingException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderingException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The inner exception.</param>
    public RenderingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

}

