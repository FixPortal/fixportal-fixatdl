#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;
using Atdl4net.Model.Controls.Support;
using Atdl4net.Model.Enumerations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Atdl4net.Model.Controls;

/// <summary>
/// Represents the RadioButtonList_t control element within FIXatdl.
/// </summary>
public class RadioButtonList_t : ListControlBase, IOrientableControl
{
    // FP Enhancement: 2026-05-23 — TODO wire injected logger when refactoring class to accept ILogger.
    private static readonly ILogger _log = NullLogger.Instance;

    /// <summary>
    /// Initializes a new instance of <see cref="RadioButtonList_t"/> using the supplied ID.
    /// </summary>
    /// <param name="id">ID for this control.</param>
    public RadioButtonList_t(string id)
        : base(id)
    {
        _log.LogDebug("New RadioButtonList_t created as control {Arg0}", id);
    }

    #region IOrientableControl Members

    /// <summary>Must be “HORIZONTAL” or “VERTICAL”. Declares the orientation of the radio buttons within a RadioButtonList
    ///  or the checkboxes within a CheckBoxList.  Applicable when xsi:type is RadioButtonList_t or CheckBoxList_t.</summary>
    public Orientation_t? Orientation { get; set; }

    #endregion
}
