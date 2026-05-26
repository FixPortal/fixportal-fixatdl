// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Collections.ObjectModel;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Utility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FixPortal.FixAtdl.Model.Collections;

/// <summary>
/// Represents the collection of state rules associated with a control.
/// </summary>
public class StateRuleCollection : Collection<StateRule_t>
{
    // FP Enhancement: 2026-05-23 — TODO wire injected logger when refactoring class to accept ILogger.
    private readonly NullLogger _log = NullLogger.Instance;

    private readonly Control_t _owner;

    /// <summary>
    /// Initializes a new <see cref="StateRuleCollection"/>.
    /// </summary>
    /// <param name="owner">The control that owns the collection.</param>
    public StateRuleCollection(Control_t owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Adds a state rule to the collection and assigns its parent control.
    /// </summary>
    /// <param name="item">The state rule to add.</param>
    public new void Add(StateRule_t item)
    {
        (item as IParentable<Control_t>).Parent = _owner;

        base.Add(item);

        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.LogDebug("StateRule_t {StateRule} added to StateRules for control Id {ControlId}", item, _owner.Id);
        }
    }

    /// <summary>
    /// Evaluates all state rules in the collection.
    /// </summary>
    public void EvaluateAll()
    {
        if (Items.Count > 0)
        {
            if (_log.IsEnabled(LogLevel.Debug))
            {
                _log.LogDebug("Evaluating all {Count} StateRule_t instances for control Id {ControlId}", Items.Count, _owner.Id);
            }
        }

        foreach (StateRule_t rule in Items)
        {
            rule.Evaluate();
        }
    }

    /// <summary>
    /// Resolves all edit refs, connects all edits to their controls.
    /// </summary>
    /// <param name="strategy">The strategy that provides the control sources for resolution.</param>
    public void ResolveAll(Strategy_t strategy)
    {
        foreach (StateRule_t rule in Items)
        {
            (rule as IResolvable<Strategy_t, Control_t>).Resolve(strategy, strategy.Controls);
        }
    }
}
