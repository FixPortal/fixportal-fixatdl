// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Globalization;
using System.Text;
using FixPortal.FixAtdl.Utility;
using FixPortal.FixAtdl.Validation;

namespace FixPortal.FixAtdl.Model.Elements;

// IDisposable is not needed: StateRule_t inherits only EditEvaluator's edit references and subscribes to
// no events (see EditEvaluator). Vestige of the removed Notification assembly (Task A8).
/// <summary>
/// Represents a FIXatdl state rule that updates control state from edit evaluation.
/// </summary>
public class StateRule_t : EditEvaluator<Control_t>, IParentable<Control_t>
{
    private Control_t _owner = null!;

    /// <summary>
    /// Enabled state for this state rule.
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Value attribute for this state rule.
    /// </summary>
    public string Value { get; set; } = null!;

    /// <summary>
    /// Visible state for this state rule.
    /// </summary>
    public bool? Visible { get; set; }

    /// <summary>
    /// Provides a string representation of this StateRule_t, primarily for debugging purposes.
    /// </summary>
    /// <returns>String representation in the format (control_id, enabled_value_if_set, value_value_if_set, visible_value_if_set).</returns>
    public override string ToString()
    {
        StringBuilder sb = new();

        // _owner is unset until the rule is parented; ToString is a debug/logging path, so guard the
        // deref to avoid an NRE when a rule is logged before it is attached to its Control_t.
        sb.AppendFormat(CultureInfo.InvariantCulture, "(Control.ID=\"{0}\"", _owner?.Id);

        if (Enabled != null)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, ", enabled=\"{0}\"", Enabled.Value.ToString().ToLowerInvariant());
        }

        if (Value != null)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, ", value=\"{0}\"", Value);
        }

        if (Visible != null)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, ", visible=\"{0}\"", Visible.Value.ToString().ToLowerInvariant());
        }

        sb.Append(')');

        return sb.ToString();
    }

    #region IParentable<Control_t> Members

    Control_t IParentable<Control_t>.Parent
    {
        get => _owner; set => _owner = value;
    }

    #endregion IParentable<Control_t> Members
}
