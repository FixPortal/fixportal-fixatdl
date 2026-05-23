#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

namespace Atdl4net.Fix
{
    /// <summary>
    /// Provides access to a set of input FIX field values used to initialize controls from their
    /// corresponding FIX tag values.  Originally part of the WPF layer; moved here so the headless
    /// library can reference it without a UI dependency.
    /// </summary>
    /// <remarks>FP Enhancement: 2026-05-23 — migrated from Atdl4net.Wpf.ViewModel to Atdl4net.Fix
    /// when the WPF layer was removed (Task A8).  Callers supply a concrete implementation; the
    /// library itself only depends on this interface.</remarks>
    public interface IInitialFixValueProvider
    {
        /// <summary>
        /// Gets the collection of FIX field values available for control initialisation.
        /// </summary>
        FixTagValuesCollection InputFixValues { get; }
    }
}
