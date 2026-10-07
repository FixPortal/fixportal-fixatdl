// FP Enhancement: 2026-10-07 — match core decimal, date, text, and boolean compares.
using System.Globalization;
using System.Text.Json;
using FixPortal.FixAtdl.Contracts;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Types;
using FixPortal.FixAtdl.Model.Types.Support;

namespace FixPortal.FixAtdl.Contracts.StateRules;

/// <summary>
/// Evaluates a <see cref="StateRuleAstNodeDto"/> tree against a snapshot of form-field values.
/// Designed to be stateless and reusable across multiple evaluations; one instance per
/// request is fine, but a singleton is equally safe.
/// </summary>
/// <remarks>
/// Missing fields represent unset values. Empty strings and empty selections do not exist.
/// Equality recognises {NULL} and selected enum ID membership. ComparisonType preserves parameter and clock semantics.
/// </remarks>
public sealed class StateRuleEvaluator
{
    // Guards against unbounded recursion on a maliciously or accidentally deeply-nested AST
    // (uncaught stack overflow crashes the process and cannot be caught by a try/catch).
    private const int MaxAstDepth = 64;

    // Same alphabet as Atdl.FixDecimalStyles, which is internal to FixPortal.FixAtdl.
    private const NumberStyles FixDecimalStyles =
        NumberStyles.AllowLeadingWhite
        | NumberStyles.AllowTrailingWhite
        | NumberStyles.AllowLeadingSign
        | NumberStyles.AllowDecimalPoint;

    /// <summary>
    /// Evaluates the AST against the given form state.
    /// </summary>
    /// <param name="ast">The root node of the state-rule condition tree.</param>
    /// <param name="formState">
    /// A mapping from field/control identifiers to their current values.
    /// Values may be <c>null</c>, <c>string</c>, <c>double</c>, <c>bool</c>, or any other
    /// JSON-primitive type; the evaluator coerces as needed.
    /// </param>
    /// <returns><c>true</c> if the state rule condition is satisfied; <c>false</c> otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ast"/> is <c>null</c>.</exception>
    /// <exception cref="AtdlParseException">
    /// Thrown with code <see cref="AtdlParseExceptionCode.UnknownStateRuleKind"/> when
    /// <paramref name="ast"/> carries an unrecognised <c>Kind</c>.
    /// </exception>
    public bool Evaluate(StateRuleAstNodeDto ast, IReadOnlyDictionary<string, object?> formState) =>
        Evaluate(ast, formState, 0);

    private bool Evaluate(StateRuleAstNodeDto ast, IReadOnlyDictionary<string, object?> formState, int depth)
    {
        ArgumentNullException.ThrowIfNull(ast);
        ArgumentNullException.ThrowIfNull(formState);

        if (depth > MaxAstDepth)
        {
            throw new AtdlParseException(
                AtdlParseExceptionCode.MaxDepthExceeded,
                $"StateRule AST nesting exceeds the maximum supported depth of {MaxAstDepth}."
            );
        }

        return ast.Kind switch
        {
            StateRuleAstKind.Compare => EvaluateCompare(ast, formState),
            StateRuleAstKind.And => EvaluateAnd(ast, formState, depth),
            StateRuleAstKind.Or => EvaluateOr(ast, formState, depth),
            StateRuleAstKind.Not => EvaluateNot(ast, formState, depth),
            StateRuleAstKind.Xor => EvaluateXor(ast, formState, depth),
            _ => throw new AtdlParseException(
                AtdlParseExceptionCode.UnknownStateRuleKind,
                $"Unrecognised StateRule AST kind '{ast.Kind}'."
            ),
        };
    }

    // -------------------------------------------------------------------------
    // Combinators
    // -------------------------------------------------------------------------

    // All() on an empty sequence returns true (vacuous truth). An empty conjunction is satisfied.
    private bool EvaluateAnd(StateRuleAstNodeDto node, IReadOnlyDictionary<string, object?> state, int depth) =>
        (node.Children ?? []).All(c => Evaluate(c, state, depth + 1));

    // Any() on an empty sequence returns false.
    private bool EvaluateOr(StateRuleAstNodeDto node, IReadOnlyDictionary<string, object?> state, int depth) =>
        (node.Children ?? []).Any(c => Evaluate(c, state, depth + 1));

    private bool EvaluateNot(StateRuleAstNodeDto node, IReadOnlyDictionary<string, object?> state, int depth)
    {
        var children = node.Children;
        if (children is null || children.Count != 1)
        {
            throw new ArgumentException(
                $"NOT node must have exactly one child; found {children?.Count ?? 0}.",
                nameof(node)
            );
        }

        return !Evaluate(children[0], state, depth + 1);
    }

    // FIXatdl defines XOR as exactly one true operand, evaluating every child.
    private bool EvaluateXor(StateRuleAstNodeDto node, IReadOnlyDictionary<string, object?> state, int depth) =>
        (node.Children ?? []).Count(c => Evaluate(c, state, depth + 1)) == 1;

    // -------------------------------------------------------------------------
    // Compare leaf
    // -------------------------------------------------------------------------

    private static bool EvaluateCompare(StateRuleAstNodeDto node, IReadOnlyDictionary<string, object?> state)
    {
        var field = node.Field ?? string.Empty;
        var @operator = node.Operator ?? string.Empty;

        state.TryGetValue(field, out var rawFieldValue);
        var fieldValue = Unwrap(rawFieldValue);
        if (@operator == StateRuleOperator.Exists)
        {
            return !IsEmpty(fieldValue);
        }
        if (@operator == StateRuleOperator.NotExists)
        {
            return IsEmpty(fieldValue);
        }

        object? astValue = node.Value;
        if (!string.IsNullOrEmpty(node.Field2))
        {
            state.TryGetValue(node.Field2, out astValue);
        }
        astValue = Unwrap(astValue);

        if (node.ComparisonType is "Data_t" or "data_t")
        {
            throw new AtdlParseException(
                AtdlParseExceptionCode.InvalidEditValue,
                "Data_t values do not support Edit comparisons."
            );
        }

        try
        {
            return @operator switch
            {
                StateRuleOperator.Eq => CompareEqual(fieldValue, astValue, node),
                StateRuleOperator.Neq => !CompareEqual(fieldValue, astValue, node),
                StateRuleOperator.Gt => CompareOrder(fieldValue, astValue, node.ComparisonType) is > 0,
                StateRuleOperator.Lt => CompareOrder(fieldValue, astValue, node.ComparisonType) is < 0,
                StateRuleOperator.Ge => CompareOrder(fieldValue, astValue, node.ComparisonType) is >= 0,
                StateRuleOperator.Le => CompareOrder(fieldValue, astValue, node.ComparisonType) is <= 0,
                _ => throw new AtdlParseException(
                    AtdlParseExceptionCode.UnknownStateRuleOperator,
                    $"Unrecognised StateRule operator '{@operator}'."
                ),
            };
        }
        catch (AtdlParseException)
        {
            throw;
        }
        catch (Exception ex)
            when (ex
                    is ArgumentException
                        or InvalidOperationException
                        or FormatException
                        or FixPortal.FixAtdl.Diagnostics.Exceptions.InvalidFieldValueException
            )
        {
            throw new AtdlParseException(
                AtdlParseExceptionCode.InvalidEditValue,
                $"Invalid value for StateRule comparison type '{node.ComparisonType}'.",
                ex
            );
        }
    }

    // -------------------------------------------------------------------------
    // Coercion helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Equality with numeric-first coercion.  If both sides can be parsed as decimals,
    /// compares numerically (so "5" == 5.0 is true).  Falls back to ordinal string
    /// comparison when either side cannot be converted.
    /// </summary>
    private static bool CompareEqual(object? a, object? b, StateRuleAstNodeDto node)
    {
        var comparisonType = node.ComparisonType;
        a = Unwrap(a);
        b = Unwrap(b);
        if (a is null || b is null)
        {
            return a is null && b is null;
        }
        if (TryCompareSelections(a, b, out var selectionEqual))
        {
            return selectionEqual;
        }
        if (TryEqualConfiguredBoolean(a, b, node, out var configuredEqual))
        {
            return configuredEqual;
        }
        if (TryCompareBoolText(a, b, out var boolEqual))
        {
            return boolEqual;
        }
        if (comparisonType is "Tenor_t" or "MonthYear_t")
        {
            return CompareDomain(a, b, comparisonType) == 0;
        }
        if (IsTemporalComparison(comparisonType) || comparisonType is "TZTimeOnly_t" or "TZTimestamp_t")
        {
            return CompareOrder(a, b, comparisonType) == 0;
        }
        if (IsTextComparison(comparisonType))
        {
            return string.Equals(UnwrapToString(a), UnwrapToString(b), StringComparison.Ordinal);
        }
        if (TryToDecimal(a, out var da) && TryToDecimal(b, out var db))
        {
            return da == db;
        }

        // At least one side is not numeric — compare as strings.
        // Unwrap JsonElement before calling ToString() so we get the actual string
        // value rather than the JSON representation (which would include quotes).
        return string.Equals(UnwrapToString(a), UnwrapToString(b), StringComparison.Ordinal);
    }

    private static bool TryCompareSelections(object a, object b, out bool equal)
    {
        if (a is object?[] left)
        {
            equal = b is object?[] right ? left.ToHashSet().SetEquals(right) : left.Contains(b);
            return true;
        }
        if (b is object?[] selections)
        {
            equal = selections.Contains(a);
            return true;
        }
        equal = false;
        return false;
    }

    private static bool TryEqualConfiguredBoolean(object a, object b, StateRuleAstNodeDto node, out bool equal)
    {
        if (node.ComparisonType != "Boolean_t" || node.TrueWireValue is null && node.FalseWireValue is null)
        {
            equal = false;
            return false;
        }

        equal = EqualBooleanWire(a, b, node);
        return true;
    }

    private static bool EqualBooleanWire(object a, object b, StateRuleAstNodeDto node)
    {
        if (!TryBooleanWire(a, node, out var left) || !TryBooleanWire(b, node, out var right))
        {
            throw new AtdlParseException(
                AtdlParseExceptionCode.InvalidEditValue,
                "Boolean Edit value does not match the parameter wire values."
            );
        }

        return left == right;
    }

    private static bool TryBooleanWire(object value, StateRuleAstNodeDto node, out bool result)
    {
        if (value is bool flag)
        {
            result = flag;
            return true;
        }

        if (value is string text)
        {
            if (text == node.TrueWireValue)
            {
                result = true;
                return true;
            }

            if (text == node.FalseWireValue)
            {
                result = false;
                return true;
            }
        }

        result = false;
        return false;
    }

    private static bool TryCompareBoolText(object a, object b, out bool equal)
    {
        if (a is bool && b is string || b is bool && a is string)
        {
            var flag = a is bool leftFlag ? leftFlag : (bool)b;
            var text = a is string leftText ? leftText : (string)b;
            equal = text.ToUpperInvariant() switch
            {
                "Y" or "TRUE" => flag,
                "N" or "FALSE" => !flag,
                _ => false,
            };
            return true;
        }
        equal = false;
        return false;
    }

    private static bool IsTextComparison(string? type) =>
        type
            is "String_t"
                or "Char_t"
                or "MultipleStringValue_t"
                or "MultipleCharValue_t"
                or "Data_t"
                or "Boolean_t"
                or "EnumState";

    private static int? CompareOrder(object? a, object? b, string? comparisonType)
    {
        if (a is null || b is null)
        {
            return null;
        }
        if (comparisonType is "Tenor_t" or "MonthYear_t")
        {
            return CompareDomain(a, b, comparisonType);
        }
        if (comparisonType is "TZTimeOnly_t" or "TZTimestamp_t")
        {
            return ParseZonedTime(a, comparisonType).CompareTo(ParseZonedTime(b, comparisonType));
        }
        if (comparisonType is "Clock_t")
        {
            return CompareTemporalOrder(a, b);
        }
        if (IsStrictTemporal(comparisonType))
        {
            return ParseWireDate(a, comparisonType!).CompareTo(ParseWireDate(b, comparisonType!));
        }
        if (IsTextComparison(comparisonType))
        {
            if (a is object?[] || b is object?[])
            {
                return null;
            }

            return string.Compare(UnwrapToString(a), UnwrapToString(b), StringComparison.Ordinal);
        }
        return CompareNumericOrder(a, b);
    }

    private static bool IsStrictTemporal(string? type) =>
        type is "UTCTimeOnly_t" or "UTCTimestamp_t" or "UTCDateOnly_t" or "LocalMktDate_t";

    private static DateTime ParseWireDate(object value, string type)
    {
        var text = UnwrapToString(value) ?? string.Empty;
        return type switch
        {
            "UTCDateOnly_t" => ParseWireDate<UTCDateOnly_t>(text),
            "LocalMktDate_t" => ParseWireDate<LocalMktDate_t>(text),
            "UTCTimeOnly_t" => ParseWireDate<UTCTimeOnly_t>(text),
            "UTCTimestamp_t" => ParseWireDate<UTCTimestamp_t>(text),
            _ => throw new AtdlParseException(
                AtdlParseExceptionCode.InvalidEditValue,
                $"Invalid value for StateRule comparison type '{type}'."
            ),
        };
    }

    private static DateTime ParseWireDate<T>(string text)
        where T : IParameterType, new()
    {
        var parameter = new Parameter_t<T>("Edit") { WireValue = text };
        return (DateTime)parameter.GetCurrentValue();
    }

    private static int? CompareTemporalOrder(object a, object b)
    {
        if (!TryDateTime(a, out var leftTime) || !TryDateTime(b, out var rightTime))
        {
            return null;
        }
        if (leftTime.Year == 1 && rightTime.Year != 1)
        {
            leftTime = rightTime.Date.Add(leftTime.TimeOfDay);
        }
        else if (rightTime.Year == 1 && leftTime.Year != 1)
        {
            rightTime = leftTime.Date.Add(rightTime.TimeOfDay);
        }
        return leftTime.CompareTo(rightTime);
    }

    private static int? CompareNumericOrder(object a, object b)
    {
        var leftNumeric = TryToDecimal(a, out var left);
        var rightNumeric = TryToDecimal(b, out var right);
        if (leftNumeric && rightNumeric)
        {
            return left.CompareTo(right);
        }
        if (a is string leftText && b is string rightText)
        {
            return string.Compare(leftText, rightText, StringComparison.Ordinal);
        }

        return null;
    }

    // WireValue is annotated non-nullable from FixPortal.FixAtdl 1.1.5 onward. CompareOrder
    // has already rejected a null value before it reaches here, so the unwrap cannot be null -
    // the same reasoning CompareDomain below already relies on.
    private static DateTime ParseZonedTime(object value, string type)
    {
        if (type == "TZTimeOnly_t")
        {
            var parameter = new Parameter_t<TZTimeOnly_t>("Edit") { WireValue = UnwrapToString(value)! };
            return (DateTime)parameter.GetCurrentValue();
        }
        var timestamp = new Parameter_t<TZTimestamp_t>("Edit") { WireValue = UnwrapToString(value)! };
        return (DateTime)timestamp.GetCurrentValue();
    }

    private static int CompareDomain(object a, object b, string type) =>
        type == "Tenor_t"
            ? Tenor.Parse(UnwrapToString(a)!).CompareTo(Tenor.Parse(UnwrapToString(b)!))
            : MonthYear.Parse(UnwrapToString(a)!).CompareTo(MonthYear.Parse(UnwrapToString(b)!));

    private static bool IsTemporalComparison(string? type) =>
        type is "Clock_t" or "UTCTimeOnly_t" or "UTCTimestamp_t" or "UTCDateOnly_t" or "LocalMktDate_t";

    private static bool TryDateTime(object value, out DateTime result) =>
        DateTime.TryParseExact(
            UnwrapToString(value),
            [
                "HH:mm",
                "HH:mm:ss",
                "HH:mm:ss.FFFFFFF",
                "yyyyMMdd-HH:mm:ss",
                "yyyyMMdd-HH:mm:ss.FFFFFFF",
                "yyyyMMdd",
                "yyyy-MM-dd",
                "yyyy-MM-ddTHH:mm:ss",
                "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
            ],
            CultureInfo.InvariantCulture,
            DateTimeStyles.NoCurrentDateDefault,
            out result
        );

    // GetString() throws on a non-string JsonElement (e.g. a numeric literal), so only
    // call it for the String kind; for every other kind use the raw text ("0", "true",
    // "null"). WHY: an == comparison between a non-numeric field (e.g. an empty string)
    // and a numeric AST literal falls back to string comparison, and must not crash —
    // it should compare "" vs "0" and return false, matching the TS evaluator.
    private static string? UnwrapToString(object? value)
    {
        // CLR bool.ToString() yields "True"/"False", which never equals a FIXatdl "true"/"false"
        // literal under ordinal comparison — lowercase it so bool form values compare correctly.
        if (value is bool b)
        {
            return b ? "true" : "false";
        }

        if (value is not JsonElement el)
        {
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        return el.ValueKind == JsonValueKind.String ? el.GetString() : el.GetRawText();
    }

    private static bool IsEmpty(object? value) => value is null or "" or object?[] { Length: 0 };

    private static object? Unwrap(object? value) =>
        value switch
        {
            "{NULL}" => null,
            JsonElement element => element.ValueKind switch
            {
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                JsonValueKind.String => Unwrap(element.GetString()),
                JsonValueKind.Number => element.TryGetDecimal(out var number) ? number : element.GetRawText(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Array => element.EnumerateArray().Select(item => Unwrap(item)).ToArray(),
                _ => element,
            },
            object?[] { Length: 0 } => null,
            _ => value,
        };

    private static bool TryToDecimal(object? value, out decimal result)
    {
        value = Unwrap(value);
        if (value is null or bool or object?[])
        {
            result = 0;
            return false;
        }

        try
        {
            // InvariantCulture is required so that decimal points in string literals
            // ("3.14") parse correctly regardless of the runner's system locale.
            // Without it, German/French locales treat '.' as a thousands separator
            // and throw, causing the catch to silently return false.
            if (value is string text)
            {
                return decimal.TryParse(text, FixDecimalStyles, CultureInfo.InvariantCulture, out result);
            }

            result = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            result = 0;
            return false;
        }
    }
}
