// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Model.Elements;

namespace FixPortal.FixAtdl.Model.Controls.Support;

/// <summary>
/// Interface to support visitor pattern.
/// </summary>
public interface IControlVisitor
{
    /// <summary>
    /// Method that the visitor will call based on the type of the control parameter.
    /// </summary>
    /// <param name="control">Control to process as part of this visitor pattern.</param>
    void Visit(Control_t control);
}

