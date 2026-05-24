#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;
using System.Collections.Generic;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Resources;
using FixPortal.FixAtdl.Utility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ThrowHelper = FixPortal.FixAtdl.Diagnostics.ThrowHelper;

namespace FixPortal.FixAtdl.Validation;

// TODO: Implement IDisposable
public abstract class EditEvaluator<T> : IResolvable<Strategy_t, T> where T : class, IValueProvider
{
    // FP Enhancement: 2026-05-23 — TODO wire injected logger when refactoring class to accept ILogger.
    private readonly ILogger _log = NullLogger.Instance;

    private Edit_t<T> _edit = null!;
    private EditRef_t<T> _editRef = null!;

    public HashSet<string> Sources
    {
        get
        {
            if (_editRef != null)
                return _editRef.Sources;
            else if (_edit != null)
                return _edit.Sources;

            throw ThrowHelper.New<InvalidOperationException>(this, ErrorMessages.NeitherEditNorEditRefSetOnObject, GetType().Name);
        }
    }

    public bool CurrentState
    {
        get
        {
            if (_edit != null)
                return _edit.CurrentState;
            else if (_editRef != null)
                return _editRef.CurrentState;

            throw ThrowHelper.New<InvalidOperationException>(this, ErrorMessages.NeitherEditNorEditRefSetOnObject, GetType().Name);
        }
    }

    public EditRef_t<T> EditRef
    {
        get { return _editRef; }

        set
        {
            if (_edit != null)
                throw ThrowHelper.New<InvalidOperationException>(this, ErrorMessages.BothEditAndEditRefSetOnObject, GetType().Name);

            _editRef = value;
        }
    }

    public Edit_t<T> Edit
    {
        get { return _edit; }

        set
        {
            if (_editRef != null)
                throw ThrowHelper.New<InvalidOperationException>(this, ErrorMessages.BothEditAndEditRefSetOnObject, GetType().Name);

            _edit = value;
        }
    }

    /// <summary>
    /// Evaluates based on the current field values and any additional FIX field values that this EditEvaluator
    /// references.  Used for evaluating Edits in the context of StrategyEdits.
    /// </summary>
    /// <param name="additionalValues">Any additional FIX field values that may be required in the Edit evaluation.</param>
    public void Evaluate(FixFieldValueProvider additionalValues)
    {
        _log.LogDebug("EditEvaluator evaluating state of Edit_t/EditRef_t; current state is {CurrentState}", CurrentState.ToString().ToLower());

        if (_edit != null)
            _edit.Evaluate(additionalValues);
        else if (_editRef != null)
            _editRef.Evaluate(additionalValues);
        else
            throw ThrowHelper.New<InvalidOperationException>(this, ErrorMessages.NeitherEditNorEditRefSetOnObject, GetType().Name);

        _log.LogDebug("EditEvaluator evaluated to state {CurrentState}", CurrentState.ToString().ToLower());
    }

    /// <summary>
    /// Evaluates based on the current field values.  Used for evaluating Edits in the context of StateRules.
    /// </summary>
    public void Evaluate()
    {
        _log.LogDebug("EditEvaluator evaluating state of Edit_t/EditRef_t; current state is {CurrentState}", CurrentState.ToString().ToLower());

        if (_edit != null)
            _edit.Evaluate();
        else if (_editRef != null)
            _editRef.Evaluate();
        else
            throw ThrowHelper.New<InvalidOperationException>(this, ErrorMessages.NeitherEditNorEditRefSetOnObject, GetType().Name);

        _log.LogDebug("EditEvaluator evaluated to state {CurrentState}", CurrentState.ToString().ToLower());
    }

    #region IResolvable<Strategy_t> Members

    /// <summary>
    /// Resolves all interdependencies e.g. edits to edit refs, control values to edits, etc.  Called once
    /// all strategies have been loaded as there may be dependencies on EditRefs at the global level.
    /// </summary>
    void IResolvable<Strategy_t, T>.Resolve(Strategy_t strategy, ISimpleDictionary<T> sourceCollection)
    {
        if (_editRef != null)
            (_editRef as IResolvable<Strategy_t, T>).Resolve(strategy, sourceCollection);
        else if (_edit != null)
            (_edit as IResolvable<Strategy_t, T>).Resolve(strategy, sourceCollection);
    }

    #endregion IResolvable<Strategy_t> Members
}


