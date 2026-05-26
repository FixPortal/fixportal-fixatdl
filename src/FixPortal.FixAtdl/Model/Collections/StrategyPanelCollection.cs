// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Collections.ObjectModel;
using FixPortal.FixAtdl.Model.Elements;

namespace FixPortal.FixAtdl.Model.Collections;

/// <summary>
/// Represents the collection of child strategy panels within a parent panel.
/// </summary>
public class StrategyPanelCollection : Collection<StrategyPanel_t>
{
}
