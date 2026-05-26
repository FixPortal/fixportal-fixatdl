// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

namespace FixPortal.FixAtdl.Utility;

/// <summary>
/// Exposes a parent relationship for a model object.
/// </summary>
/// <typeparam name="T">The parent type.</typeparam>
public interface IParentable<T>
{
    /// <summary>
    /// Gets or sets the parent object.
    /// </summary>
    T Parent { get; set; }
}
