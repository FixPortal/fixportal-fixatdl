// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Controls.Support;
using FixPortal.FixAtdl.Model.Elements.Support;

namespace FixPortal.FixAtdl.Model.Controls;

/// <summary>
/// Represents the Slider_t control element within FIXatdl.
/// </summary>
/// <remarks>ListItems select discrete enumerations; without ListItems the slider holds a numeric value.</remarks>
public class Slider_t : ListControlBase
{
    private readonly SingleSpinner_t _numeric;

    /// <summary>
    /// Initializes a new instance of <see cref="Slider_t"/> using the supplied ID.
    /// </summary>
    /// <param name="id">ID for this control.</param>
    public Slider_t(string id)
        : base(id)
    {
        _numeric = new SingleSpinner_t(id);
    }

    /// <summary>Gets or sets the increment for a numeric slider.</summary>
    public decimal? Increment
    {
        get => _numeric.Increment;
        set => _numeric.Increment = value;
    }

    /// <inheritdoc />
    public override bool HasEnumeratedState => HasListItems && base.HasEnumeratedState;

    /// <inheritdoc />
    public override void LoadInitValue(FixFieldValueProvider controlInitValueProvider)
    {
        if (HasListItems)
        {
            base.LoadInitValue(controlInitValueProvider);
            return;
        }

        _numeric.ParameterRef = ParameterRef;
        _numeric.InitPolicy = InitPolicy;
        _numeric.InitFixField = InitFixField;
        _numeric.SetValue(InitValue);
        _numeric.InitValue = (decimal?)_numeric.GetCurrentValue();
        _numeric.LoadInitValue(controlInitValueProvider);
    }

    /// <inheritdoc />
    public override object GetCurrentValue() => HasListItems ? base.GetCurrentValue() : _numeric.GetCurrentValue();

    /// <inheritdoc />
    public override void SetValue(object newValue)
    {
        if (HasListItems)
        {
            base.SetValue(newValue);
        }
        else
        {
            _numeric.SetValue(newValue);
        }
    }

    /// <inheritdoc />
    public override void Reset()
    {
        base.Reset();
        _numeric.Reset();
    }

    /// <inheritdoc />
    public override void SetValueFromParameter(IParameter parameter)
    {
        if (HasListItems)
        {
            base.SetValueFromParameter(parameter);
        }
        else
        {
            _numeric.SetValueFromParameter(parameter);
        }
    }

    /// <inheritdoc />
    public override IParameterConvertible GetValueForParameter() =>
        HasListItems ? base.GetValueForParameter() : _numeric;

    /// <inheritdoc />
    public override decimal? ToDecimal(IParameter targetParameter, IFormatProvider provider) =>
        HasListItems ? base.ToDecimal(targetParameter, provider) : _numeric.ToDecimal(targetParameter, provider);

    /// <inheritdoc />
    public override int? ToInt32(IParameter targetParameter, IFormatProvider provider) =>
        HasListItems ? base.ToInt32(targetParameter, provider) : _numeric.ToInt32(targetParameter, provider);

    /// <inheritdoc />
    public override uint? ToUInt32(IParameter targetParameter, IFormatProvider provider) =>
        HasListItems ? base.ToUInt32(targetParameter, provider) : _numeric.ToUInt32(targetParameter, provider);

    /// <inheritdoc />
    public override string ToString(IParameter targetParameter) =>
        HasListItems ? base.ToString(targetParameter) : _numeric.ToString(targetParameter);
}
