using System.Text;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Model;
using FixPortal.FixAtdl.Model.Controls.Support;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Utility;
using FixPortal.FixAtdl.Xml;

namespace FixPortal.FixAtdl.Tests.Conformance;

/// <summary>
/// FIXatdl 1.1 section 4.3 and Edit/Parameter attribute-table conformance.
/// Specification: FIXTradingCommunity/fixatdl-specification, 9bf2b793, v1-1-STANDARD/FIXatdl.md.
/// </summary>
public class ExpressionAndParameterConformanceTests
{
    private static Strategy_t Load()
    {
        using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(FixtureFiles.ReadAllText("Fixtures/Conformance/expressions.xml"))
        );
        return new StrategiesReader().Load(stream).Strategies[0];
    }

    private static Edit_t<IParameter> ParameterEdit(
        Strategy_t strategy,
        string field,
        Operator_t op,
        string? value = null,
        string? field2 = null
    )
    {
        var edit = new Edit_t<IParameter>
        {
            Field = field,
            Operator = op,
            Value = value!,
            Field2 = field2!,
        };
        ((IResolvable<Strategy_t, IParameter>)edit).Resolve(strategy, strategy.Parameters);
        return edit;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void String_parameter_preserves_leading_zero_in_literal_and_field2_comparisons(bool field2)
    {
        var strategy = Load();
        strategy.Parameters["Text"].WireValue = "01";
        strategy.Parameters["OtherText"].WireValue = "1";
        var edit = ParameterEdit(strategy, "Text", Operator_t.Equal, field2 ? null : "1", field2 ? "OtherText" : null);

        edit.Evaluate();

        edit.CurrentState.Should().BeFalse("String_t values retain their declared string identity");
    }

    [Theory]
    [InlineData("T", "T", true)]
    [InlineData("T", "F", false)]
    [InlineData("F", "F", true)]
    [InlineData("F", "T", false)]
    public void Strategy_edit_boolean_literal_uses_declared_wire_mapping(string wire, string literal, bool expected)
    {
        var strategy = Load();
        strategy.Parameters["Enabled"].WireValue = wire;
        var edit = ParameterEdit(strategy, "Enabled", Operator_t.Equal, literal);

        edit.Evaluate();

        edit.CurrentState.Should().Be(expected, "Edit/@value is the parameter wireValue in a StrategyEdit");
    }

    [Theory]
    [InlineData("1", "1.0", true)]
    [InlineData("2", "1.0", false)]
    public void Numeric_field2_comparison_accepts_compatible_numeric_types(string left, string right, bool expected)
    {
        var strategy = Load();
        strategy.Parameters["Amount"].WireValue = left;
        strategy.Parameters["OtherAmount"].WireValue = right;
        var edit = ParameterEdit(strategy, "Amount", Operator_t.Equal, field2: "OtherAmount");

        edit.Evaluate();

        edit.CurrentState.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, Operator_t.NotExist, true)]
    [InlineData("", Operator_t.NotExist, true)]
    [InlineData("{NULL}", Operator_t.NotExist, true)]
    [InlineData("0", Operator_t.Exist, true)]
    [InlineData(" ", Operator_t.Exist, true)]
    public void Missing_empty_and_explicitly_cleared_text_are_absent(string? wire, Operator_t op, bool expected)
    {
        var strategy = Load();
        if (wire != null)
        {
            strategy.Parameters["Text"].WireValue = wire;
        }
        var edit = ParameterEdit(strategy, "Text", op);

        edit.Evaluate();

        edit.CurrentState.Should().Be(expected);
    }

    [Theory]
    [InlineData(Operator_t.Equal, true)]
    [InlineData(Operator_t.NotEqual, false)]
    [InlineData(Operator_t.GreaterThan, false)]
    [InlineData(Operator_t.LessThan, false)]
    public void Two_absent_parameter_operands_are_equal_but_not_ordered(Operator_t op, bool expected)
    {
        var strategy = Load();
        var edit = ParameterEdit(strategy, "Text", op, field2: "OtherText");

        edit.Evaluate();

        edit.CurrentState.Should().Be(expected);
    }

    [Fact]
    public void Enumerations_use_enum_ids_for_state_rules_and_wire_values_for_strategy_edits()
    {
        var strategy = Load();
        var control = strategy.Controls["ModeControl"];
        control.LoadInitValue(FixPortal.FixAtdl.Fix.FixFieldValueProvider.Empty);
        control.SetValue(new EnumState(["e_Low", "e_High"]) { ["e_Low"] = true });
        strategy.Parameters["Mode"].SetValueFromControl(control).IsValid.Should().BeTrue();
        var stateEdit = new Edit_t<Control_t>
        {
            Field = "ModeControl",
            Operator = Operator_t.Equal,
            Value = "e_Low",
        };
        ((IResolvable<Strategy_t, Control_t>)stateEdit).Resolve(strategy, strategy.Controls);
        var strategyEdit = ParameterEdit(strategy, "Mode", Operator_t.Equal, "L");

        stateEdit.Evaluate();
        strategyEdit.Evaluate();

        stateEdit.CurrentState.Should().BeTrue();
        strategyEdit.CurrentState.Should().BeTrue();
        strategy.Parameters["Mode"].WireValue.Should().Be("L");
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    public void Xor_requires_exactly_one_true_operand(int trueCount, bool expected)
    {
        var strategy = Load();
        strategy.Parameters["Amount"].WireValue = "1";
        var edit = new Edit_t<IParameter> { LogicOperator = LogicOperator_t.Xor };
        for (var index = 0; index < 3; index++)
        {
            edit.Edits.Add(ParameterEdit(strategy, "Amount", Operator_t.Equal, index < trueCount ? "1" : "2"));
        }

        edit.Evaluate();

        edit.CurrentState.Should().Be(expected, "FIXatdl defines XOR as one and only one, not odd parity");
    }

    [Theory]
    [InlineData("1")]
    [InlineData("9")]
    public void Parameter_numeric_bounds_are_inclusive(string wire)
    {
        var parameter = Load().Parameters["Amount"];

        parameter.WireValue = wire;

        parameter.WireValue.Should().Be(wire);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("10")]
    [InlineData(Atdl.NullValue)]
    [InlineData("")]
    public void Invalid_or_missing_required_wire_assignment_preserves_last_valid_value(string wire)
    {
        var parameter = Load().Parameters["Amount"];
        parameter.WireValue = "5";

        var act = () => parameter.WireValue = wire;

        act.Should().Throw<InvalidFieldValueException>();
        parameter.WireValue.Should().Be("5");
    }

    [Fact]
    public void Unset_required_parameter_blocks_wire_output()
    {
        var strategy = Load();

        var act = () => strategy.Parameters.GetOutputValues();

        act.Should().Throw<MissingMandatoryValueException>();
    }

    [Theory]
    [InlineData(0, false, "5")]
    [InlineData(1, true, "1")]
    [InlineData(9, true, "9")]
    [InlineData(10, false, "5")]
    public void Control_to_parameter_update_applies_the_same_numeric_bounds(int value, bool valid, string expectedWire)
    {
        var strategy = Load();
        strategy.Controls.LoadDefaults(FixPortal.FixAtdl.Fix.FixFieldValueProvider.Empty);
        strategy.Parameters["Amount"].WireValue = "5";
        strategy.Controls["AmountControl"].SetValue((decimal)value);

        var result = strategy.Controls.TryUpdateParameterValues(strategy.Parameters, false, out var failures);

        result.Should().Be(valid);
        (failures == null).Should().Be(valid);
        strategy.Parameters["Amount"].WireValue.Should().Be(expectedWire);
    }

    [Fact]
    public void Clearing_required_control_reports_missing_and_clears_parameter()
    {
        var strategy = Load();
        strategy.Controls.LoadDefaults(FixPortal.FixAtdl.Fix.FixFieldValueProvider.Empty);
        strategy.Parameters["Amount"].WireValue = "5";
        strategy.Controls["AmountControl"].Reset();

        var result = strategy.Controls.TryUpdateParameterValues(strategy.Parameters, false, out var failures);

        result.Should().BeFalse();
        failures.Should().ContainSingle().Which.IsMissing.Should().BeTrue();
        strategy.Parameters["Amount"].IsSet.Should().BeFalse();
    }

    [Theory]
    [InlineData("EQ")]
    [InlineData("NE")]
    [InlineData("LT")]
    [InlineData("LE")]
    [InlineData("GT")]
    [InlineData("GE")]
    public void Comparison_without_value_or_field2_is_rejected_during_loading(string op)
    {
        var xml = FixtureFiles
            .ReadAllText("Fixtures/Conformance/expressions.xml")
            .Replace(
                "</Strategy>",
                $"""
                <val:StrategyEdit xmlns:val="http://www.fixprotocol.org/FIXatdl-1-1/Validation" errorMessage="Invalid comparison">
                  <val:Edit field="Text" operator="{op}"/>
                </val:StrategyEdit>
                </Strategy>
                """,
                StringComparison.Ordinal
            );
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        var act = () => new StrategiesReader().Load(stream);

        act.Should().Throw<InconsistentStrategyException>();
    }
}
