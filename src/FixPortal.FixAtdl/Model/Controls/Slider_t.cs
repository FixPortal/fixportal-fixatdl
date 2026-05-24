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
/// Represents the Slider_t control element within FIXatdl.
/// </summary>
/// <remarks>The FIXatdl 1.1 specification is a little unclear on what a Slider_t can do.  The current Atdl4net implementation supports
/// selecting from a set of options (ListItems) but not selecting a numerical value.</remarks>
public class Slider_t : ListControlBase
{
    // FP Enhancement: 2026-05-23 — TODO wire injected logger when refactoring class to accept ILogger.
    private static readonly ILogger _log = NullLogger.Instance;

    /// <summary>
    /// Initializes a new instance of <see cref="Slider_t"/> using the supplied ID.
    /// </summary>
    /// <param name="id">ID for this control.</param>
    public Slider_t(string id)
        : base(id)
    {
        _log.LogDebug("New Slider_t created as control {Arg0}", id);
    }
}

