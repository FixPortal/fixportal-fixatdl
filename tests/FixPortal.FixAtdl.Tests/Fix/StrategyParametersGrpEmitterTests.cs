using System.Globalization;
using System.Text;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Utility;
using FixPortal.FixAtdl.Xml;

namespace FixPortal.FixAtdl.Tests.Fix;

/// <summary>
/// FIXatdl 1.1 section 4.5 StrategyParametersGrp (tags 957-960) conformance.
/// Mirrors the backend AtdlFixPreviewEmitter and React fixPreviewEmitter test suites exactly,
/// including the empty-group convention (omit 957 entirely rather than emit 957=0).
/// </summary>
public class StrategyParametersGrpEmitterTests
{
    private static Strategy_t Load()
    {
        using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(FixtureFiles.ReadAllText("Fixtures/Conformance/expressions.xml"))
        );
        return new StrategiesReader().Load(stream).Strategies[0];
    }

    [Fact]
    public void Omits_tag_957_entirely_when_no_parameters_are_set()
    {
        var strategy = Load();

        StrategyParametersGrpEmitter.Emit(strategy).Should().BeEmpty();
    }

    [Fact]
    public void Emits_957_then_958_959_960_for_a_single_set_parameter()
    {
        var strategy = Load();
        strategy.Parameters["Amount"].WireValue = "5";

        StrategyParametersGrpEmitter.Emit(strategy).Should().Equal((957, "1"), (958, "Amount"), (959, "1"), (960, "5"));
    }

    [Fact]
    public void Emits_parameters_in_declaration_order_when_multiple_are_set()
    {
        var strategy = Load();
        strategy.Parameters["OtherAmount"].WireValue = "1.5";
        strategy.Parameters["Text"].WireValue = "hello";

        var names = StrategyParametersGrpEmitter.Emit(strategy).Where(t => t.Tag == 958).Select(t => t.Value);

        // Declaration order in expressions.xml: Text, OtherText, Amount, OtherAmount, ...
        names.Should().Equal("Text", "OtherAmount");
    }

    [Fact]
    public void Omits_parameters_that_are_not_set()
    {
        var strategy = Load();
        strategy.Parameters["Text"].WireValue = "hello";

        StrategyParametersGrpEmitter
            .Emit(strategy)
            .Should()
            .Equal((957, "1"), (958, "Text"), (959, "14"), (960, "hello"));
    }

    [Theory]
    [InlineData("Text", "x", 14)]
    [InlineData("Amount", "5", 1)]
    [InlineData("OtherAmount", "1.5", 6)]
    [InlineData("Enabled", "T", 13)]
    [InlineData("Mode", "x", 12)]
    public void Resolves_the_FIX_type_code_for_each_parameter_type(
        string parameterName,
        string wireValue,
        int expectedCode
    )
    {
        var strategy = Load();
        strategy.Parameters[parameterName].WireValue = wireValue;

        StrategyParametersGrpEmitter
            .Emit(strategy)
            .Should()
            .ContainEquivalentOf((959, expectedCode.ToString(CultureInfo.InvariantCulture)));
    }
}
