// FP Enhancement: 2026-10-07 — wire constants, named FIX tags, and FIX/boolean edits.
using FixPortal.FixAtdl.Contracts.StateRules;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Controls;
using FixPortal.FixAtdl.Model.Controls.Support;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Elements.Support;

namespace FixPortal.FixAtdl.Contracts;

/// <summary>
/// Converts a parsed <see cref="Strategies_t"/> into the wire contract consumed by
/// @fix-portal/fixatdl-react.
/// </summary>
/// <remarks>
/// The mapper owns the strategy-to-DTO transformation: parameter inlining, panel-tree recursion,
/// and StateRule AST embedding. Schema validation, which this package does not perform, and XML
/// parsing stay with the caller.
///
/// Panel child ordering:
/// <c>StrategyPanel_t</c> holds child panels and controls in two separate collections with no
/// shared ordering index. This mapper emits all child panels first, then all controls within
/// each parent, rather than re-parsing the source XML to recover the original interleaved order.
/// Mixed-order panel/control authoring surfaces in non-author order.
/// </remarks>
public sealed class AtdlDtoMapper
{
    // Guards against unbounded recursion on maliciously or accidentally deeply-nested panels
    // (uncaught stack overflow crashes the process and cannot be caught by a try/catch).
    private const int MaxPanelDepth = 64;

    private static readonly IReadOnlyDictionary<string, string> NoSourceXml = new Dictionary<string, string>();

    /// <summary>
    /// Maps every strategy in a parsed FIXatdl document to the wire contract.
    /// </summary>
    /// <param name="strategies">The document parsed by <see cref="FixPortal.FixAtdl.Xml.StrategiesReader"/>.</param>
    /// <param name="sourceXmlByStrategy">
    /// Optional source XML per strategy name, emitted as each strategy's <c>sourceXml</c>.
    /// A strategy with no entry gets an empty string.
    /// </param>
    /// <returns>The contract, in document order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="strategies"/> is <c>null</c>.</exception>
    /// <exception cref="AtdlParseException">A state rule cannot be built; see <see cref="AtdlParseException.Code"/>.</exception>
    public AtdlStrategiesDto Map(
        Strategies_t strategies,
        IReadOnlyDictionary<string, string>? sourceXmlByStrategy = null
    )
    {
        ArgumentNullException.ThrowIfNull(strategies);

        var sliceByName = sourceXmlByStrategy ?? NoSourceXml;
        var strategyDtos = new List<AtdlStrategyDto>(strategies.Count);
        var globalEdits = strategies.Edits;

        foreach (Strategy_t strategy in strategies)
        {
            strategyDtos.Add(MapStrategy(strategy, sliceByName, globalEdits));
        }

        return new AtdlStrategiesDto(strategyDtos);
    }

    private AtdlStrategyDto MapStrategy(
        Strategy_t strategy,
        IReadOnlyDictionary<string, string> sliceByName,
        EditCollection? globalEdits
    )
    {
        // A per-strategy builder is created with the correct global + strategy-level Edit registries
        // so that EditRef resolution (BuildFromRef) works without relying on the DI singleton's
        // null registries.  EditRef_t<T>.ReferencedEdit is internal in the FixAtdl assembly, so
        // registry-based lookup via the public Id property is the only external access path.
        var comparisonTypes = strategy
            .Controls.GroupBy(control => control.Id)
            .ToDictionary(
                group => group.Key,
                group =>
                    group.Last() switch
                    {
                        Clock_t => "Clock_t",
                        ListControlBase => "EnumState",
                        BinaryControlBase binary when binary.HasEnumeratedState => "EnumState",
                        _ => (string?)null,
                    }
            );
        var builder = new StateRuleAstBuilder(globalEdits, strategy.Edits, comparisonTypes);

        // Map parameters first so we can re-use the same DTO instances when inlining into controls.
        var parameterDtos = MapParameters(strategy);
        // Duplicate parameter names are defensively last-wins rather than throwing —
        // a malformed document should still map, with the validator surfacing the duplicate.
        var paramByName = parameterDtos.GroupBy(p => p.Name).ToDictionary(g => g.Key, g => g.Last());

        var rootPanel = strategy.StrategyLayout?.StrategyPanel;
        var panelDto = rootPanel is not null
            ? MapPanel(rootPanel, paramByName, builder)
            : new AtdlPanelDto(null, "None", "Vertical", false, false, []);

        sliceByName.TryGetValue(strategy.Name, out var sourceXml);

        return new AtdlStrategyDto(
            strategy.Name,
            strategy.Description?.Content,
            parameterDtos,
            panelDto,
            sourceXml ?? string.Empty,
            strategy
                .StrategyEdits.Select(edit => new AtdlStrategyEditDto(
                    edit.ErrorMessage,
                    MapStrategyExpression(ResolveStrategyEditExpression(edit, builder), paramByName)
                ))
                .ToList()
        );
    }

    private static StateRuleAstNodeDto ResolveStrategyEditExpression(StrategyEdit_t edit, StateRuleAstBuilder builder)
    {
        if (edit.Edit is not null)
        {
            return builder.Build(edit.Edit);
        }
        if (edit.EditRef is not null)
        {
            return builder.BuildFromRef(edit.EditRef.Id);
        }
        throw new AtdlParseException(
            AtdlParseExceptionCode.ParseFailed,
            "StrategyEdit requires an Edit or EditRef expression."
        );
    }

    private static StateRuleAstNodeDto MapStrategyExpression(
        StateRuleAstNodeDto node,
        Dictionary<string, AtdlParameterDto> parameters
    )
    {
        var mapped = node with
        {
            Children = node.Children?.Select(child => MapStrategyExpression(child, parameters)).ToList(),
        };
        if (node.Field is null || !parameters.TryGetValue(node.Field, out var parameter))
        {
            return mapped with { ComparisonType = ClassifyFixField(node.Field) };
        }
        mapped = mapped with { ComparisonType = parameter.Type };
        if (parameter.Type == "Boolean_t" && node.Field2 is not null)
        {
            return mapped with
            {
                TrueWireValue = parameter.TrueWireValue ?? "Y",
                FalseWireValue = parameter.FalseWireValue ?? "N",
            };
        }
        if (parameter.Type != "Boolean_t" || node.Value is not string literal)
        {
            return mapped;
        }
        return MapBooleanLiteral(node, mapped, parameter, literal, node.Field);
    }

    private static string? ClassifyFixField(string? field)
    {
        if (field is null || !field.StartsWith("FIX_", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (
            !field.Contains(',')
            && Enum.TryParse(field, ignoreCase: true, out FixField parsed)
            && Enum.IsDefined(parsed)
        )
        {
            return FixFieldTypes.IsNumeric(parsed) ? null : "String_t";
        }

        return "String_t";
    }

    private static StateRuleAstNodeDto MapBooleanLiteral(
        StateRuleAstNodeDto node,
        StateRuleAstNodeDto mapped,
        AtdlParameterDto parameter,
        string literal,
        string field
    )
    {
        if (literal == "{NULL}" && node.Operator is StateRuleOperator.Eq or StateRuleOperator.Neq)
        {
            var omitted = new List<StateRuleAstNodeDto> { StateRuleAst.Compare(field, StateRuleOperator.NotExists) };
            if (parameter.TrueWireValue == "{NULL}")
            {
                omitted.Add(StateRuleAst.Compare(field, StateRuleOperator.Eq, true));
            }
            if (parameter.FalseWireValue == "{NULL}")
            {
                omitted.Add(StateRuleAst.Compare(field, StateRuleOperator.Eq, false));
            }
            var expression = StateRuleAst.Or(omitted);
            return node.Operator == StateRuleOperator.Eq ? expression : StateRuleAst.Not(expression);
        }
        if (MatchesBooleanLiteral(literal, parameter.TrueWireValue, parameter.FalseWireValue, "Y", "TRUE"))
        {
            return mapped with { Value = true };
        }
        if (MatchesBooleanLiteral(literal, parameter.FalseWireValue, parameter.TrueWireValue, "N", "FALSE"))
        {
            return mapped with { Value = false };
        }
        throw new AtdlParseException(
            AtdlParseExceptionCode.InvalidEditValue,
            $"Boolean Edit literal '{literal}' does not match parameter '{parameter.Name}' wire values."
        );
    }

    private static bool MatchesBooleanLiteral(
        string literal,
        string? configured,
        string? overridden,
        params string[] defaults
    ) =>
        configured is not null && string.Equals(literal, configured, StringComparison.Ordinal)
        || defaults.Any(value =>
            !string.Equals(value, overridden, StringComparison.Ordinal)
            && string.Equals(literal, value, StringComparison.Ordinal)
        );

    // ---------------------------------------------------------------------------
    // Parameter mapping
    // ---------------------------------------------------------------------------

    private static List<AtdlParameterDto> MapParameters(Strategy_t strategy)
    {
        var result = new List<AtdlParameterDto>(strategy.Parameters.Count);

        foreach (var param in strategy.Parameters)
        {
            result.Add(MapParameter(param));
        }

        return result;
    }

    private static AtdlParameterDto MapParameter(IParameter param)
    {
        // Parameter_t<T>.Value holds the type constraints; IParameter does not expose them.
        var parameterType = GetProperty(param, "Value");
        var enumValues = param.HasEnumPairs
            ? param.EnumPairs.Select(ep => new AtdlEnumPairDto(ep.EnumId, ep.WireValue)).ToList()
            : null;

        return new AtdlParameterDto(
            Name: param.Name,
            FixTag: param.FixTag.HasValue ? (int?)param.FixTag.Value : null,
            Type: param.Type,
            EnumValues: enumValues,
            Min: MapBound(parameterType, "Min", param.Type),
            Max: MapBound(parameterType, "Max", param.Type),
            Precision: GetProperty(parameterType, "Precision") as int?,
            MutableOnCxlRpl: param.MutableOnCxlRpl ?? true,
            UseValue: param.Use.ToString().ToLowerInvariant(),
            DefaultValue: null, // FIXatdl defaults belong to Control/@initValue, not Parameter.
            TrueWireValue: GetProperty(parameterType, "TrueWireValue") as string,
            FalseWireValue: GetProperty(parameterType, "FalseWireValue") as string,
            InvertOnWire: GetProperty(parameterType, "InvertOnWire") as bool?,
            LocalMktTz: GetProperty(parameterType, "LocalMktTz") as string,
            ConstValue: MapConstValue(param, parameterType),
            MinLength: GetProperty(parameterType, "MinLength") as int?,
            MaxLength: GetProperty(parameterType, "MaxLength") as int?,
            MultiplyBy100: GetProperty(parameterType, "MultiplyBy100") as bool?
        );
    }

    // ---------------------------------------------------------------------------
    // Panel and control mapping
    // ---------------------------------------------------------------------------

    private AtdlPanelDto MapPanel(
        StrategyPanel_t panel,
        Dictionary<string, AtdlParameterDto> paramByName,
        StateRuleAstBuilder builder,
        int depth = 0
    )
    {
        if (depth > MaxPanelDepth)
        {
            throw new AtdlParseException(
                AtdlParseExceptionCode.MaxDepthExceeded,
                $"Strategy panel nesting exceeds the maximum supported depth of {MaxPanelDepth}."
            );
        }

        var children = new List<AtdlPanelChildDto>();

        // Phase-1 deferral: child panels are emitted before controls because there is no
        // shared ordering index across the two separate StrategyPanel_t collections.
        foreach (StrategyPanel_t childPanel in panel.StrategyPanels)
        {
            children.Add(MapPanel(childPanel, paramByName, builder, depth + 1));
        }

        // Controls are ordered by their Index property, which is set sequentially as
        // each control is added to a ControlCollection.
        foreach (Control_t control in panel.Controls.OrderBy(c => c.Index))
        {
            children.Add(MapControl(control, paramByName, builder));
        }

        return new AtdlPanelDto(
            Title: panel.Title,
            Border: panel.Border?.ToString() ?? "None",
            Orientation: panel.Orientation?.ToString() ?? "Vertical",
            Collapsible: panel.Collapsible ?? false,
            Collapsed: panel.Collapsed ?? false,
            Children: children
        );
    }

    private static AtdlControlDto MapControl(
        Control_t control,
        Dictionary<string, AtdlParameterDto> paramByName,
        StateRuleAstBuilder builder
    )
    {
        // Inline the parameter DTO when ParameterRef resolves to a known parameter.
        // If ParameterRef is set but the parameter was not found (validator catches that),
        // leave Parameter null and let the validator surface the error.
        AtdlParameterDto? inlinedParam = null;
        if (!string.IsNullOrEmpty(control.ParameterRef))
        {
            paramByName.TryGetValue(control.ParameterRef, out inlinedParam);
        }

        // List items — present only on list-bearing controls.
        IReadOnlyList<AtdlListItemDto>? listItems = null;
        if (control is ListControlBase listControl && listControl.HasListItems)
        {
            listItems = listControl.ListItems.Select(li => new AtdlListItemDto(li.EnumId, li.UiRep)).ToList();
        }

        // InitValue — accessible from InitializableControl<T>.InitValue but the type varies.
        // Read it as object via the generic base; safe to box since it's for JSON display.
        object? initValue = GetInitValue(control);

        var stateRules = MapStateRules(control, builder);

        return new AtdlControlDto(
            Id: control.Id,
            Type: control.GetType().Name,
            Label: control.Label,
            ParameterRef: string.IsNullOrEmpty(control.ParameterRef) ? null : control.ParameterRef,
            Parameter: inlinedParam,
            ListItems: listItems,
            InitValue: initValue,
            StateRules: stateRules,
            Tooltip: control.ToolTip,
            CheckedEnumRef: (control as BinaryControlBase)?.CheckedEnumRef,
            UncheckedEnumRef: (control as BinaryControlBase)?.UncheckedEnumRef,
            RadioGroup: (control as RadioButton_t)?.RadioGroup,
            Increment: (control as SingleSpinner_t)?.Increment,
            InitValueMode: (control as Clock_t)?.InitValueMode,
            LocalMktTz: (control as Clock_t)?.LocalMktTz,
            InnerIncrement: (control as DoubleSpinner_t)?.InnerIncrement,
            OuterIncrement: (control as DoubleSpinner_t)?.OuterIncrement,
            InitPolicy: control.InitPolicy?.ToString(),
            InitFixField: ResolveInitFixField(control.InitFixField)
        );
    }

    // ---------------------------------------------------------------------------
    // StateRule mapping
    // ---------------------------------------------------------------------------

    private static List<AtdlStateRuleDto> MapStateRules(Control_t control, StateRuleAstBuilder builder)
    {
        if (control.StateRules.Count == 0)
        {
            return [];
        }

        var result = new List<AtdlStateRuleDto>(control.StateRules.Count);

        foreach (StateRule_t rule in control.StateRules)
        {
            // StateRule_t models each behaviour as a separate nullable property rather than
            // as an Inclusion_t enum — Enabled, Visible, and Value are independent.
            // Emit one AtdlStateRuleDto per non-null behaviour.
            if (rule.Enabled is not null)
            {
                result.Add(BuildStateRuleDto(rule, AtdlStateRuleEffect.Enabled, rule.Enabled.Value, builder));
            }

            if (rule.Visible is not null)
            {
                result.Add(BuildStateRuleDto(rule, AtdlStateRuleEffect.Visible, rule.Visible.Value, builder));
            }

            if (rule.Value is not null)
            {
                result.Add(BuildStateRuleDto(rule, AtdlStateRuleEffect.Value, rule.Value, builder));
            }
        }

        return result;
    }

    private static AtdlStateRuleDto BuildStateRuleDto(
        StateRule_t rule,
        string effect,
        bool targetValue,
        StateRuleAstBuilder builder
    )
    {
        var expression = ResolveStateRuleExpression(rule, builder);
        return new AtdlStateRuleDto(effect, targetValue, expression);
    }

    private static AtdlStateRuleDto BuildStateRuleDto(
        StateRule_t rule,
        string effect,
        string targetStringValue,
        StateRuleAstBuilder builder
    )
    {
        var expression = ResolveStateRuleExpression(rule, builder);
        return new AtdlStateRuleDto(effect, false, expression, targetStringValue);
    }

    private static StateRuleAstNodeDto ResolveStateRuleExpression(StateRule_t rule, StateRuleAstBuilder builder)
    {
        if (rule.Edit is not null)
        {
            return builder.Build(rule.Edit);
        }

        if (rule.EditRef is not null)
        {
            // EditRef_t<T>.ReferencedEdit is internal in the FixAtdl assembly.
            // Use the public Id property and resolve through the per-strategy builder's registries,
            // which were populated with the strategy's Edits collection before mapping began.
            return builder.BuildFromRef(rule.EditRef.Id);
        }

        throw new AtdlParseException(
            AtdlParseExceptionCode.ParseFailed,
            "No valid Edit or EditRef was supplied for this StateRule."
        );
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static object? GetInitValue(Control_t control)
    {
        // InitValue lives on InitializableControl<T> which is a generic type.
        // Access it via reflection-free casting to the known concrete base types.
        return control switch
        {
            Clock_t c => c.InitValue?.Raw,
            NumericControlBase n => n.InitValue,
            ListControlBase l => l.InitValue,
            // BinaryControlBase has its own InitValue; access through the base property.
            _ => TryGetInitValueViaReflection(control),
        };
    }

    // Fallback for control types whose InitValue we cannot reach via a known base cast
    // (e.g. Clock_t, HiddenField_t, Label_t).  Uses a single reflection call at mapping
    // time (not a hot path) so the cost is acceptable.
    private static object? TryGetInitValueViaReflection(Control_t control)
    {
        return GetProperty(control, "InitValue");
    }

    private static object? MapBound(object? parameterType, string prefix, string type)
    {
        var value = GetProperty(parameterType, prefix + "ValueText") ?? GetProperty(parameterType, prefix + "Value");
        return MapScalar(value, type);
    }

    private static object? MapConstValue(IParameter param, object? parameterType)
    {
        var constValue = GetProperty(parameterType, "ConstValue");
        if (constValue is null or string or int or uint or decimal or bool)
        {
            return constValue;
        }

        return param.WireValue;
    }

    private static int? ResolveInitFixField(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        if (int.TryParse(name, out var tag))
        {
            return tag;
        }

        if (!name.Contains(',') && Enum.TryParse(name, ignoreCase: true, out FixField field) && Enum.IsDefined(field))
        {
            return (int)field;
        }

        return null;
    }

    private static object? MapScalar(object? value, string type) =>
        value is null or string or int or uint or decimal or bool ? value : FixValueFormatter.Format(value, type, null);

    private static object? GetProperty(object? value, string name) =>
        value?.GetType().GetProperty(name)?.GetValue(value);
}
