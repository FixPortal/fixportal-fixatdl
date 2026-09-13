// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
//
//   This software is released under the MIT License..
//
#endregion

using System.Globalization;
using System.Text;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Controls.Support;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Model.Types;
using FixPortal.FixAtdl.Resources;
using FixPortal.FixAtdl.Utility;
using FixPortal.FixAtdl.Validation;
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

    /// <summary>
    /// Gets or sets the optional identifier for this edit.
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the comparison operator used by this edit.
    /// </summary>
    public Operator_t? Operator { get; set; }

    /// <summary>
    /// Gets or sets the logical operator used to combine child edits.
    /// </summary>
    public LogicOperator_t? LogicOperator { get; set; }

    /// <summary>
    /// Gets or sets the optional fixed right-hand-side value for the edit.
    /// </summary>
    public string Value { get; set; } = null!;

    /// <summary>
    /// Gets the child edits contained by this edit.
    /// </summary>
    public EditCollection Edits { get; private set; }

    /// <summary>
    /// Initializes a new <see cref="Edit_t"/>.
    /// </summary>
    public Edit_t()
    {
        Edits = [];
    }
}

/// <summary>
/// Represents a FIXatdl Edit_t when implemented within a StateRule_t or StrategyEdit_t element.
/// </summary>
public class Edit_t<T> : IEdit<T>, IResolvable<Strategy_t, T>
    where T : class, IValueProvider
{
    // Use FixPortal.FixAtdl.Validation namespace rather than FixPortal.FixAtdl.Model.Elements for debugging purposes
    private static readonly bool isPartOfStrategyEdit = typeof(T) == typeof(IParameter);
    private T _fieldSource = null!;
    private T _field2Source = null!;

    /// <summary>
    /// Initializes a new <see cref="Edit_t{T}"/> instance.
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

        // Guard the trailing ", " trim: an empty edit leaves the buffer as just "(", so text[..^2]
        // would throw ArgumentOutOfRangeException.
        return text.Length > 1 ? string.Format(CultureInfo.InvariantCulture, "{0})", text[..^2]) : "()";
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
                // Field may be unset (e.g. only Field2 is configured); never add a null into the
                // HashSet<string>, which would NRE in consumers / pollute subscription wiring.
                if (Field != null)
                {
                    sources.Add(Field);
                }

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
        get => Edits.LogicOperator;
        set => Edits.LogicOperator = value;
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

            throw ThrowHelper.New<InvalidOperationException>(
                this,
                "Edit attempted to access FieldValue but requisite control was not set."
            );
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

            throw ThrowHelper.New<InvalidOperationException>(
                this,
                "Edit attempted to access Field2Value but requisite control was not set."
            );
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
        if (Operator != null)
        {
            // ValidateInvariants rejects an operator without 'field' at Resolve; guard here too so that
            // evaluating an unresolved edit raises a domain error instead of an NRE from Field.StartsWith.
            if (Field == null)
            {
                throw ThrowHelper.New<InvalidOperationException>(
                    this,
                    "Edit attempted to evaluate an operator comparison but the 'field' attribute was not set."
                );
            }

            object lhs = GetLhsValue(additionalValues);

            CurrentState = Operator switch
            {
                Operator_t.Exist or Operator_t.NotExist => EvaluateExists(lhs),
                Operator_t.Equal or Operator_t.NotEqual => EvaluateEquality(lhs, GetRhsValue(additionalValues, lhs)),
                _ => EvaluateInequalityComparison(lhs, GetRhsValue(additionalValues, lhs)),
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
    }

    #endregion IEdit_t Members

    private bool EvaluateExists(object value)
    {
        bool checkingForExist = Operator == Operator_t.Exist;

        // A list control returns a never-null EnumState; "nothing selected" is an all-false EnumState
        // (not null and not ""), so it must be treated as absent here or EX/NX would always be wrong
        // for list controls. Scalar/text/clock controls already return null when unset.
        bool empty = value is null or "" or EnumState { HasSelection: false };

        bool result = checkingForExist ? !empty : empty;

        return result;
    }

    private bool EvaluateEquality(object lhs, object rhs)
    {
        CheckForUnsupportedComparisons(lhs, rhs);

        bool equal = AreEqual(lhs, rhs);

        bool finalResult = Operator == Operator_t.Equal ? equal : !equal;

        return finalResult;
    }

    private bool EvaluateInequalityComparison(object? lhs, object? rhs)
    {
        // It's not clear what the right thing is to do with a null LHS and an inequality operator
        // so we return false anyway
        if (lhs == null)
        {
            return false;
        }

        // A null RHS — e.g. an inequality against a missing FIX field — is an indeterminate
        // comparison, not an ordering. Short-circuit it to false symmetrically with the null-LHS
        // guard above, rather than letting lhs.CompareTo(null) fabricate a definite (+1) result.
        if (rhs == null)
        {
            return false;
        }

        CheckForUnsupportedComparisons(lhs, rhs);

        object? normLhs = NormaliseValue(lhs, rhs);
        object? normRhs = NormaliseValue(rhs, lhs);

        // Operands of non-comparable or mismatched runtime types cannot be ordered
        if (
            normLhs is not IComparable comparable
            || normRhs is not IComparable
            || comparable.GetType() != normRhs.GetType()
        )
        {
            throw ThrowHelper.New<InvalidOperationException>(
                this,
                ErrorMessages.UnsupportedComparisonOperation,
                lhs,
                rhs
            );
        }

        // FIX wire values are opaque byte sequences: order strings ordinally, never by host culture (R18).
        int compareResult =
            comparable is string lhsText && normRhs is string rhsText
                ? string.CompareOrdinal(lhsText, rhsText)
                : comparable.CompareTo(normRhs);

        bool finalResult = Operator switch
        {
            Operator_t.GreaterThan => compareResult > 0,
            Operator_t.GreaterThanOrEqual => compareResult >= 0,
            Operator_t.LessThan => compareResult < 0,
            Operator_t.LessThanOrEqual => compareResult <= 0,
            _ => false,
        };

        return finalResult;
    }

    private static bool AreEqual(object? lhs, object? rhs)
    {
        lhs = NormaliseValue(lhs, rhs);
        rhs = NormaliseValue(rhs, lhs);

        if (lhs == null)
        {
            return rhs == null || rhs as string == Atdl.NullValue;
        }

        // RHS "{NULL}" is the FIXatdl sentinel for "field is not set" — never a real value to
        // numerically/enum-convert against. For a non-null LHS this always means "the field IS
        // set", so EQ-to-null is false (NE is true, via the caller's negation). An EnumState LHS
        // uses its own "no selection" check rather than Matches("{NULL}"), which would throw
        // since "{NULL}" is not a valid EnumID.
        if (rhs is Atdl.NullValue)
        {
            return lhs is EnumState { HasSelection: false };
        }

        if (lhs is EnumState lhsEnumState)
        {
            return rhs switch
            {
                string rhsEnumId => lhsEnumState.Matches(rhsEnumId),
                EnumState rhsState => lhsEnumState.Equals(rhsState),
                _ => lhs.Equals(rhs),
            };
        }

        if (lhs is string lhsEnumId && rhs is EnumState rhsEnumState)
        {
            return rhsEnumState.Matches(lhsEnumId);
        }

        // FIX wire values compare ordinally: string.CompareTo(string) would use the host's culture (R18).
        if (lhs is string lhsString && rhs is string rhsString)
        {
            return string.Equals(lhsString, rhsString, StringComparison.Ordinal);
        }

        return lhs is IComparable comparableLhs && rhs is IComparable comparableRhs
            ? (comparableLhs.GetType() == comparableRhs.GetType() && comparableLhs.CompareTo(comparableRhs) == 0)
            : lhs.Equals(rhs);
    }

    private object GetLhsValue(FixFieldValueProvider additionalValues)
    {
        if (Field.StartsWith("FIX_", StringComparison.Ordinal))
        {
            // When the opposite operand is a parameter, convert the FIX field's string to the
            // parameter's native type — the same conversion the literal path applies — rather than
            // facing the parameter with a raw string it can never equal (R02).
            return _field2Source is IParameter parameter
                ? ConvertFixFieldForParameter(additionalValues, Field, parameter, Field2Value)
                : GetFixFieldValue(additionalValues, Field);
        }

        // A Boolean parameter can retain true/false while its declared wire mapping suppresses the tag.
        // Only a literal wire NULL comparison observes suppression; field2 and EX retain native semantics.
        if (
            Value == Atdl.NullValue
            && Operator is Operator_t.Equal or Operator_t.NotEqual
            && _fieldSource is Parameter_t<Boolean_t> { WireValue: null }
        )
        {
            return null!;
        }

        // Parameters already supply their declared native type. In particular, String_t "01"
        // must not become the number 1. Text controls retain their numeric-entry conversion.
        return GetComparisonValue(_fieldSource, FieldValue, Value);
    }

    private object GetRhsValue(FixFieldValueProvider additionalValues, object lhs)
    {
        if (Value != null)
        {
            // "{NULL}" is a sentinel, not a real value of lhs's type — routing it through numeric/
            // enum conversion would throw or silently coerce it. Short-circuit so it flows into
            // AreEqual as the literal sentinel string.
            if (Value == Atdl.NullValue)
            {
                return Value;
            }

            // StrategyEdit literals use the parameter's wire representation, including the
            // Boolean_t trueWireValue/falseWireValue overrides. StateRules still compare bools.
            if (_fieldSource is Parameter_t<Boolean_t> booleanParameter)
            {
                return booleanParameter.Value.ParseWireValue(Value)!;
            }

            // A string LHS (text control) compares against the literal as text: GetComparisonValue
            // keeps both sides strings unless BOTH parse as decimal, so a numeric entry facing a
            // non-numeric literal evaluates instead of throwing (R03).
            return lhs is string ? Value : EditValueConverter.ConvertToComparableType(lhs, Value);
        }

        if (Field2 != null)
        {
            if (Field2.StartsWith("FIX_", StringComparison.Ordinal))
            {
                return _fieldSource is IParameter parameter
                    ? ConvertFixFieldForParameter(additionalValues, Field2, parameter, lhs)
                    : GetFixFieldValue(additionalValues, Field2);
            }

            return GetComparisonValue(_field2Source, Field2Value);
        }

        return null!;
    }

    // R02: a FIX_ operand facing an IParameter must be converted to the parameter's native type, exactly
    // as the literal path is via ConvertToComparableType. Previously only string parameters ever matched
    // the raw FIX string, so char/bool/date-time/MonthYear/Tenor/ISO-enum parameters silently mis-compared
    // (EQ always false, NE always true, inequalities throwing on the type mismatch). A missing FIX field
    // stays null so EX/NX and null comparisons keep their meaning. Boolean parameters go through their
    // declared wire mapping first (custom true/false tokens such as 1/0), falling back to the generic
    // conversion when the field is not one of those tokens.
    private static object ConvertFixFieldForParameter(
        FixFieldValueProvider additionalValues,
        string fixField,
        IParameter parameter,
        object parameterValue
    )
    {
        if (!additionalValues.TryGetValue(fixField, out string? fixString) || fixString == null)
        {
            return null!;
        }

        if (parameter is Parameter_t<Boolean_t> booleanParameter)
        {
            try
            {
                return booleanParameter.Value.ParseWireValue(fixString)!;
            }
            catch (InvalidFieldValueException)
            {
                // Not one of the declared boolean tokens — compare via the generic conversion below.
            }
        }

        return EditValueConverter.ConvertToComparableType(parameterValue, fixString);
    }

    private static object GetComparisonValue(T source, object value, string? literal = null)
    {
        if (source is BinaryControlBase { HasEnumeratedState: true } binary && value is bool selected)
        {
            return selected ? binary.CheckedEnumRef : binary.UncheckedEnumRef;
        }

        // R03: a text control's comparison type is data-dependent. Take the decimal path only when BOTH
        // sides of a literal comparison parse as decimal; facing a non-numeric literal, keep the text as
        // a string so the comparison evaluates (ordinally) instead of throwing from ConvertToComparableType.
        if (
            literal != null
            && value is string text
            && decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out _)
            && !decimal.TryParse(literal, NumberStyles.Number, CultureInfo.InvariantCulture, out _)
        )
        {
            return text;
        }

        return isPartOfStrategyEdit ? value : NormaliseNumericString(value);
    }

    // If the field value is a string that parses as a decimal, surface it as a decimal so that comparisons
    // between LHS and RHS use compatible runtime types. Without this, a non-FIX_ Field2 returning a string
    // would hit IComparable.CompareTo(object) against a decimalised LHS and throw ArgumentException.
    private static object NormaliseNumericString(object fieldValue)
    {
        if (fieldValue is not string value)
        {
            return fieldValue;
        }

        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal number)
            ? number
            : value;
    }

    private static bool IsNumericType(Type type)
    {
        return Type.GetTypeCode(type) switch
        {
            TypeCode.Byte
            or TypeCode.SByte
            or TypeCode.UInt16
            or TypeCode.Int16
            or TypeCode.UInt32
            or TypeCode.Int32
            or TypeCode.UInt64
            or TypeCode.Int64
            or TypeCode.Decimal
            or TypeCode.Double
            or TypeCode.Single => true,
            _ => false,
        };
    }

    private static object? NormaliseValue(object? val, object? other)
    {
        if (val == null)
        {
            return null;
        }

        if (val is double d && (double.IsNaN(d) || double.IsInfinity(d)))
        {
            throw ThrowHelper.New<InvalidOperationException>(null, "Cannot compare NaN or Infinity values.");
        }

        if (val is float f && (float.IsNaN(f) || float.IsInfinity(f)))
        {
            throw ThrowHelper.New<InvalidOperationException>(null, "Cannot compare NaN or Infinity values.");
        }

        if (IsNumericType(val.GetType()))
        {
            // Operands already sharing one numeric runtime type compare natively via IComparable;
            // decimal normalisation exists to give cross-type comparisons a common type. A
            // host-registered double-backed CustomParameterType can hold a value outside decimal's
            // range, where Convert.ToDecimal overflows and would abort the whole evaluation.
            if (other != null && other.GetType() == val.GetType())
            {
                return val;
            }

            try
            {
                return Convert.ToDecimal(val, CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
            {
                throw ThrowHelper.New<InvalidOperationException>(
                    null,
                    ex,
                    "Numeric conversion failed for comparison value: {0}",
                    val
                );
            }
        }

        return val;
    }

    private void CheckForUnsupportedComparisons(object lhs, object rhs)
    {
        // We don't currently support comparisons for type 'Data_t' which is represented by a char[].
        if (lhs is char[] chars)
        {
            throw ThrowHelper.New<InvalidOperationException>(
                this,
                ErrorMessages.UnsupportedComparisonOperation,
                Value,
                new string(chars)
            );
        }

        if (rhs is char[] rhs1)
        {
            throw ThrowHelper.New<InvalidOperationException>(
                this,
                ErrorMessages.UnsupportedComparisonOperation,
                Value,
                new string(rhs1)
            );
        }
    }

    private static object GetFixFieldValue(FixFieldValueProvider additionalValues, string fixField)
    {
        bool gotValue = additionalValues.TryGetValue(fixField, out var value);

        object? result = gotValue switch
        {
            false => null,
            _ => IsNumericFixField(fixField)
            // A String/Char FIX field (e.g. a zero-padded ClOrdID, or a symbol that happens to
            // look numeric) must never be silently decimal-parsed - that loses leading zeros and
            // compares it as a number instead of text. Only convert when the field's actual FIX
            // data type is numeric. NumberStyles is stated explicitly so thousands separators are
            // rejected rather than silently swallowed (R21).
            && decimal.TryParse(
                value,
                NumberStyles.Float | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out decimal number
            )
                ? number
                : value,
        };

        return result!;
    }

    // A FixField enum member absent from FixFieldTypes (e.g. one FIX50SP2-absent tag found during
    // generation) defaults to non-numeric via FixFieldTypes.IsNumeric's own GetValueOrDefault - the
    // safe default, since it never turns a string value into a misleading number. The catch below
    // is unreachable in practice: TryGetValue (called by GetFixFieldValue just before this) already
    // requires fixField to parse as a FixField for gotValue to be true, so this method is never
    // invoked with a name ParseAsEnum would reject. Kept only as defensive robustness against a
    // future caller that bypasses that invariant; it returns the same safe non-numeric default.
    private static bool IsNumericFixField(string fixField)
    {
        try
        {
            return FixFieldTypes.IsNumeric(fixField.ParseAsEnum<FixField>());
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    #region IResolvable<Strategy_t> Members

    /// <summary>
    /// Resolves all interdependencies e.g. edits to edit refs, control values to edits, etc.  Called once
    /// all strategies have been loaded as there may be dependencies on EditRefs at the global level.
    /// </summary>
    void IResolvable<Strategy_t, T>.Resolve(Strategy_t strategy, ISimpleDictionary<T> sourceCollection)
    {
        ValidateInvariants();

        (Edits as IResolvable<Strategy_t, T>).Resolve(strategy, sourceCollection);

        if (!string.IsNullOrEmpty(Field) && !Field.StartsWith("FIX_", StringComparison.Ordinal))
        {
            _fieldSource = ResolveField(Field, nameof(Field), sourceCollection);
        }

        if (!string.IsNullOrEmpty(Field2) && !Field2.StartsWith("FIX_", StringComparison.Ordinal))
        {
            _field2Source = ResolveField(Field2, nameof(Field2), sourceCollection);
        }
    }

    private void ValidateInvariants()
    {
        string editId = string.IsNullOrEmpty(Id) ? "(unnamed)" : Id;

        // Fail at load, not at first evaluation: an Edit with neither operator nor logicOperator
        // can never evaluate (R17).
        if (Operator == null && LogicOperator == null)
        {
            throw ThrowHelper.New<InconsistentStrategyException>(
                this,
                "An Edit (Id '{0}') has neither 'operator' nor 'logicOperator' specified.",
                editId
            );
        }

        if (Value != null && Field2 != null)
        {
            throw ThrowHelper.New<InconsistentStrategyException>(this, ErrorMessages.EditValueAndField2BothSet, editId);
        }

        if (Operator != null && string.IsNullOrEmpty(Field))
        {
            throw ThrowHelper.New<InconsistentStrategyException>(
                this,
                "An Edit (Id '{0}') has an operator specified but is missing the 'field' attribute.",
                editId
            );
        }

        if (
            Operator is not null and not Operator_t.Exist and not Operator_t.NotExist
            && Value == null
            && Field2 == null
        )
        {
            throw ThrowHelper.New<InconsistentStrategyException>(
                this,
                "An Edit (Id '{0}') has a comparison operator but neither 'value' nor 'field2'.",
                editId
            );
        }

        if (Operator != null && (LogicOperator != null || Edits.Count > 0))
        {
            throw ThrowHelper.New<InconsistentStrategyException>(
                this,
                "An Edit (Id '{0}') has both comparison operator and child edits/logicOperator configured.",
                editId
            );
        }
    }

    private T ResolveField(string fieldName, string propertyName, ISimpleDictionary<T> sourceCollection)
    {
        return sourceCollection.Contains(fieldName)
            ? sourceCollection[fieldName]
            : throw ThrowHelper.New<ReferencedObjectNotFoundException>(
                this,
                ErrorMessages.EditRefFieldControlNotFound,
                fieldName,
                propertyName
            );
    }

    #endregion IResolvable<Strategy_t> Members
}
