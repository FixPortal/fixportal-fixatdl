// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Resources;
using FixPortal.FixAtdl.Utility;
using FixPortal.FixAtdl.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ThrowHelper = FixPortal.FixAtdl.Diagnostics.ThrowHelper;

namespace FixPortal.FixAtdl.Model.Elements;

/// <summary>
/// Represents the FIXatdl type Edit_t when it occurs outside of a StateRule_t or a StrategyEdit_t element.
/// </summary>
public class Edit_t
{
    /// <summary>
    /// Gets/sets the first field name for comparison. When the edit is used within a StateRule, this field 
    /// must refer to the ID of a Control. When the edit is used within a StrategyEdit, this field must refer 
    /// to either the name of a parameter or a standard FIX field name. When referring to a standard FIX tag
    /// then the name must be pre-pended with the string "FIX_", e.g. "FIX_OrderQty". Required the Operator is 
    /// not null.
    /// </summary>
    public string Field { get; set; } = null!;

    /// <summary>
    /// Gets/sets the optional second field name for comparison. When the edit is used within a StateRule, this field
    /// must refer to the ID of a Control. When the edit is used within a StrategyEdit, this field must refer
    /// to either the name of a parameter or a standard FIX field name. When referring to a standard FIX tag
    /// then the name must be pre-pended with the string "FIX_", e.g. "FIX_OrderQty".
    /// </summary>
    public string Field2 { get; set; } = null!;

    public string Id { get; set; } = null!;

    public Operator_t? Operator { get; set; }

    public LogicOperator_t? LogicOperator { get; set; }

    public string Value { get; set; } = null!;

    public EditCollection Edits { get; private set; }

    public Edit_t()
    {
        Edits = [];
    }
}

/// <summary>
/// Represents a FIXatdl Edit_t when implemented within a StateRule_t or StrategyEdit_t element.
/// </summary>
public class Edit_t<T> : IEdit<T>, IResolvable<Strategy_t, T> where T : class, IValueProvider
{
    // Use FixPortal.FixAtdl.Validation namespace rather than FixPortal.FixAtdl.Model.Elements for debugging purposes
    // FP Enhancement: 2026-05-23 — TODO wire injected logger when refactoring class to accept ILogger.
    private static readonly ILogger _log = NullLogger.Instance;
    private static readonly bool isPartOfStrategyEdit = typeof(T) == typeof(IParameter);
    private T _fieldSource = null!;
    private T _field2Source = null!;

    /// <summary>
    /// Initializes a new <see cref="Edit{T}"/> instance.
    /// </summary>
    public Edit_t()
    {
        Edits = [];
        EditRefs = new EditRefCollection<T>(Edits);

        // For StrategyEdits, we want to start with the assumption that the current state of this
        // Edit is true (i.e., valid) before it has been evaluated
        CurrentState = isPartOfStrategyEdit;
    }

    /// <summary>
    /// Provides a string representation of this Edit_t, primarily for debugging purposes.
    /// </summary>
    /// <returns>String representation of this Edit_t.</returns>
    public override string ToString()
    {
        StringBuilder sb = new();

        sb.Append('(');

        if (Id != null)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, "Id=\"{0}\", ", Id);
        }

        if (LogicOperator != null)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, "LogicOperator=\"{0}\", ", LogicOperator);
        }

        if (Field != null)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, "Field=\"{0}\", ", Field);
        }

        if (Operator != null)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, "Operator=\"{0}\", ", Operator);
        }

        if (Value != null)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, "Value=\"{0}\", ", Value);
        }

        if (Field2 != null)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, "Field2=\"{0}\", ", Field2);
        }

        // Convert to string so we can remove trailing ', '
        string text = sb.ToString();

        return string.Format(CultureInfo.InvariantCulture, "{0})", text[..^2]);
    }

    /// <summary>
    /// Gets the collection of EditRefs for this Edit.
    /// </summary>
    public EditRefCollection<T> EditRefs { get; }

    /// <summary>
    /// Gets the set of sources for this Edit and its children.  As source is non-null Field or Field2 value.
    /// </summary>
    public HashSet<string> Sources
    {
        get
        {
            HashSet<string> sources = [];

            if (Operator != null)
            {
                sources.Add(Field);

                if (Field2 != null)
                {
                    sources.Add(Field2);
                }
            }
            else
            {
                foreach (string source in Edits.Sources)
                {
                    sources.Add(source);
                }
            }

            return sources;
        }
    }

    #region IEdit_t Members

    /// <summary>
    /// Gets/sets the name of field to be used as left hand side of the evaluation.
    /// </summary>
    public string Field { get; set; } = null!;

    /// <summary>
    /// Gets/sets the name of second (optional) field, to be used as the right hand side of the evaluation.
    /// </summary>
    public string Field2 { get; set; } = null!;

    /// <summary>
    /// Gets/sets the optional ID for this Edit.
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets/sets the optional operator - used when comparing two values.
    /// </summary>
    public Operator_t? Operator { get; set; }

    /// <summary>
    /// Gets/sets the optional fixed value to be used as the right hand side of the evaluation.
    /// </summary>
    /// <remarks>From the spec:<br/><br/>"When Edit is a descendant of a StateRule element, Value refers to the 
    /// value of the control referred by Field. If the control referred by Field has enumerated values then Value 
    /// refers to the enumID of one of the control's ListItem elements.<br/>
    /// When Edit is a descendant of a StrategyEdit element, Value refers to the wireValue of the parameter 
    /// referred by Field."</remarks>
    public string Value { get; set; } = null!;

    /// <summary>
    /// Gets the current state of this Edit based on the most recent evaluation.
    /// </summary>
    public bool CurrentState { get; private set; }

    /// <summary>
    /// Gets the collection of child Edits.  May be empty, unless LogicOperator is non-null.
    /// </summary>
    public EditEvaluatingCollection<T> Edits { get; }

    /// <summary>
    /// Gets/sets the optional logical operator - used when combining two or more Edits.
    /// </summary>
    public LogicOperator_t? LogicOperator
    {
        get => Edits.LogicOperator; set => Edits.LogicOperator = value;
    }

    /// <summary>
    /// Gets the current value of the field pointed to by the Field property.
    /// </summary>
    public object FieldValue
    {
        get
        {
            if (_fieldSource != null)
            {
                return _fieldSource.GetCurrentValue();
            }
            else
            {
                throw ThrowHelper.New<InvalidOperationException>(this, "Edit attempted to access FieldValue but requisite control was not set.");
            }
        }
    }

    /// <summary>
    /// Gets the current value of the field pointed to by the Field2 property.
    /// </summary>
    public object Field2Value
    {
        get
        {
            if (_field2Source != null)
            {
                return _field2Source.GetCurrentValue();
            }
            else
            {
                throw ThrowHelper.New<InvalidOperationException>(this, "Edit attempted to access Field2Value but requisite control was not set.");
            }
        }
    }

    /// <summary>
    /// Evaluates this Edit based on the current field values.
    /// </summary>
    public void Evaluate()
    {
        Evaluate(FixFieldValueProvider.Empty);
    }

    /// <summary>
    /// Evaluates this Edit based on the current field values and any supplied FIX field values.
    /// </summary>
    /// <param name="additionalValues">Any additional FIX field values that may be required in the Edit evaluation.</param>
    public void Evaluate(FixFieldValueProvider additionalValues)
    {
        _log.LogDebug("Evaluating Edit_t {Arg0}; current state is {Arg1}", ToString(), CurrentState.ToString().ToLower());

        if (Operator != null)
        {
            object lhs = GetLhsValue(additionalValues);

            CurrentState = Operator switch
            {
                Operator_t.Exist or Operator_t.NotExist => EvaluateExists(lhs),
                Operator_t.Equal or Operator_t.NotEqual => EvaluateEquality(lhs, GetRhsValue(additionalValues, lhs)),
                _ => EvaluateInequalityComparison((lhs as IComparable)!, (GetRhsValue(additionalValues, lhs) as IComparable)!),
            };
        }
        else if (LogicOperator != null)
        {
            Edits.Evaluate(additionalValues);

            CurrentState = Edits.CurrentState;
        }
        else
        {
            throw ThrowHelper.New<InvalidOperationException>(this, ErrorMessages.MissingOperatorsOnEdit);
        }

        _log.LogDebug("Evaluation of Edit_t {Arg0} yielded state of {Arg1}", ToString(), CurrentState.ToString().ToLower());
    }

    #endregion IEdit_t Members

    private bool EvaluateExists(object value)
    {
        bool checkingForExist = Operator == Operator_t.Exist;

        bool empty = value == null || value as string == string.Empty;

        bool result = checkingForExist ? !empty : empty;

        _log.LogDebug("Evaluated whether Field {Arg0} {Arg1} a value; result is {Arg2} (value was '{Arg3}')", Field,
            checkingForExist ? "has" : "does not have", result.ToString().ToLower(), empty ? "N/A" : value);

        return result;
    }

    private bool EvaluateEquality(object lhs, object rhs)
    {
        _log.LogDebug("Comparing values operand1={Arg0}, operand2={Arg1} for equality with operator {Arg2}", lhs, rhs, Operator);

        CheckForUnsupportedComparisons(lhs, rhs);

        bool equal = lhs == null
            ? rhs == null || rhs as string == Atdl.NullValue
            : lhs is IComparable comparableLhs && rhs is IComparable comparableRhs
                ? comparableLhs.CompareTo(comparableRhs) == 0
                : lhs.Equals(rhs);

        bool finalResult = Operator == Operator_t.Equal ? equal : !equal;

        _log.LogDebug("Result of equality comparison = {Arg0}", finalResult.ToString().ToLower());

        return finalResult;
    }

    private bool EvaluateInequalityComparison(IComparable lhs, IComparable rhs)
    {
        _log.LogDebug("Comparing values lhs='{Arg0}', rhs='{Arg1}' for inequality with operator {Arg2}", lhs, rhs, Operator);

        // It's not clear what the right thing is to do with a null LHS and an inequality operator
        // so we return false anyway
        if (lhs == null)
        {
            _log.LogDebug("Left hand side of inequality comparison is null so returning false");

            return false;
        }

        int compareResult = lhs.CompareTo(rhs);

        bool finalResult = false;

        switch (Operator)
        {
            case Operator_t.GreaterThan:
                finalResult = compareResult > 0;
                break;

            case Operator_t.GreaterThanOrEqual:
                finalResult = compareResult >= 0;
                break;

            case Operator_t.LessThan:
                finalResult = compareResult < 0;
                break;

            case Operator_t.LessThanOrEqual:
                finalResult = compareResult <= 0;
                break;
        }

        _log.LogDebug("Compared values '{Arg0}' and '{Arg1}' as part of Edit_t evaluation; result was {Arg2}",
            lhs, rhs, finalResult.ToString().ToLower());

        return finalResult;
    }

    private object GetLhsValue(FixFieldValueProvider additionalValues)
    {
        if (Field.StartsWith("FIX_", StringComparison.Ordinal))
        {
            return GetFixFieldValue(additionalValues, Field);
        }

        object result;
        object fieldValue = FieldValue;

        // If the field value can be converted into a number, most likely it should be treated as one
        // for comparison purposes
        result = fieldValue is string ? decimal.TryParse(fieldValue as string, out decimal number) ? number : fieldValue : fieldValue;

        return result;
    }

    private object GetRhsValue(FixFieldValueProvider additionalValues, object lhs)
    {
        if (Value != null)
        {
            return EditValueConverter.ConvertToComparableType(lhs, Value);
        }

        if (Field2 != null)
        {
            if (Field2.StartsWith("FIX_", StringComparison.Ordinal))
            {
                return GetFixFieldValue(additionalValues, Field2);
            }

            return Field2Value;
        }

        return null!;
    }

    private void CheckForUnsupportedComparisons(object lhs, object rhs)
    {
        // We don't currently support comparisons for type 'Data_t' which is represented by a char[].
        if (lhs is char[])
        {
            throw ThrowHelper.New<InvalidOperationException>(this, ErrorMessages.UnsupportedComparisonOperation, Value, new string(lhs as char[]));
        }

        if (rhs is char[])
        {
            throw ThrowHelper.New<InvalidOperationException>(this, ErrorMessages.UnsupportedComparisonOperation, Value, new string(rhs as char[]));
        }
    }

    private static object GetFixFieldValue(FixFieldValueProvider additionalValues, string fixField)
    {
        object? result;
        string? value;

        bool gotValue = additionalValues.TryGetValue(fixField, out value!);

        // If the FIX value can be converted into a number, most likely it should be treated as one
        // for comparison purposes
        result = gotValue ? decimal.TryParse(value, out decimal number) ? number : value : null;

        _log.LogDebug("Looked up FIX field {Arg0} for comparison; field was {Arg1}, value={Arg2}",
            fixField, gotValue ? "found" : "not found", gotValue ? result : "N/A");

        return result!;
    }

    #region IResolvable<Strategy_t> Members

    /// <summary>
    /// Resolves all interdependencies e.g. edits to edit refs, control values to edits, etc.  Called once
    /// all strategies have been loaded as there may be dependencies on EditRefs at the global level.
    /// </summary>
    void IResolvable<Strategy_t, T>.Resolve(Strategy_t strategy, ISimpleDictionary<T> sourceCollection)
    {
        (Edits as IResolvable<Strategy_t, T>).Resolve(strategy, sourceCollection);

        if (!string.IsNullOrEmpty(Field) && !Field.StartsWith("FIX_", StringComparison.Ordinal))
        {
            _fieldSource = sourceCollection.Contains(Field)
                ? sourceCollection[Field]
                : throw ThrowHelper.New<ReferencedObjectNotFoundException>(this, ErrorMessages.EditRefFieldControlNotFound, Field, "Field");
        }

        if (!string.IsNullOrEmpty(Field2) && !Field2.StartsWith("FIX_", StringComparison.Ordinal))
        {
            _field2Source = sourceCollection.Contains(Field2)
                ? sourceCollection[Field2]
                : throw ThrowHelper.New<ReferencedObjectNotFoundException>(this, ErrorMessages.EditRefFieldControlNotFound, Field2, "Field2");
        }
    }

    #endregion IResolvable<Strategy_t> Members
}
