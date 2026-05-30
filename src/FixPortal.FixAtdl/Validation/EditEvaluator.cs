// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Resources;
using FixPortal.FixAtdl.Utility;
using ThrowHelper = FixPortal.FixAtdl.Diagnostics.ThrowHelper;

namespace FixPortal.FixAtdl.Validation;

// TODO: Implement IDisposable
/// <summary>
/// Provides shared edit-evaluation behavior for state rules and strategy edits.
/// </summary>
public abstract class EditEvaluator<T> : IResolvable<Strategy_t, T> where T : class, IValueProvider
{
    /// <summary>
    /// Gets the set of field sources referenced by the active edit or edit reference.
    /// </summary>
    public HashSet<string> Sources
    {
        get
        {
            if (EditRef != null)
            {
                return EditRef.Sources;
            }
            else if (Edit != null)
            {
                return Edit.Sources;
            }

            throw ThrowHelper.New<InvalidOperationException>(this, ErrorMessages.NeitherEditNorEditRefSetOnObject, GetType().Name);
        }
    }

    /// <summary>
    /// Gets the current evaluation state of the active edit or edit reference.
    /// </summary>
    public bool CurrentState
    {
        get
        {
            if (Edit != null)
            {
                return Edit.CurrentState;
            }
            else if (EditRef != null)
            {
                return EditRef.CurrentState;
            }

            throw ThrowHelper.New<InvalidOperationException>(this, ErrorMessages.NeitherEditNorEditRefSetOnObject, GetType().Name);
        }
    }

    /// <summary>
    /// Gets or sets the referenced edit wrapper.
    /// </summary>
    public EditRef_t<T> EditRef
    {
        get;

        set
        {
            if (Edit != null)
            {
                throw ThrowHelper.New<InvalidOperationException>(this, ErrorMessages.BothEditAndEditRefSetOnObject, GetType().Name);
            }

            field = value;
        }
    } = null!;

    /// <summary>
    /// Gets or sets the direct edit definition.
    /// </summary>
    public Edit_t<T> Edit
    {
        get;

        set
        {
            if (EditRef != null)
            {
                throw ThrowHelper.New<InvalidOperationException>(this, ErrorMessages.BothEditAndEditRefSetOnObject, GetType().Name);
            }

            field = value;
        }
    } = null!;

    /// <summary>
    /// Evaluates based on the current field values and any additional FIX field values that this EditEvaluator
    /// references.  Used for evaluating Edits in the context of StrategyEdits.
    /// </summary>
    /// <param name="additionalValues">Any additional FIX field values that may be required in the Edit evaluation.</param>
    public void Evaluate(FixFieldValueProvider additionalValues)
    {
        if (Edit != null)
        {
            Edit.Evaluate(additionalValues);
        }
        else if (EditRef != null)
        {
            EditRef.Evaluate(additionalValues);
        }
        else
        {
            throw ThrowHelper.New<InvalidOperationException>(this, ErrorMessages.NeitherEditNorEditRefSetOnObject, GetType().Name);
        }
    }

    /// <summary>
    /// Evaluates based on the current field values.  Used for evaluating Edits in the context of StateRules.
    /// </summary>
    public void Evaluate()
    {
        if (Edit != null)
        {
            Edit.Evaluate();
        }
        else if (EditRef != null)
        {
            EditRef.Evaluate();
        }
        else
        {
            throw ThrowHelper.New<InvalidOperationException>(this, ErrorMessages.NeitherEditNorEditRefSetOnObject, GetType().Name);
        }
    }

    #region IResolvable<Strategy_t> Members

    /// <summary>
    /// Resolves all interdependencies e.g. edits to edit refs, control values to edits, etc.  Called once
    /// all strategies have been loaded as there may be dependencies on EditRefs at the global level.
    /// </summary>
    /// <param name="strategy">The strategy providing resolution context.</param>
    /// <param name="sourceCollection">The value source collection used to resolve field references.</param>
    void IResolvable<Strategy_t, T>.Resolve(Strategy_t strategy, ISimpleDictionary<T> sourceCollection)
    {
        if (EditRef != null)
        {
            (EditRef as IResolvable<Strategy_t, T>).Resolve(strategy, sourceCollection);
        }
        else if (Edit != null)
        {
            (Edit as IResolvable<Strategy_t, T>).Resolve(strategy, sourceCollection);
        }
    }

    #endregion IResolvable<Strategy_t> Members
}
