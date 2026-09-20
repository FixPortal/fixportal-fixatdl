// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Collections;
using System.Collections.Specialized;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Controls;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Resources;
using FixPortal.FixAtdl.Utility;
using FixPortal.FixAtdl.Validation;
using ThrowHelper = FixPortal.FixAtdl.Diagnostics.ThrowHelper;

namespace FixPortal.FixAtdl.Model.Collections;

/// <summary>
/// Provides read-only keyed access to the controls defined for a strategy.
/// </summary>
public class ReadOnlyControlCollection : IParentable<Strategy_t>, IEnumerable<Control_t>, ISimpleDictionary<Control_t>
{
    private Strategy_t Owner { get; set; }
    private readonly Dictionary<string, Control_t> _controls = [];

    // Tracks which control Ids each subscribed panel collection contributed, so a Reset (Clear) from
    // one collection removes only that collection's controls. Rebuilding from the layout root instead
    // would silently drop controls living on panels not reachable from the root (#R23).
    private readonly Dictionary<object, HashSet<string>> _controlIdsBySource = [];

    /// <summary>
    /// Initializes a new <see cref="ReadOnlyControlCollection"/>.
    /// </summary>
    /// <param name="owner">The owning strategy.</param>
    public ReadOnlyControlCollection(Strategy_t owner)
    {
        Owner = owner;
    }

    internal void SourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                HandleAddAction(e, sender);
                break;

            // MSDN documentation says helpfully: "The content of the collection changed dramatically."
            case NotifyCollectionChangedAction.Reset:
                HandleResetAction(sender);
                break;

            case NotifyCollectionChangedAction.Remove:
                HandleRemoveAction(e, sender);
                break;

            case NotifyCollectionChangedAction.Replace:
                HandleReplaceAction(e, sender);
                break;
        }
    }

    private void HandleResetAction(object? sender)
    {
        // A Reset carries no item payload, so only the sender's recorded contributions can be removed.
        // An unknown sender contributed nothing, so there is nothing to do.
        if (sender != null && _controlIdsBySource.Remove(sender, out HashSet<string>? ids))
        {
            foreach (string id in ids)
            {
                _controls.Remove(id);
            }
        }
    }

    private void AddControl(Control_t control)
    {
        if (!_controls.TryAdd(control.Id, control))
        {
            throw ThrowHelper.New<DuplicateKeyException>(
                this,
                ErrorMessages.AttemptToAddDuplicateKey,
                control.Id,
                "Controls"
            );
        }
    }

    private void HandleAddAction(NotifyCollectionChangedEventArgs e, object? sender)
    {
        foreach (Control_t item in e.NewItems!)
        {
            AddControl(item);
            if (sender != null)
            {
                if (!_controlIdsBySource.TryGetValue(sender, out HashSet<string>? ids))
                {
                    ids = [];
                    _controlIdsBySource[sender] = ids;
                }
                ids.Add(item.Id);
            }
        }
    }

    private void HandleRemoveAction(NotifyCollectionChangedEventArgs e, object? sender)
    {
        foreach (Control_t item in e.OldItems!)
        {
            _controls.Remove(item.Id);
            if (sender != null && _controlIdsBySource.TryGetValue(sender, out HashSet<string>? ids))
            {
                ids.Remove(item.Id);
                // Drop an emptied entry: the key is the sender's ControlCollection, so keeping it
                // would retain that panel for the strategy's lifetime.
                if (ids.Count == 0)
                {
                    _controlIdsBySource.Remove(sender);
                }
            }
        }
    }

    private void HandleReplaceAction(NotifyCollectionChangedEventArgs e, object? sender)
    {
        for (int n = 0; n < e.OldItems!.Count; n++)
        {
            string oldId = ((Control_t)e.OldItems[n]!).Id;
            Control_t newControl = (Control_t)e.NewItems![n]!;

            if (newControl.Id != oldId && !_controls.TryAdd(newControl.Id, newControl))
            {
                throw ThrowHelper.New<DuplicateKeyException>(
                    this,
                    ErrorMessages.AttemptToAddDuplicateKey,
                    newControl.Id,
                    "Controls"
                );
            }

            if (newControl.Id != oldId)
            {
                _controls.Remove(oldId);
                if (sender != null && _controlIdsBySource.TryGetValue(sender, out HashSet<string>? ids))
                {
                    ids.Remove(oldId);
                    ids.Add(newControl.Id);
                }
            }
            else
            {
                _controls[oldId] = newControl;
            }
        }
    }

    /// <summary>
    /// Determines whether the collection contains a control with the specified identifier.
    /// </summary>
    /// <param name="key">The control identifier to look up.</param>
    /// <returns><see langword="true"/> if the control exists; otherwise, <see langword="false"/>.</returns>
    public bool Contains(string key)
    {
        return _controls.ContainsKey(key);
    }

    /// <summary>
    /// Gets the control with the specified identifier.
    /// </summary>
    /// <param name="key">The control identifier.</param>
    public Control_t this[string key] => _controls[key];

    /// <summary>
    /// Loads the initial values for each control based on the InitPolicy, InitFixField and InitValue attributes.
    /// </summary>
    /// <param name="controlInitValueProvider">Value provider for initializing control values from InitFixField.</param>
    /// <remarks>The spec states: 'If the value of the initPolicy attribute is undefined or equal to "UseValue" and the initValue attribute is
    /// defined then initialize with initValue.  If the value is equal to "UseFixField" then attempt to initialize with the value of
    /// the tag specified in the initFixField attribute. If the value is equal to "UseFixField" and it is not possible to access the
    /// value of the specified fix tag then revert to using initValue. If the value is equal to "UseFixField", the field is not accessible,
    /// and initValue is not defined, then do not initialize.</remarks>
    public void LoadDefaults(FixFieldValueProvider controlInitValueProvider)
    {
        Control_t? control = null;

        try
        {
            foreach (Control_t thisControl in this)
            {
                control = thisControl;

                thisControl.LoadInitValue(controlInitValueProvider);
            }
        }
        catch (Exception ex)
        {
            throw ThrowHelper.Rethrow(
                this,
                ex,
                ErrorMessages.InitControlValueError,
                control != null ? control.Id : "(unknown)"
            );
        }
    }

    /// <summary>
    /// Updates the parameter values from the controls in this control collection.
    /// </summary>
    /// <param name="parameters">Collection of parameters to be updated.</param>
    /// <param name="shortCircuit">If true, this method returns as soon as any error is found; if false, an attempt is made to update all parameter
    /// values before the method returns.</param>
    /// <param name="validationResults">If one or more validations fail, this parameter contains a list of ValidationResults; null otherwise.</param>
    public bool TryUpdateParameterValues(
        ParameterCollection parameters,
        bool shortCircuit,
        out IList<ValidationResult>? validationResults
    )
    {
        bool isValid = true;
        validationResults = null;
        var handledParameters = new HashSet<string>(StringComparer.Ordinal);

        foreach (Control_t control in this)
        {
            string? parameter = control.ParameterRef;

            if (parameter == null)
            {
                continue;
            }

            if (!parameters.Contains(parameter))
            {
                throw ThrowHelper.New<ReferencedObjectNotFoundException>(
                    this,
                    ErrorMessages.UnresolvedParameterRefError,
                    parameter
                );
            }

            // Only radio-group members share a parameter by design: they resolve to a single value
            // source (GetParameterValueSource), so update and report that parameter once, not once per
            // member. Non-radio controls sharing a ParameterRef are an independent authoring choice —
            // each keeps its own update, in collection order (last one wins).
            if (control is RadioButton_t && !handledParameters.Add(parameter))
            {
                continue;
            }

            ValidationResult result = parameters[parameter].SetValueFromControl(GetParameterValueSource(control));

            if (result.IsValid)
            {
                continue;
            }

            validationResults ??= [];
            validationResults.Add(result);

            if (shortCircuit)
            {
                return false;
            }

            isValid = false;
        }

        return isValid;
    }

    /// <summary>
    /// Gets the control contributing this parameter's value. A selected radio button takes
    /// precedence over unselected members of the same group that share its parameter.
    /// Consumers validating individual controls should use this same source as bulk updates.
    /// </summary>
    /// <param name="control">Control whose parameter is being validated.</param>
    /// <returns>The selected radio sibling, or the supplied control when none is selected.</returns>
    public Control_t GetParameterValueSource(Control_t control)
    {
        if (control is not RadioButton_t radio || radio.ParameterRef == null)
        {
            return control;
        }

        // radioGroup is optional in the schema: ungrouped radios sharing a parameter act as an implicit
        // group with their panel siblings (mirroring SetCompanionRadioButton), so the selected sibling
        // must be resolved there - otherwise the last enumerated unselected radio nulls the value (#R22).
        IEnumerable<RadioButton_t> candidates = string.IsNullOrEmpty(radio.RadioGroup)
            ? radio.OwningStrategyPanel?.Controls.OfType<RadioButton_t>() ?? []
            : this.OfType<RadioButton_t>().Where(candidate => candidate.RadioGroup == radio.RadioGroup);

        // ponytail: linear scan per radio, index groups if layouts grow beyond ordinary forms.
        return candidates.FirstOrDefault(candidate =>
                candidate.ParameterRef == radio.ParameterRef && candidate.GetCurrentValue() is true
            ) ?? control;
    }

    /// <summary>
    /// Updates the values of each control from its respective parameter.
    /// </summary>
    /// <param name="parameters">Parameter collection.</param>
    public void UpdateValuesFromParameters(ParameterCollection parameters)
    {
        foreach (Control_t control in this)
        {
            bool hasParameterRef = control.ParameterRef != null;
            bool isValidParameter = hasParameterRef && parameters.Contains(control.ParameterRef!);
            IParameter parameter = isValidParameter ? parameters[control.ParameterRef!] : null!;
            object parameterValue = isValidParameter ? parameter.GetCurrentValue() : null!;

            if (hasParameterRef && !isValidParameter)
            {
                throw ThrowHelper.New<ReferencedObjectNotFoundException>(
                    this,
                    ErrorMessages.UnresolvedParameterRefError,
                    control.ParameterRef
                );
            }

            // An empty parameter clears its bound control rather than leaving stale UI state behind.
            if (parameterValue == null)
            {
                control.Reset();
                UpdateRelatedHelperControls(control);
                continue;
            }

            try
            {
                control.SetValueFromParameter(parameter);
            }
            catch (Exception ex)
                when (ex is ArgumentException or FormatException or InvalidCastException or OverflowException)
            {
                // Defense in depth: a value/type-conversion failure while pushing a parameter value into
                // its bound control (e.g. an unresolvable date/time Kind) must not propagate as a raw,
                // uncaught exception (D-F8) - wrap it with control/parameter context instead.
                throw ThrowHelper.Rethrow(
                    this,
                    ex,
                    ErrorMessages.UnsuccessfulSetParameterOperation,
                    control.ParameterRef!,
                    control.Id,
                    ex.Message
                );
            }

            UpdateRelatedHelperControls(control);
        }
    }

    /// <summary>
    /// Evaluates all the state rules for each control.
    /// </summary>
    public void RunStateRules()
    {
        foreach (Control_t control in this)
        {
            control.StateRules.EvaluateAll();
        }
    }

    /// <summary>
    /// Resets every control in this collection to its empty state.
    /// </summary>
    public void ResetAll()
    {
        foreach (Control_t control in this)
        {
            control.Reset();
        }
    }

    /// <summary>
    /// Resolves all the dependencies between each control's StateRules and their dependent control values.
    /// </summary>
    public void ResolveAll()
    {
        foreach (Control_t control in this)
        {
            control.StateRules.ResolveAll(Owner);
        }
    }

    // This is a bit of a hack to address a design deficiency in FIXatdl 1.1, whereby when doing order amendments
    // the state of any helper controls is not directly available from the input FIX fields.
    // To simplify matters, we only apply this algorithm in the scenario when the StateRule's immediate Edit_t
    // has a toggleable control as its source and the operator is 'EQ' (this is the same as atdl4j as at the
    // time of writing).
    private void UpdateRelatedHelperControls(Control_t control)
    {
        foreach (StateRule_t stateRule in control.StateRules)
        {
            Edit_t<Control_t>? edit = stateRule.Edit;

            if (edit is not { } || stateRule.Value != Atdl.NullValue || edit.Operator != Operator_t.Equal)
            {
                continue;
            }

            string sourceControlId = edit.Field;

            if (
                IsValidControlId(sourceControlId)
                && this[sourceControlId] is { } sourceControl
                && sourceControl.IsToggleable
                && bool.TryParse(edit.Value, out bool result)
            )
            {
                ApplyHelperControlToggle(sourceControl, result);
            }
        }
    }

    private void ApplyHelperControlToggle(Control_t sourceControl, bool result)
    {
        // Radio buttons can only be set directly; un-setting is done via the companion control.
        if (sourceControl is CheckBox_t || !result)
        {
            sourceControl.SetValue(!result);
        }
        else if (sourceControl is RadioButton_t radioButton)
        {
            SetCompanionRadioButton(radioButton);
        }
    }

    private bool IsValidControlId(string value)
    {
        return this.Any(c => c.Id == value);
    }

    // This method looks for the sole companion radio button in the same group as the supplied button.
    private void SetCompanionRadioButton(RadioButton_t radioButton)
    {
        // Approach 1: RadioGroup name; Approach 2 (fallback): sibling controls on same panel.
        IEnumerable<RadioButton_t> radioButtons = !string.IsNullOrEmpty(radioButton.RadioGroup)
            ? _controls
                .Values.OfType<RadioButton_t>()
                .Where(c => c.Id != radioButton.Id && c.RadioGroup == radioButton.RadioGroup)
            : radioButton.OwningStrategyPanel?.Controls.OfType<RadioButton_t>().Where(c => c.Id != radioButton.Id)
                ?? Enumerable.Empty<RadioButton_t>();

        // The query is lazy; Count() + First() would enumerate it twice. Materialise once
        // (Take(2) is enough to distinguish "exactly one companion").
        List<RadioButton_t> companions = [.. radioButtons.Take(2)];

        if (companions.Count == 1)
        {
            companions[0].SetValue(true);
        }
    }

    #region IParentable<Strategy_t> Members

    /// <summary>
    /// Gets/sets the parent/owner of this control collection.
    /// </summary>
    Strategy_t IParentable<Strategy_t>.Parent
    {
        get => Owner;
        set => Owner = value;
    }

    #endregion

    #region IEnumerable<Control_t> Members

    IEnumerator<Control_t> IEnumerable<Control_t>.GetEnumerator()
    {
        foreach (Control_t control in _controls.Values)
        {
            yield return control;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<Control_t>)this).GetEnumerator();
    }

    #endregion IEnumerable<Control_t> Members
}
