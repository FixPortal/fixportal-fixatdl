// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Model.Controls.Support;
using FixPortal.FixAtdl.Model.Enumerations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FixPortal.FixAtdl.Model.Controls;

/// <summary>
/// Represents the SingleSpinner_t control element within FIXatdl.
/// </summary>
public class SingleSpinner_t : NumericControlBase
{
    // FP Enhancement: 2026-05-23 — TODO wire injected logger when refactoring class to accept ILogger.
    private static readonly NullLogger _log = NullLogger.Instance;

    /// <summary>
    /// Initializes a new instance of <see cref="SingleSpinner_t"/> using the supplied ID.
    /// </summary>
    /// <param name="id">ID for this control.</param>
    public SingleSpinner_t(string id)
        : base(id)
    {
        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("New SingleSpinner_t created as control {Arg0}", id);
        }
    }

    /// <summary>Limits the granularity of a spinner control. Useful in spinner objects to enforce odd-lot and sub-penny
    ///  restrictions.  Applicable when xsi:type is SingleSpinner_t or Slider_t.</summary>
    public decimal? Increment { get; set; }

    /// <summary>For single spinner control, defines how to determine the increment. Applicable when xsi:type is SingleSpinner_t.</summary>
    public IncrementPolicy_t? IncrementPolicy { get; set; }
}
