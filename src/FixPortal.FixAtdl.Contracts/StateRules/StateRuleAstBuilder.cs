using FixPortal.FixAtdl.Contracts;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Enumerations;
using IValueProvider = FixPortal.FixAtdl.Validation.IValueProvider;

namespace FixPortal.FixAtdl.Contracts.StateRules;

/// <summary>
/// Converts the unresolved <see cref="Edit_t"/> tree read from a FIXatdl document into a
/// <see cref="StateRuleAstNodeDto"/> — a serialisable, evaluator-ready AST — without
/// requiring control-binding resolution (which is a runtime concern, not a parse-time one).
/// </summary>
/// <remarks>
/// The builder accepts two optional <see cref="EditCollection"/> registries (global-level and
/// strategy-level) so it can resolve <c>EditRef</c> references by ID.  If an EditRef ID
/// cannot be found in either registry, an <see cref="AtdlParseException"/> is thrown.
/// </remarks>
public sealed class StateRuleAstBuilder
{
    // Guards against unbounded recursion on maliciously or accidentally deeply-nested Edit
    // trees (uncaught stack overflow crashes the process and cannot be caught by a try/catch).
    private const int MaxEditDepth = 64;

    private readonly EditCollection? _globalEdits;
    private readonly EditCollection? _strategyEdits;
    private readonly IReadOnlyDictionary<string, string?>? _comparisonTypes;

    /// <param name="globalEdits">
    /// The <see cref="Strategies_t.Edits"/> collection, or <c>null</c> if none are defined.
    /// </param>
    /// <param name="strategyEdits">
    /// The <see cref="Strategy_t.Edits"/> collection for the containing strategy, or <c>null</c>
    /// if none are defined.
    /// </param>
    /// <param name="comparisonTypes">Optional control operand types, preserving enum and clock comparisons.</param>
    public StateRuleAstBuilder(
        EditCollection? globalEdits = null,
        EditCollection? strategyEdits = null,
        IReadOnlyDictionary<string, string?>? comparisonTypes = null
    )
    {
        _globalEdits = globalEdits;
        _strategyEdits = strategyEdits;
        _comparisonTypes = comparisonTypes;
    }

    /// <summary>
    /// Builds an AST from an <see cref="Edit_t"/> root node (strategy/global-level non-generic form).
    /// </summary>
    /// <param name="edit">The root Edit_t as it appears in the parsed FIXatdl model.</param>
    /// <returns>The equivalent <see cref="StateRuleAstNodeDto"/>.</returns>
    /// <exception cref="AtdlParseException">
    /// Thrown when an EditRef cannot be resolved or an unrecognised operator is encountered.
    /// </exception>
    public StateRuleAstNodeDto Build(Edit_t edit) => BuildFromEdit(edit, 0);

    /// <summary>
    /// Builds an AST from a bound <see cref="Edit_t{T}"/> root node (the generic form held by
    /// <see cref="FixPortal.FixAtdl.Model.Elements.StateRule_t"/> after resolution).
    /// </summary>
    /// <typeparam name="T">The value-provider type bound to the edit (typically <see cref="Control_t"/>).</typeparam>
    /// <param name="edit">The root Edit_t&lt;T&gt; as it appears on a resolved StateRule.</param>
    /// <returns>The equivalent <see cref="StateRuleAstNodeDto"/>.</returns>
    /// <exception cref="AtdlParseException">
    /// Thrown when an unrecognised operator is encountered.
    /// </exception>
    public StateRuleAstNodeDto Build<T>(Edit_t<T> edit)
        where T : class, IValueProvider => BuildFromBoundEdit(edit, 0);

    private StateRuleAstNodeDto BuildFromEdit(Edit_t edit, int depth)
    {
        if (depth > MaxEditDepth)
        {
            throw new AtdlParseException(
                AtdlParseExceptionCode.MaxDepthExceeded,
                $"StateRule Edit nesting exceeds the maximum supported depth of {MaxEditDepth}."
            );
        }

        return BuildCore(
            edit.Operator,
            edit.LogicOperator,
            edit.Field,
            edit.Field2,
            edit.Value,
            () => BuildChildren(edit.Edits, depth + 1)
        );
    }

    private List<StateRuleAstNodeDto> BuildChildren(EditCollection edits, int depth)
    {
        // An EditCollection is a KeyedCollection<string, Edit_t> and also implements
        // IEnumerable<Edit_t> through its base Collection<Edit_t>.
        var result = new List<StateRuleAstNodeDto>(edits.Count);
        var index = 0;
        foreach (Edit_t child in edits)
        {
            try
            {
                result.Add(BuildFromEdit(child, depth));
            }
            catch (AtdlParseException ex)
            {
                var ctx = string.IsNullOrWhiteSpace(child.Field)
                    ? $"index {index}"
                    : $"index {index}, field '{child.Field}'";
                throw new AtdlParseException(ex.Code, $"Failed to build child edit ({ctx}): {ex.Message}", ex);
            }
            index++;
        }
        return result;
    }

    /// <summary>
    /// Builds an AST by resolving an EditRef ID against the registered collections.
    /// </summary>
    /// <param name="editRefId">
    /// The value of the <c>id</c> attribute on a FIXatdl EditRef element.
    /// </param>
    /// <exception cref="AtdlParseException">Thrown when the ID is not found in either registry.</exception>
    public StateRuleAstNodeDto BuildFromRef(string editRefId) => BuildFromRef(editRefId, 0);

    private StateRuleAstNodeDto BuildFromRef(string editRefId, int depth)
    {
        // Strategy-level Edits take precedence over global Edits, mirroring FixAtdl's own
        // resolution order in EditRef_t<T>.Resolve.
        Edit_t? resolved = null;

        if (_strategyEdits is not null && _strategyEdits.Contains(editRefId))
        {
            resolved = _strategyEdits[editRefId];
        }
        else if (_globalEdits is not null && _globalEdits.Contains(editRefId))
        {
            resolved = _globalEdits[editRefId];
        }

        if (resolved is null)
        {
            throw new AtdlParseException(
                AtdlParseExceptionCode.UnresolvedEditRef,
                $"EditRef '{editRefId}' could not be resolved against the strategy-level or global Edit registries."
            );
        }

        return BuildFromEdit(resolved, depth + 1);
    }

    // Mirrors BuildFromEdit but for the bound generic Edit_t<T>, which has the same logical
    // structure (Field, Operator, Value, LogicOperator, Edits) but a different collection type.
    private StateRuleAstNodeDto BuildFromBoundEdit<T>(Edit_t<T> edit, int depth)
        where T : class, IValueProvider
    {
        if (depth > MaxEditDepth)
        {
            throw new AtdlParseException(
                AtdlParseExceptionCode.MaxDepthExceeded,
                $"StateRule Edit nesting exceeds the maximum supported depth of {MaxEditDepth}."
            );
        }

        return BuildCore(
            edit.Operator,
            edit.LogicOperator,
            edit.Field,
            edit.Field2,
            edit.Value,
            () => BuildBoundChildren(edit.Edits, depth + 1)
        );
    }

    private List<StateRuleAstNodeDto> BuildBoundChildren<T>(EditEvaluatingCollection<T> edits, int depth)
        where T : class, IValueProvider
    {
        var result = new List<StateRuleAstNodeDto>(edits.Count);
        var index = 0;
#pragma warning disable S3267 // Type-dispatch with exhaustive else-throw; not a filter
        foreach (var child in edits)
        {
            try
            {
                if (child is Edit_t<T> concreteEdit)
                {
                    result.Add(BuildFromBoundEdit(concreteEdit, depth));
                }
                else if (child is EditRef_t<T> editRef)
                {
                    // EditRef_t<T>.ReferencedEdit is internal; resolve by ID through the builder's registries.
                    result.Add(BuildFromRef(editRef.Id, depth));
                }
                else
                {
                    var typeName = child?.GetType().FullName ?? "<null>";
                    throw new AtdlParseException(
                        AtdlParseExceptionCode.UnknownStateRuleOperator,
                        $"A child of a compound StateRule Edit has unexpected type '{typeName}'; expected Edit_t<T> or EditRef_t<T>."
                    );
                }
            }
            catch (AtdlParseException ex)
            {
                var childContext = child switch
                {
                    EditRef_t<T> r => $"EditRef '{r.Id}'",
                    Edit_t<T> => "concrete Edit",
                    null => "null child",
                    _ => $"type '{child.GetType().Name}'",
                };
                throw new AtdlParseException(
                    ex.Code,
                    $"Failed to build bound child at index {index} ({childContext}): {ex.Message}",
                    ex
                );
            }
            index++;
        }
#pragma warning restore S3267
        return result;
    }

    private StateRuleAstNodeDto BuildCore(
        Operator_t? opToken,
        LogicOperator_t? logicToken,
        string field,
        string? field2,
        string? rawValue,
        Func<IReadOnlyList<StateRuleAstNodeDto>> buildChildren
    )
    {
        // Leaf compare node: Operator is set, LogicOperator is absent.
        if (opToken is not null)
        {
            return BuildCompare(opToken.Value, field, field2, rawValue);
        }

        // Compound node: LogicOperator combines one or more child Edits.
        if (logicToken is not null)
        {
            // Children must be built to know the operand count; the NOT-arity check below
            // therefore runs post-build and reports the exact count in its error message.
            return BuildLogic(logicToken.Value, buildChildren());
        }

        throw new AtdlParseException(
            AtdlParseExceptionCode.UnknownStateRuleOperator,
            "A StateRule Edit has neither an Operator nor a LogicOperator; the FIXatdl document is malformed."
        );
    }

    private StateRuleAstNodeDto BuildCompare(Operator_t opToken, string field, string? field2, string? rawValue)
    {
        var op = MapOperator(opToken);
        if (
            op is not (StateRuleOperator.Exists or StateRuleOperator.NotExists)
            && string.IsNullOrEmpty(field2)
            && rawValue is null
        )
        {
            throw new AtdlParseException(
                AtdlParseExceptionCode.InvalidEditValue,
                $"Operator '{opToken}' requires either a Value or Field2 operand."
            );
        }

        // EX / not-exists carry no value; all others carry the raw FIXatdl Value string.
        object? value = (op == StateRuleOperator.Exists || op == StateRuleOperator.NotExists) ? null : rawValue;
        return StateRuleAst.Compare(field, op, value, string.IsNullOrEmpty(field2) ? null : field2) with
        {
            ComparisonType = _comparisonTypes?.GetValueOrDefault(field),
        };
    }

    private static StateRuleAstNodeDto BuildLogic(
        LogicOperator_t logicToken,
        IReadOnlyList<StateRuleAstNodeDto> children
    )
    {
        return logicToken switch
        {
            LogicOperator_t.And => StateRuleAst.And(children),
            LogicOperator_t.Or => StateRuleAst.Or(children),
            LogicOperator_t.Not => children.Count == 1
                ? StateRuleAst.Not(children[0])
                : throw new AtdlParseException(
                    AtdlParseExceptionCode.UnknownStateRuleOperator,
                    $"NOT operator requires exactly one child operand; found {children.Count}."
                ),
            // XOR is defined in the FIXatdl spec but extremely rare in practice.
            // Preserved as a distinct kind so evaluators can handle it explicitly rather than
            // silently mapping it to an incorrect combinator.
            LogicOperator_t.Xor => new StateRuleAstNodeDto(StateRuleAstKind.Xor, null, null, null, children),
            _ => throw new AtdlParseException(
                AtdlParseExceptionCode.UnknownStateRuleOperator,
                $"Unrecognised LogicOperator value '{logicToken}' in StateRule Edit."
            ),
        };
    }

    private static string MapOperator(Operator_t op) =>
        op switch
        {
            Operator_t.Equal => StateRuleOperator.Eq,
            Operator_t.NotEqual => StateRuleOperator.Neq,
            Operator_t.GreaterThan => StateRuleOperator.Gt,
            Operator_t.LessThan => StateRuleOperator.Lt,
            Operator_t.GreaterThanOrEqual => StateRuleOperator.Ge,
            Operator_t.LessThanOrEqual => StateRuleOperator.Le,
            Operator_t.Exist => StateRuleOperator.Exists,
            Operator_t.NotExist => StateRuleOperator.NotExists,
            _ => throw new AtdlParseException(
                AtdlParseExceptionCode.UnknownStateRuleOperator,
                $"Unrecognised Operator_t value '{op}' ({(int)op}) in StateRule Edit."
            ),
        };
}
