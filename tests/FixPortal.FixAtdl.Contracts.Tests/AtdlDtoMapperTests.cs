using System.Globalization;
using AwesomeAssertions;
using FixPortal.FixAtdl.Contracts;
using FixPortal.FixAtdl.Contracts.StateRules;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Controls;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Enumerations;
using Xunit;

namespace FixPortal.FixAtdl.Contracts.Tests;

/// <summary>
/// Unit tests for <see cref="AtdlDtoMapper"/>.
/// Model objects are constructed directly in code (no XML round-trip) for tight isolation.
/// </summary>
public class AtdlDtoMapperTests
{
    // ---------------------------------------------------------------------------
    // Fixture helpers
    // ---------------------------------------------------------------------------

    private static AtdlDtoMapper Mapper() => new();

    /// <summary>
    /// Builds a minimal Strategy_t with a StrategyLayout containing one root panel.
    /// Returns both the strategy and the root panel so callers can add controls / child panels.
    /// </summary>
    private static (Strategy_t strategy, StrategyPanel_t rootPanel) MakeStrategy(string name)
    {
        var strategy = new Strategy_t { Name = name };
        var panel = new StrategyPanel_t(strategy)
        {
            Title = $"{name} Parameters",
            Orientation = Orientation_t.Vertical,
            Collapsible = false,
            Collapsed = false,
        };
        strategy.StrategyLayout = new StrategyLayout_t { StrategyPanel = panel };
        return (strategy, panel);
    }

    private static (Strategies_t Strategies, IReadOnlyDictionary<string, string> SourceXml) SingleStrategyResult(
        Strategy_t strategy,
        string sourceXml = "<Strategy/>"
    )
    {
        var strategies = new Strategies_t();
        strategies.Strategies.Add(strategy);
        return (strategies, new Dictionary<string, string> { [strategy.Name] = sourceXml });
    }

    private static (Strategies_t Strategies, IReadOnlyDictionary<string, string> SourceXml) MultiStrategyResult(
        IReadOnlyList<Strategy_t> strategyList,
        IReadOnlyList<string>? sourceXmls = null
    )
    {
        var strategies = new Strategies_t();
        var slices = new Dictionary<string, string>();
        for (var i = 0; i < strategyList.Count; i++)
        {
            strategies.Strategies.Add(strategyList[i]);
            slices[strategyList[i].Name] = sourceXmls is not null
                ? sourceXmls[i]
                : $"<Strategy name=\"{strategyList[i].Name}\"/>";
        }
        return (strategies, slices);
    }

    // ---------------------------------------------------------------------------
    // Tests
    // ---------------------------------------------------------------------------

    [Fact]
    public void Missing_strategy_edit_expression_reports_parse_failure()
    {
        var (strategy, _) = MakeStrategy("MissingEdit");
        strategy.StrategyEdits.Add(new StrategyEdit_t { ErrorMessage = "missing" });
        var parsed = SingleStrategyResult(strategy);
        var act = () => Mapper().Map(parsed.Strategies, parsed.SourceXml);
        act.Should().Throw<AtdlParseException>().Which.Code.Should().Be(AtdlParseExceptionCode.ParseFailed);
    }

    [Fact]
    public void Invalid_boolean_edit_literal_reports_value_error()
    {
        var (strategy, _) = MakeStrategy("BooleanLiteral");
        strategy.Parameters.Add(new Parameter_t<FixAtdl.Model.Types.Boolean_t>("Flag"));
        strategy.StrategyEdits.Add(
            new StrategyEdit_t
            {
                Edit = new Edit_t<FixAtdl.Model.Elements.Support.IParameter>
                {
                    Field = "Flag",
                    Operator = Operator_t.Equal,
                    Value = "invalid",
                },
            }
        );
        var parsed = SingleStrategyResult(strategy);
        var act = () => Mapper().Map(parsed.Strategies, parsed.SourceXml);
        act.Should().Throw<AtdlParseException>().Which.Code.Should().Be(AtdlParseExceptionCode.InvalidEditValue);
    }

    [Fact]
    public void Configured_boolean_wire_values_override_the_defaults()
    {
        var (strategy, _) = MakeStrategy("BooleanWireOverrides");
        var flag = new Parameter_t<FixAtdl.Model.Types.Boolean_t>("Flag");
        flag.Value.TrueWireValue = "N";
        flag.Value.FalseWireValue = "Y";
        strategy.Parameters.Add(flag);
        strategy.StrategyEdits.Add(
            new StrategyEdit_t
            {
                Edit = new Edit_t<FixAtdl.Model.Elements.Support.IParameter>
                {
                    Field = "Flag",
                    Operator = Operator_t.Equal,
                    Value = "Y",
                },
            }
        );

        var parsed = SingleStrategyResult(strategy);
        var edit = Mapper().Map(parsed.Strategies, parsed.SourceXml).Strategies[0].StrategyEdits!.Single();

        edit.Expression.Value.Should().Be(false);
    }

    [Fact]
    public void Nested_panel_controls_keep_typed_state_rule_comparisons()
    {
        var (strategy, panel) = MakeStrategy("NestedClock");
        var child = new StrategyPanel_t(strategy);
        panel.StrategyPanels.Add(child);
        child.Controls.Add(new Clock_t("clock"));
        var target = new TextField_t("target");
        target.StateRules.Add(
            new StateRule_t
            {
                Enabled = false,
                Edit = new Edit_t<Control_t>
                {
                    Field = "clock",
                    Operator = Operator_t.LessThan,
                    Value = "11:00:00",
                },
            }
        );
        panel.Controls.Add(target);
        var parsed = SingleStrategyResult(strategy);
        var dto = Mapper().Map(parsed.Strategies, parsed.SourceXml);
        dto.Strategies[0]
            .Panel.Children.OfType<AtdlControlDto>()
            .Single()
            .StateRules[0]
            .Expression.ComparisonType.Should()
            .Be("Clock_t");
    }

    [Fact]
    public void Boolean_strategy_edits_preserve_native_state_and_wire_omission_semantics()
    {
        var (strategy, _) = MakeStrategy("Booleans");
        var flag = new Parameter_t<FixAtdl.Model.Types.Boolean_t>("Flag");
        flag.Value.TrueWireValue = "T";
        flag.Value.FalseWireValue = "F";
        var suppressed = new Parameter_t<FixAtdl.Model.Types.Boolean_t>("Suppressed");
        suppressed.Value.TrueWireValue = "{NULL}";
        suppressed.Value.FalseWireValue = "F";
        strategy.Parameters.Add(flag);
        strategy.Parameters.Add(suppressed);
        strategy.StrategyEdits.Add(
            new StrategyEdit_t
            {
                ErrorMessage = "literal",
                Edit = new Edit_t<FixAtdl.Model.Elements.Support.IParameter>
                {
                    Field = "Flag",
                    Operator = Operator_t.Equal,
                    Value = "T",
                },
            }
        );
        strategy.StrategyEdits.Add(
            new StrategyEdit_t
            {
                ErrorMessage = "field2",
                Edit = new Edit_t<FixAtdl.Model.Elements.Support.IParameter>
                {
                    Field = "Flag",
                    Operator = Operator_t.Equal,
                    Field2 = "Suppressed",
                },
            }
        );
        strategy.StrategyEdits.Add(
            new StrategyEdit_t
            {
                ErrorMessage = "omitted",
                Edit = new Edit_t<FixAtdl.Model.Elements.Support.IParameter>
                {
                    Field = "Suppressed",
                    Operator = Operator_t.Equal,
                    Value = "{NULL}",
                },
            }
        );
        strategy.StrategyEdits.Add(
            new StrategyEdit_t
            {
                ErrorMessage = "exists",
                Edit = new Edit_t<FixAtdl.Model.Elements.Support.IParameter>
                {
                    Field = "Suppressed",
                    Operator = Operator_t.Exist,
                },
            }
        );
        var parsed = SingleStrategyResult(strategy);
        var edits = Mapper().Map(parsed.Strategies, parsed.SourceXml).Strategies[0].StrategyEdits!;
        var values = new Dictionary<string, object?> { ["Flag"] = true, ["Suppressed"] = true };
        var evaluator = new StateRuleEvaluator();

        edits[0].Expression.Value.Should().Be(true);
        edits.Should().OnlyContain(edit => evaluator.Evaluate(edit.Expression, values));
        values["Suppressed"] = false;
        evaluator.Evaluate(edits[2].Expression, values).Should().BeFalse();
        values["Suppressed"] = null;
        evaluator.Evaluate(edits[2].Expression, values).Should().BeTrue();
        evaluator.Evaluate(edits[3].Expression, values).Should().BeFalse();
    }

    [Fact]
    public void Maps_parameter_wire_modifiers_and_text_limits()
    {
        var (strategy, _) = MakeStrategy("Modifiers");
        var text = new Parameter_t<FixAtdl.Model.Types.String_t>("Text");
        text.Value.MinLength = 1;
        text.Value.MaxLength = 12;
        var percent = new Parameter_t<FixAtdl.Model.Types.Percentage_t>("Percent");
        percent.Value.MultiplyBy100 = true;
        var selection = new Parameter_t<FixAtdl.Model.Types.MultipleStringValue_t>("Selection");
        selection.Value.InvertOnWire = true;
        var timestamp = new Parameter_t<FixAtdl.Model.Types.UTCTimestamp_t>("Timestamp");
        timestamp.Value.LocalMktTz = "Europe/London";
        strategy.Parameters.Add(text);
        strategy.Parameters.Add(percent);
        strategy.Parameters.Add(selection);
        strategy.Parameters.Add(timestamp);

        var parsed = SingleStrategyResult(strategy);
        var dto = Mapper().Map(parsed.Strategies, parsed.SourceXml).Strategies[0];

        dto.Parameters[0].MinLength.Should().Be(1);
        dto.Parameters[0].MaxLength.Should().Be(12);
        dto.Parameters[1].MultiplyBy100.Should().BeTrue();
        dto.Parameters[2].InvertOnWire.Should().BeTrue();
        dto.Parameters[3].LocalMktTz.Should().Be("Europe/London");
    }

    [Fact]
    public void Maps_constraints_control_metadata_and_strategy_assertions()
    {
        var (strategy, panel) = MakeStrategy("Conformance");
        var parameter = new Parameter_t<FixAtdl.Model.Types.Float_t>("Price");
        parameter.Value.MinValue = 1.25m;
        parameter.Value.MaxValue = 10.5m;
        parameter.Value.Precision = 2;
        strategy.Parameters.Add(parameter);
        var flag = new Parameter_t<FixAtdl.Model.Types.Boolean_t>("Flag");
        flag.Value.TrueWireValue = "T";
        flag.Value.FalseWireValue = "F";
        flag.Value.ConstValue = true;
        strategy.Parameters.Add(flag);
        panel.Controls.Add(
            new SingleSpinner_t("price")
            {
                ParameterRef = "Price",
                Increment = 0.25m,
                InitValue = 2.5m,
                InitPolicy = InitPolicy_t.UseFixField,
                InitFixField = "44",
            }
        );
        panel.Controls.Add(
            new RadioButton_t("enabled")
            {
                CheckedEnumRef = "yes",
                UncheckedEnumRef = "no",
                RadioGroup = "choices",
                InitValue = true,
            }
        );
        panel.Controls.Add(
            new Clock_t("start")
            {
                InitValue = new InitValueClock("09:30:00"),
                InitValueMode = 1,
                LocalMktTz = "Europe/London",
            }
        );
        panel.Controls.Add(new DoubleSpinner_t("double") { InnerIncrement = 0.1m, OuterIncrement = 1m });
        panel
            .Controls[2]
            .StateRules.Add(
                new StateRule_t
                {
                    Enabled = false,
                    Edit = new Edit_t<Control_t>
                    {
                        Field = "start",
                        Value = "09:30:00",
                        Operator = Operator_t.LessThan,
                    },
                }
            );
        strategy.StrategyEdits.Add(
            new StrategyEdit_t
            {
                ErrorMessage = "Price must be below Limit",
                Edit = new Edit_t<FixAtdl.Model.Elements.Support.IParameter>
                {
                    Field = "Price",
                    Field2 = "Limit",
                    Operator = Operator_t.LessThan,
                },
            }
        );

        var parsed = SingleStrategyResult(strategy);
        var dto = Mapper().Map(parsed.Strategies, parsed.SourceXml).Strategies[0];

        dto.Parameters[0].Min.Should().Be(1.25m);
        dto.Parameters[0].Max.Should().Be(10.5m);
        dto.Parameters[0].Precision.Should().Be(2);
        dto.Parameters[0].DefaultValue.Should().BeNull();
        dto.Parameters[1].TrueWireValue.Should().Be("T");
        dto.Parameters[1].FalseWireValue.Should().Be("F");
        dto.Parameters[1].ConstValue.Should().Be(true);
        var spinner = (AtdlControlDto)dto.Panel.Children[0];
        spinner.Increment.Should().Be(0.25m);
        spinner.InitPolicy.Should().Be("UseFixField");
        spinner.InitFixField.Should().Be(44);
        spinner.InitValue.Should().Be(2.5m);
        spinner.Parameter.Should().BeSameAs(dto.Parameters[0]);
        var radio = (AtdlControlDto)dto.Panel.Children[1];
        radio.CheckedEnumRef.Should().Be("yes");
        radio.UncheckedEnumRef.Should().Be("no");
        radio.RadioGroup.Should().Be("choices");
        radio.InitValue.Should().Be(true);
        var clock = (AtdlControlDto)dto.Panel.Children[2];
        clock.InitValueMode.Should().Be(1);
        clock.LocalMktTz.Should().Be("Europe/London");
        clock.InitValue.Should().Be("09:30:00");
        clock.StateRules[0].Expression.ComparisonType.Should().Be("Clock_t");
        var doubleSpinner = (AtdlControlDto)dto.Panel.Children[3];
        doubleSpinner.InnerIncrement.Should().Be(0.1m);
        doubleSpinner.OuterIncrement.Should().Be(1m);
        dto.StrategyEdits.Should().ContainSingle();
        dto.StrategyEdits[0].ErrorMessage.Should().Be("Price must be below Limit");
        dto.StrategyEdits[0].Expression.Field2.Should().Be("Limit");
        dto.StrategyEdits[0].Expression.ComparisonType.Should().Be("Float_t");
    }

    [Fact]
    public void Maps_canonical_TWAP_to_dto_with_one_strategy()
    {
        // Arrange: TWAP strategy with two parameters and two controls.
        var (strategy, panel) = MakeStrategy("TWAP");

        // Parameters
        var durParam = new Parameter_t<FixAtdl.Model.Types.Int_t>("Duration");
        var urgParam = new Parameter_t<FixAtdl.Model.Types.Char_t>("Urgency");
        strategy.Parameters.Add(durParam);
        strategy.Parameters.Add(urgParam);

        // Controls
        var durControl = new TextField_t("c_Duration") { ParameterRef = "Duration", Label = "Duration (mins)" };
        var urgControl = new DropDownList_t("c_Urgency") { ParameterRef = "Urgency", Label = "Urgency" };
        panel.Controls.Add(durControl);
        panel.Controls.Add(urgControl);

        var parsed = SingleStrategyResult(strategy);

        // Act
        var dto = Mapper().Map(parsed.Strategies, parsed.SourceXml);

        // Assert
        dto.Strategies.Should().HaveCount(1);
        var strat = dto.Strategies[0];
        strat.Name.Should().Be("TWAP");
        strat.Parameters.Should().HaveCount(2);
        strat.Parameters[0].Name.Should().Be("Duration");
        strat.Parameters[1].Name.Should().Be("Urgency");
    }

    [Fact]
    public void Inlines_parameterRef_into_control_parameter()
    {
        // Arrange: one strategy with one control that references "Duration".
        var (strategy, panel) = MakeStrategy("TWAP");

        var durParam = new Parameter_t<FixAtdl.Model.Types.Int_t>("Duration");
        strategy.Parameters.Add(durParam);

        var durControl = new TextField_t("c_Duration") { ParameterRef = "Duration", Label = "Duration (mins)" };
        panel.Controls.Add(durControl);

        var parsed = SingleStrategyResult(strategy);

        // Act
        var dto = Mapper().Map(parsed.Strategies, parsed.SourceXml);

        // Assert — the control DTO's Parameter property must be populated and name-matched.
        var stratDto = dto.Strategies[0];
        var firstChild = stratDto.Panel.Children[0];
        firstChild.Should().BeOfType<AtdlControlDto>();
        var controlDto = (AtdlControlDto)firstChild;
        controlDto.ParameterRef.Should().Be("Duration");
        controlDto.Parameter.Should().NotBeNull();
        controlDto.Parameter.Name.Should().Be("Duration");
        // The inlined parameter must be identical to the strategy-level parameter DTO.
        stratDto.Parameters.Should().ContainSingle(p => p.Name == "Duration");
        controlDto
            .Parameter.Should()
            .Be(
                stratDto.Parameters.Single(p => p.Name == "Duration"),
                because: "the inlined parameter must be the same DTO instance as the strategy-level parameter"
            );
    }

    [Fact]
    public void Preserves_panel_tree_depth_and_child_order()
    {
        // Arrange: root panel containing a child panel THEN a control.
        // Phase-1 deferral: the mapper emits all child panels before controls within the same
        // parent because StrategyPanel_t keeps panels and controls in separate collections with
        // no shared index.  The correct declaration order (child panel first, control second)
        // round-trips correctly under this deferral.
        var (strategy, rootPanel) = MakeStrategy("TWAP");

        // Child panel nested inside root.
        var nestedPanel = new StrategyPanel_t(strategy, rootPanel)
        {
            Title = "Nested",
            Orientation = Orientation_t.Horizontal,
        };
        rootPanel.StrategyPanels.Add(nestedPanel);

        // Control on the root panel.
        var control = new TextField_t("c_Qty") { Label = "Qty" };
        rootPanel.Controls.Add(control);

        // A control inside the nested panel.
        var nestedControl = new TextField_t("c_Price") { Label = "Price" };
        nestedPanel.Controls.Add(nestedControl);

        var parsed = SingleStrategyResult(strategy);

        // Act
        var dto = Mapper().Map(parsed.Strategies, parsed.SourceXml);

        var rootPanelDto = dto.Strategies[0].Panel;

        // Root panel: 2 children — nested panel first, then control.
        rootPanelDto.Children.Should().HaveCount(2);
        rootPanelDto
            .Children[0]
            .Should()
            .BeOfType<AtdlPanelDto>(because: "child panels are emitted before controls in phase-1 deferral ordering");
        rootPanelDto.Children[1].Should().BeOfType<AtdlControlDto>();

        // Nested panel has one control child.
        var nestedDto = (AtdlPanelDto)rootPanelDto.Children[0];
        nestedDto.Title.Should().Be("Nested");
        nestedDto.Children.Should().HaveCount(1);
        nestedDto.Children[0].Should().BeOfType<AtdlControlDto>();
    }

    [Fact]
    public void Embeds_StateRule_AST_on_controls_that_have_them()
    {
        // Arrange: a control with a StateRule that disables it when c_Duration == "0".
        var (strategy, panel) = MakeStrategy("TWAP");

        var control = new TextField_t("c_Duration") { Label = "Duration" };
        panel.Controls.Add(control);

        // StateRule: enabled=false when the condition is met.
        var stateRule = new StateRule_t { Enabled = false };
        // Attach an Edit_t<Control_t> — the bound generic form used post-parse.
        var edit = new Edit_t<Control_t>
        {
            Field = "c_Duration",
            Operator = Operator_t.Equal,
            Value = "0",
        };
        stateRule.Edit = edit;
        control.StateRules.Add(stateRule);

        var parsed = SingleStrategyResult(strategy);

        // Act
        var dto = Mapper().Map(parsed.Strategies, parsed.SourceXml);

        var controlDto = dto.Strategies[0].Panel.Children.OfType<AtdlControlDto>().Single();

        controlDto.StateRules.Should().HaveCount(1);
        var srDto = controlDto.StateRules[0];
        srDto.Effect.Should().Be(AtdlStateRuleEffect.Enabled);
        srDto.TargetValue.Should().BeFalse();
        srDto.Expression.Should().NotBeNull();
        srDto.Expression.Operator.Should().Be(StateRuleOperator.Eq);
        srDto.Expression.Field.Should().Be("c_Duration");
    }

    [Fact]
    public void StrategyCount_matches_top_level_strategies_count()
    {
        // Arrange: two strategies.
        var (s1, _) = MakeStrategy("TWAP");
        var (s2, _) = MakeStrategy("VWAP");
        var parsed = MultiStrategyResult([s1, s2]);

        // Act
        var dto = Mapper().Map(parsed.Strategies, parsed.SourceXml);

        // Assert
        dto.Strategies.Should().HaveCount(2);
        dto.Strategies[0].Name.Should().Be("TWAP");
        dto.Strategies[1].Name.Should().Be("VWAP");
    }

    [Fact]
    public void Carries_per_strategy_source_xml_for_the_XML_pane()
    {
        // Arrange: two strategies each with a distinct source-XML slice.
        var (s1, _) = MakeStrategy("TWAP");
        var (s2, _) = MakeStrategy("VWAP");
        const string twapSlice = "<Strategy name=\"TWAP\"><Parameter name=\"P1\"/></Strategy>";
        const string vwapSlice = "<Strategy name=\"VWAP\"><Parameter name=\"P2\"/></Strategy>";
        var parsed = MultiStrategyResult([s1, s2], [twapSlice, vwapSlice]);

        // Act
        var dto = Mapper().Map(parsed.Strategies, parsed.SourceXml);

        // Assert — each strategy carries its own source slice, not the full document.
        dto.Strategies[0].SourceXml.Should().Be(twapSlice);
        dto.Strategies[1].SourceXml.Should().Be(vwapSlice);
    }

    // ---------------------------------------------------------------------------
    // M-06 — Value state rules must appear in the wire DTO
    // ---------------------------------------------------------------------------

    [Fact]
    public void Value_StateRule_is_emitted_with_Value_effect_and_TargetStringValue()
    {
        // Arrange: a control with a StateRule that assigns value "100" when condition is met.
        var (strategy, panel) = MakeStrategy("TWAP");

        var control = new TextField_t("c_Qty") { Label = "Qty" };
        panel.Controls.Add(control);

        // StateRule with Value (not Enabled/Visible) — currently the mapper silently drops this.
        var stateRule = new StateRule_t { Value = "100" };
        var edit = new Edit_t<Control_t>
        {
            Field = "c_Side",
            Operator = Operator_t.Equal,
            Value = "1",
        };
        stateRule.Edit = edit;
        control.StateRules.Add(stateRule);

        var parsed = SingleStrategyResult(strategy);

        // Act
        var dto = Mapper().Map(parsed.Strategies, parsed.SourceXml);

        var controlDto = dto.Strategies[0].Panel.Children.OfType<AtdlControlDto>().Single();

        // Before fix: 0 state rules (Value is silently dropped).
        // After fix: 1 state rule with Effect="value" and TargetStringValue="100".
        controlDto.StateRules.Should().HaveCount(1);
        var srDto = controlDto.StateRules[0];
        srDto.Effect.Should().Be(AtdlStateRuleEffect.Value);
        srDto.TargetStringValue.Should().Be("100");
        srDto.Expression.Should().NotBeNull();
        srDto.Expression.Field.Should().Be("c_Side");
    }

    [Fact]
    public void Typed_constants_use_parameter_wire_text()
    {
        var (strategy, _) = MakeStrategy("WireConstants");
        var data = new Parameter_t<FixAtdl.Model.Types.Data_t>("Raw");
        data.Value.ConstValue = "SOH".ToCharArray();
        var when = new Parameter_t<FixAtdl.Model.Types.TZTimestamp_t>("When");
        when.Value.ConstValue = new DateTime(2026, 7, 15, 7, 39, 0, DateTimeKind.Utc);
        strategy.Parameters.Add(data);
        strategy.Parameters.Add(when);
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var parsed = SingleStrategyResult(strategy);
            var parameters = Mapper().Map(parsed.Strategies, parsed.SourceXml).Strategies[0].Parameters;
            parameters[0].ConstValue.Should().Be("SOH");
            parameters[1].ConstValue.Should().Be("20260715-07:39:00Z");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Named_init_fix_fields_resolve_to_tags()
    {
        var (strategy, panel) = MakeStrategy("NamedTags");
        panel.Controls.Add(new TextField_t("qty") { InitFixField = "FIX_OrderQty" });
        panel.Controls.Add(new TextField_t("px") { InitFixField = "44" });
        panel.Controls.Add(new TextField_t("bad") { InitFixField = "NotAField" });
        panel.Controls.Add(new TextField_t("plus") { InitFixField = "+44" });
        panel.Controls.Add(new TextField_t("minus") { InitFixField = "-44" });
        panel.Controls.Add(new TextField_t("padded") { InitFixField = " 44 " });
        panel.Controls.Add(new TextField_t("zero") { InitFixField = "0" });
        var parsed = SingleStrategyResult(strategy);
        var controls = Mapper()
            .Map(parsed.Strategies, parsed.SourceXml)
            .Strategies[0]
            .Panel.Children.Cast<AtdlControlDto>()
            .ToList();
        controls[0].InitFixField.Should().Be((int)FixField.FIX_OrderQty);
        controls[1].InitFixField.Should().Be(44);
        controls[2].InitFixField.Should().BeNull();
        controls[3].InitFixField.Should().BeNull();
        controls[4].InitFixField.Should().BeNull();
        controls[5].InitFixField.Should().BeNull();
        controls[6].InitFixField.Should().BeNull();
    }

    [Fact]
    public void Fix_field_edits_take_their_comparison_type_from_the_field_dictionary()
    {
        var (strategy, _) = MakeStrategy("FixEdits");
        AddParameterEdit(strategy, "FIX_ClOrdID", "1");
        AddParameterEdit(strategy, "FIX_OrderQty", "100");
        AddParameterEdit(strategy, "fix_OrderQty", "100");
        AddParameterEdit(strategy, "FIX_NoUsernames", "1");
        AddParameterEdit(strategy, "FIX_NotAField", "1");
        var parsed = SingleStrategyResult(strategy);
        var edits = Mapper().Map(parsed.Strategies, parsed.SourceXml).Strategies[0].StrategyEdits!;
        var evaluator = new StateRuleEvaluator();

        edits[0].Expression.ComparisonType.Should().Be("String_t");
        evaluator.Evaluate(edits[0].Expression, State("FIX_ClOrdID", "0001")).Should().BeFalse();
        edits[1].Expression.ComparisonType.Should().BeNull();
        evaluator.Evaluate(edits[1].Expression, State("FIX_OrderQty", "0100.0")).Should().BeTrue();
        evaluator.Evaluate(edits[1].Expression, State("FIX_OrderQty", "1E2")).Should().BeFalse();
        edits[2].Expression.ComparisonType.Should().BeNull();
        edits[3].Expression.ComparisonType.Should().Be("String_t");
        edits[4].Expression.ComparisonType.Should().Be("String_t");
    }

    [Fact]
    public void Boolean_fix_field_compare_keeps_the_parameter_wire_mapping()
    {
        var (strategy, _) = MakeStrategy("BooleanFix");
        var flag = new Parameter_t<FixAtdl.Model.Types.Boolean_t>("Flag");
        flag.Value.TrueWireValue = "1";
        flag.Value.FalseWireValue = "0";
        strategy.Parameters.Add(flag);
        strategy.StrategyEdits.Add(
            new StrategyEdit_t
            {
                Edit = new Edit_t<FixAtdl.Model.Elements.Support.IParameter>
                {
                    Field = "Flag",
                    Field2 = "FIX_PossResend",
                    Operator = Operator_t.Equal,
                },
            }
        );
        var parsed = SingleStrategyResult(strategy);
        var expression = Mapper()
            .Map(parsed.Strategies, parsed.SourceXml)
            .Strategies[0]
            .StrategyEdits!.Single()
            .Expression;
        var evaluator = new StateRuleEvaluator();

        expression.TrueWireValue.Should().Be("1");
        expression.FalseWireValue.Should().Be("0");
        evaluator
            .Evaluate(expression, new Dictionary<string, object?> { ["Flag"] = true, ["FIX_PossResend"] = "1" })
            .Should()
            .BeTrue();
        var act = () =>
            evaluator.Evaluate(
                expression,
                new Dictionary<string, object?> { ["Flag"] = true, ["FIX_PossResend"] = "True" }
            );
        act.Should().Throw<AtdlParseException>().Which.Code.Should().Be(AtdlParseExceptionCode.InvalidEditValue);
    }

    [Fact]
    public void Boolean_literals_match_wire_tokens_ordinally()
    {
        var (strategy, _) = MakeStrategy("OrdinalBoolean");
        var mixed = new Parameter_t<FixAtdl.Model.Types.Boolean_t>("Mixed");
        mixed.Value.TrueWireValue = "a";
        mixed.Value.FalseWireValue = "A";
        var loweredFalse = new Parameter_t<FixAtdl.Model.Types.Boolean_t>("LoweredFalse");
        loweredFalse.Value.FalseWireValue = "y";
        strategy.Parameters.Add(mixed);
        strategy.Parameters.Add(loweredFalse);
        AddParameterEdit(strategy, "Mixed", "A");
        AddParameterEdit(strategy, "LoweredFalse", "Y");
        var parsed = SingleStrategyResult(strategy);
        var edits = Mapper().Map(parsed.Strategies, parsed.SourceXml).Strategies[0].StrategyEdits!;

        edits[0].Expression.Value.Should().Be(false);
        edits[1].Expression.Value.Should().Be(true);
    }

    [Fact]
    public void State_rule_without_an_edit_reports_parse_failure()
    {
        var (strategy, panel) = MakeStrategy("MissingStateRule");
        var target = new TextField_t("target");
        target.StateRules.Add(new StateRule_t { Enabled = false });
        panel.Controls.Add(target);
        var parsed = SingleStrategyResult(strategy);
        var act = () => Mapper().Map(parsed.Strategies, parsed.SourceXml);

        act.Should()
            .Throw<AtdlParseException>()
            .Which.Should()
            .Match<AtdlParseException>(ex =>
                ex.Code == AtdlParseExceptionCode.ParseFailed
                && ex.Message.Contains("StateRule", StringComparison.Ordinal)
            );
    }

    private static void AddParameterEdit(Strategy_t strategy, string field, string value)
    {
        strategy.StrategyEdits.Add(
            new StrategyEdit_t
            {
                Edit = new Edit_t<FixAtdl.Model.Elements.Support.IParameter>
                {
                    Field = field,
                    Operator = Operator_t.Equal,
                    Value = value,
                },
            }
        );
    }

    private static Dictionary<string, object?> State(string field, string value) => new() { [field] = value };
}
