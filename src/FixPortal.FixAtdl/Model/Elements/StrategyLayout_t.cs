#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Model.Elements.Support;

namespace FixPortal.FixAtdl.Model.Elements;

/// <summary>
/// Represents the FIXatdl StrategyLayout element that contains the root StrategyPanel.
/// </summary>
public class StrategyLayout_t : IStrategyPanel
{
    /// <summary>
    /// Gets/sets the root StrategyPanel.
    /// </summary>
    public StrategyPanel_t StrategyPanel { get; set; } = null!;
}

