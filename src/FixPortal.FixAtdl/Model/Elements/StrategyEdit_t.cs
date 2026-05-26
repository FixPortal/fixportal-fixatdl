// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Validation;

namespace FixPortal.FixAtdl.Model.Elements;

/// <summary>
/// Represents the FIXatdl StrategyEdit element, which is a definition of a validation rule. A StrategyEdit element must 
/// contain an Edit element as a child. The boolean expression described by the Edit element is an assertion, 
/// i.e., validation succeeds if the condition described by the Edit is true and fails when the condition described by 
/// the Edit element is false. In the case where validation fails, the error message, supplied by the errorMsg attribute 
/// of the StrategyEdit, may be displayed to an OMS user or logged.
/// </summary>
public class StrategyEdit_t : EditEvaluator<IParameter>
{
    /// <summary>
    ///  Gets the internal ID for this StrategyEdit; used to support lookups when applying the results of validations to
    ///  controls.
    /// </summary>
    public string InternalId { get; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets/sets the error message to display when the boolean expression defined by StrategyEdit/Edit evaluates to False.
    /// </summary>
    public string ErrorMessage { get; set; } = null!;
}

