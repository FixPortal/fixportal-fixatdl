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
                foreach (Control_t item in e.NewItems!)
                {
                    if (_controls.ContainsKey(item.Id))
                    {
                        throw ThrowHelper.New<DuplicateKeyException>(this, ErrorMessages.AttemptToAddDuplicateKey, item.Id, "Controls");
                    }

                    _controls.Add(item.Id, item);
                }
                break;

            // MSDN documentation says helpfully: "The content of the collection changed dramatically."
            case NotifyCollectionChangedAction.Reset:
                _controls.Clear();
                break;

            case NotifyCollectionChangedAction.Remove:
                foreach (Control_t item in e.OldItems!)
                {
                    if (_controls.ContainsKey(item.Id))
                    {
                        _controls.Remove(item.Id);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Replace:
                for (int n = 0; n < e.OldItems!.Count; n++)
                {
                    string oldId = ((Control_t)e.OldItems[n]!).Id;
                    Control_t newControl = (Control_t)e.NewItems![n]!;

                    // Re-key under the replacement's OWN Id. Keying the new control under the old Id
                    // (as before) left it unreachable by its real Id and returned it under a stale
                    // key whenever the replacement carried a different Id.
                    _controls.Remove(oldId);
                    _controls[newControl.Id] = newControl;
                }
                break;
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
    public Control_t this[string key]
    {
        get
        {
            if (_controls.TryGetValue(key, out Control_t? value))
            {
                return value;
            }
            else
            {
                return null!;
            }
        }
    }

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
            throw ThrowHelper.Rethrow(this, ex, ErrorMessages.InitControlValueError, control != null ? control.Id : "(unknown)");
        }
    }

    /// <summary>
    /// Updates the parameter values from the controls in this control collection.
    /// </summary>
    /// <param name="parameters">Collection of parameters to be updated.</param>
    /// <param name="shortCircuit">If true, this method returns as soon as any error is found; if false, an attempt is made to update all parameter
    /// values before the method returns.</param>
    /// <param name="validationResults">If one or more validations fail, this parameter contains a list of ValidationResults; null otherwise.</param>
    public bool TryUpdateParameterValues(ParameterCollection parameters, bool shortCircuit, out IList<ValidationResult>? validationResults)
    {
        bool isValid = true;
        validationResults = null;

        foreach (Control_t control in this)
        {
            string parameter = control.ParameterRef;

            if (parameter != null)
            {
                if (!parameters.Contains(parameter))
                {
                    throw ThrowHelper.New<ReferencedObjectNotFoundException>(this, ErrorMessages.UnresolvedParameterRefError, parameter);
                }

                ValidationResult result = parameters[parameter].SetValueFromControl(control);

                if (!result.IsValid)
                {
                    validationResults ??= [];

                    validationResults.Add(result);

                    if (shortCircuit)
                    {
                        return false;
                    }

                    isValid = false;
                }
            }
        }

        return isValid;
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
                throw ThrowHelper.New<ReferencedObjectNotFoundException>(this, ErrorMessages.UnresolvedParameterRefError, control.ParameterRef);
            }

            // We only want to update the control value if the parameter has a value
            if (parameterValue != null)
            {
                control.SetValueFromParameter(parameter);

                UpdateRelatedHelperControls(control);
            }
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
            Edit_t<Control_t> edit = stateRule.Edit;

            if (stateRule.Value == Atdl.NullValue && edit.Operator == Operator_t.Equal)
            {
                string sourceControlId = edit.Field;

                if (IsValidControlId(sourceControlId))
                {
                    Control_t sourceControl = this[sourceControlId];


                    if (sourceControl.IsToggleable && bool.TryParse(edit.Value, out bool result))
                    {
                        // If the control is a radio button, then we can only set directly, un-set
                        // has be done by setting its companion control
                        if (sourceControl is CheckBox_t || !result)
                        {
                            sourceControl.SetValue(!result);
                        }
                        else
                        {
                            SetCompanionRadioButton((sourceControl as RadioButton_t)!);
                        }
                    }
                }
            }
        }
    }

    private bool IsValidControlId(string value)
    {
        return this.Any(c => c.Id == value);
    }

    // This method looks for all the radio buttons in the same group as the supplied radio button
    // and if there are only two
    private void SetCompanionRadioButton(RadioButton_t radioButton)
    {
        IEnumerable<RadioButton_t> radioButtons;

        // Approach 1 - use the radio button group name
        if (radioButton.RadioGroup != null)
        {
            radioButtons = from c in _controls.Values
                where c.Id != radioButton.Id &&
                      c is RadioButton_t t && t.RadioGroup == radioButton.RadioGroup
                select c as RadioButton_t;
        }
        else
        {
            // Approach 2 - look for radio buttons on the same panel
            radioButtons = from c in radioButton.OwningStrategyPanel.Controls
                where c.Id != radioButton.Id &&
                      c is RadioButton_t
                select c as RadioButton_t;
        }

        if (radioButtons.Count() == 1)
        {
            radioButtons.First().SetValue(true);
        }
    }

    #region IParentable<Strategy_t> Members

    /// <summary>
    /// Gets/sets the parent/owner of this control collection.
    /// </summary>
    Strategy_t IParentable<Strategy_t>.Parent
    {
        get => Owner; set => Owner = value;
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
