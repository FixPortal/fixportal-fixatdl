// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;
using FixPortal.FixAtdl.Model.Controls.Support;
using FixPortal.FixAtdl.Model.Enumerations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FixPortal.FixAtdl.Model.Controls;

/// <summary>
/// Represents the DoubleSpinner_t control element within FIXatdl.
/// </summary>
public class DoubleSpinner_t : NumericControlBase
{
    // FP Enhancement: 2026-05-23 — TODO wire injected logger when refactoring class to accept ILogger.
    private static readonly NullLogger _log = NullLogger.Instance;

    /// <summary>
    /// Initializes a new instance of <see cref="DoubleSpinner_t"/> using the supplied ID.
    /// </summary>
    /// <param name="id">ID for this control.</param>
    public DoubleSpinner_t(string id)
        : base(id)
    {
        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("New DoubleSpinner_t created as control {Arg0}", id);
        }
    }

    /// <summary>Limits the granularity of the inner spinner of a double spinner control. Useful in spinner objects to enforce
    ///  odd-lot and sub-penny restrictions.  Applicable when xsi:type is DoubleSpinner_t.</summary>
    public decimal? InnerIncrement { get; set; }

    /// <summary>For double spinner control, defines how to determine the increment for the inner set of spinners. Applicable 
    /// when xsi:type is DoubleSpinner_t only.</summary>
    public IncrementPolicy_t? InnerIncrementPolicy { get; set; }

    /// <summary>Limits the granularity of the outer spinner of a double spinner control. Useful in spinner objects to enforce
    ///  odd-lot and sub-penny restrictions.  Applicable when xsi:type is DoubleSpinner_t.</summary>
    public decimal? OuterIncrement { get; set; }

    /// <summary>For double spinner control, defines how to determine the increment for the outer set of spinners. Applicable 
    /// when xsi:type is DoubleSpinner_t only.</summary>
    public IncrementPolicy_t? OuterIncrementPolicy { get; set; }
}
