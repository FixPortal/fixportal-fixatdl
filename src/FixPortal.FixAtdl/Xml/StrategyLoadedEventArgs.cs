#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;

namespace FixPortal.FixAtdl.Xml;

/// <summary>
/// Event arguments raised by <see cref="StrategiesReader"/> each time a strategy is loaded from
/// an ATDL document.
/// </summary>
/// <remarks>FP Enhancement: 2026-05-23 — moved from Atdl4net.Notification to FixPortal.FixAtdl.Xml when
/// the Notification assembly was removed (Task A8).  The event itself is still public API.</remarks>
public sealed class StrategyLoadedEventArgs : EventArgs
{
    /// <summary>Gets the zero-based index of the strategy just loaded.</summary>
    public int Index { get; }

    /// <summary>Gets the total number of strategies in the document (0 if unknown at load time).</summary>
    public int Total { get; }

    /// <summary>Gets the name of the strategy that was loaded.</summary>
    public string StrategyName { get; }

    /// <summary>
    /// Initializes a new <see cref="StrategyLoadedEventArgs"/>.
    /// </summary>
    public StrategyLoadedEventArgs(int index, int total, string strategyName)
    {
        Index = index;
        Total = total;
        StrategyName = strategyName;
    }
}

