#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;
using FixPortal.FixAtdl.Model.Controls.Support;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FixPortal.FixAtdl.Model.Controls;

/// <summary>
/// Represents the TextField_t control element within FIXatdl.
/// </summary>
public class TextField_t : TextControlBase
{
    // FP Enhancement: 2026-05-23 — TODO wire injected logger when refactoring class to accept ILogger.
    private static readonly ILogger _log = NullLogger.Instance;

    /// <summary>
    /// Initializes a new instance of <see cref="TextField_t"/> using the supplied ID.
    /// </summary>
    /// <param name="id">ID for this control.</param>
    public TextField_t(string id)
        : base(id)
    {
        _log.LogDebug("New TextField_t created as control {Arg0}", id);
    }
}

