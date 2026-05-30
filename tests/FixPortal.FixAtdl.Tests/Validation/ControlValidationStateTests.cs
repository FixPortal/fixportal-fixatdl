using System.Text;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Utility;
using FixPortal.FixAtdl.Validation;
using FixPortal.FixAtdl.Xml;

namespace FixPortal.FixAtdl.Tests.Validation;

/// <summary>
/// Tests for <see cref="ControlValidationState"/>: construction, default state,
/// Add/Remove, Evaluate, ControlValidationResult, ParameterValidationResult,
/// ErrorText composition.
/// </summary>
public class ControlValidationStateTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Strategy_t LoadTwap()
    {
        const string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Strategies xmlns=""http://www.fixprotocol.org/FIXatdl-1-1/Core""
            xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
            strategyIdentifierTag=""5001"">
  <Strategy name=""TWAP"" version=""1"" wireValue=""TWAP"" uiRep=""TWAP"" providerID=""DEMO"">
    <Parameter name=""Participation"" xsi:type=""Percentage_t"" fixTag=""7700"" use=""optional""/>
  </Strategy>
</Strategies>";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return new StrategiesReader().Load(stream).Strategies[0];
    }

    private static StrategyEdit_t MakePassingStrategyEdit(Strategy_t twap)
    {
        twap.Parameters["Participation"].WireValue = "50";
        var edit = new Edit_t<IParameter>
        {
            Field = "Participation",
            Operator = Operator_t.Equal,
            Value = "50"
        };
        ((IResolvable<Strategy_t, IParameter>)edit).Resolve(twap, twap.Parameters);
        return new StrategyEdit_t { Edit = edit, ErrorMessage = "Participation must be 50" };
    }

    private static StrategyEdit_t MakeFailingStrategyEdit(Strategy_t twap)
    {
        twap.Parameters["Participation"].WireValue = "30";
        var edit = new Edit_t<IParameter>
        {
            Field = "Participation",
            Operator = Operator_t.Equal,
            Value = "50"
        };
        ((IResolvable<Strategy_t, IParameter>)edit).Resolve(twap, twap.Parameters);
        var se = new StrategyEdit_t { Edit = edit, ErrorMessage = "Participation must be 50" };
        // Pre-evaluate so CurrentState is set before adding to ControlValidationState.
        se.Evaluate();
        return se;
    }

    // ── Construction ─────────────────────────────────────────────────────────

    [Fact]
    public void ControlId_is_set_from_constructor()
    {
        var state = new ControlValidationState("ctrl-1");
        state.ControlId.Should().Be("ctrl-1");
    }

    // ── CurrentState defaults ─────────────────────────────────────────────────

    [Fact]
    public void CurrentState_is_true_when_no_edits_and_no_results_set()
    {
        var state = new ControlValidationState("ctrl-1");
        state.CurrentState.Should().BeTrue();
    }

    [Fact]
    public void CurrentState_is_false_when_ControlValidationResult_is_invalid()
    {
        var state = new ControlValidationState("ctrl-1") { ControlValidationResult = new ValidationResult(ValidationResult.ResultType.Invalid, "bad") };
        state.CurrentState.Should().BeFalse();
    }

    [Fact]
    public void CurrentState_is_true_when_ControlValidationResult_is_valid()
    {
        var state = new ControlValidationState("ctrl-1") { ControlValidationResult = ValidationResult.ValidResult };
        state.CurrentState.Should().BeTrue();
    }

    [Fact]
    public void CurrentState_is_false_when_ParameterValidationResult_is_invalid()
    {
        var state = new ControlValidationState("ctrl-1") { ParameterValidationResult = new ValidationResult(ValidationResult.ResultType.Invalid, "param-bad") };
        state.CurrentState.Should().BeFalse();
    }

    // ── Add / Remove ──────────────────────────────────────────────────────────

    [Fact]
    public void CurrentState_is_false_when_added_failing_strategyEdit()
    {
        var twap = LoadTwap();
        var se = MakeFailingStrategyEdit(twap);

        var state = new ControlValidationState("ctrl-1");
        state.Add(se);

        state.CurrentState.Should().BeFalse();
    }

    [Fact]
    public void CurrentState_is_true_after_failing_strategyEdit_removed()
    {
        var twap = LoadTwap();
        var se = MakeFailingStrategyEdit(twap);

        var state = new ControlValidationState("ctrl-1");
        state.Add(se);
        state.Remove(se);

        state.CurrentState.Should().BeTrue();
    }

    [Fact]
    public void CurrentState_is_true_after_passing_strategyEdit_added()
    {
        var twap = LoadTwap();
        var se = MakePassingStrategyEdit(twap);
        se.Evaluate();

        var state = new ControlValidationState("ctrl-1");
        state.Add(se);

        state.CurrentState.Should().BeTrue();
    }

    // ── Evaluate ──────────────────────────────────────────────────────────────

    [Fact]
    public void Evaluate_with_empty_provider_updates_strategyEdit_state()
    {
        var twap = LoadTwap();
        var se = MakePassingStrategyEdit(twap);

        var state = new ControlValidationState("ctrl-1");
        state.Add(se);
        state.Evaluate(FixFieldValueProvider.Empty);

        // After evaluate the passing edit should have CurrentState = true.
        state.CurrentState.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_marks_state_false_when_edit_fails()
    {
        var twap = LoadTwap();
        // Set value that does NOT satisfy the edit (value=30, edit checks ==50)
        twap.Parameters["Participation"].WireValue = "30";
        var edit = new Edit_t<IParameter>
        {
            Field = "Participation",
            Operator = Operator_t.Equal,
            Value = "50"
        };
        ((IResolvable<Strategy_t, IParameter>)edit).Resolve(twap, twap.Parameters);
        var se = new StrategyEdit_t { Edit = edit, ErrorMessage = "Must be 50" };

        var state = new ControlValidationState("ctrl-1");
        state.Add(se);
        state.Evaluate(FixFieldValueProvider.Empty);

        state.CurrentState.Should().BeFalse();
    }

    // ── ErrorText ────────────────────────────────────────────────────────────

    [Fact]
    public void ErrorText_is_empty_when_all_valid()
    {
        var state = new ControlValidationState("ctrl-1") { ControlValidationResult = ValidationResult.ValidResult };
        state.ErrorText.Should().BeEmpty();
    }

    [Fact]
    public void ErrorText_contains_ControlValidationResult_error()
    {
        var state = new ControlValidationState("ctrl-1") { ControlValidationResult = new ValidationResult(ValidationResult.ResultType.Invalid, "ctrl error") };
        state.ErrorText.Should().Contain("ctrl error");
    }

    [Fact]
    public void ErrorText_contains_ParameterValidationResult_error()
    {
        var state = new ControlValidationState("ctrl-1") { ParameterValidationResult = new ValidationResult(ValidationResult.ResultType.Invalid, "param error") };
        state.ErrorText.Should().Contain("param error");
    }

    [Fact]
    public void ErrorText_contains_failing_strategyEdit_error_message()
    {
        var twap = LoadTwap();
        var se = MakeFailingStrategyEdit(twap);

        var state = new ControlValidationState("ctrl-1");
        state.Add(se);
        state.ErrorText.Should().Contain("Participation must be 50");
    }

    [Fact]
    public void ErrorText_combines_multiple_error_sources()
    {
        var twap = LoadTwap();
        var se = MakeFailingStrategyEdit(twap);

        var state = new ControlValidationState("ctrl-1")
        {
            ControlValidationResult = new ValidationResult(ValidationResult.ResultType.Invalid, "ctrl error"),
            ParameterValidationResult = new ValidationResult(ValidationResult.ResultType.Invalid, "param error")
        };
        state.Add(se);

        var errorText = state.ErrorText;
        errorText.Should().Contain("ctrl error");
        errorText.Should().Contain("param error");
        errorText.Should().Contain("Participation must be 50");
    }

    [Fact]
    public void ErrorText_does_not_include_passing_strategyEdit()
    {
        var twap = LoadTwap();
        var se = MakePassingStrategyEdit(twap);
        se.Evaluate();

        var state = new ControlValidationState("ctrl-1");
        state.Add(se);
        state.ErrorText.Should().BeEmpty();
    }

    // ── ValidationResult ─────────────────────────────────────────────────────

    [Fact]
    public void ValidationResult_ValidResult_is_valid_with_null_error_text()
    {
        var result = ValidationResult.ValidResult;
        result.IsValid.Should().BeTrue();
        result.IsMissing.Should().BeFalse();
        // NOTE: ErrorText on the private default constructor is initialised to null! not empty string.
        result.ErrorText.Should().BeNull();
    }

    [Fact]
    public void ValidationResult_Missing_type_reports_IsMissing_true()
    {
        var result = new ValidationResult(ValidationResult.ResultType.Missing, "missing field {0}", "Participation");
        result.IsValid.Should().BeFalse();
        result.IsMissing.Should().BeTrue();
        result.ErrorText.Should().Contain("Participation");
    }

    [Fact]
    public void ValidationResult_Invalid_type_reports_IsValid_false()
    {
        var result = new ValidationResult(ValidationResult.ResultType.Invalid, "bad value");
        result.IsValid.Should().BeFalse();
        result.IsMissing.Should().BeFalse();
        result.ErrorText.Should().Be("bad value");
    }
}
