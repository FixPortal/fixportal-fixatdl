using System.Text;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Xml;

namespace FixPortal.FixAtdl.Tests.Model;

public sealed class StrategyWrapperTests
{
    private const string FixEdits = """
        <val:StrategyEdit errorMessage="Quantity"><val:Edit field="Qty" operator="EQ" field2="FIX_OrderQty" /></val:StrategyEdit>
        <val:StrategyEdit errorMessage="Other"><val:Edit field="Other" operator="EQ" field2="FIX_MaxFloor" /></val:StrategyEdit>
        """;

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EvaluateAllStrategyEdits_uses_the_supplied_FIX_values_for_the_loaded_parameter_comparison(bool matches)
    {
        var strategy = Load(FixEdits);
        strategy.Parameters["Qty"].WireValue = "10";
        strategy.Parameters["Other"].WireValue = "20";
        var input = new Input();
        input.InputFixValues.Add(38, matches ? "10" : "11");
        input.InputFixValues.Add(111, "20");

        strategy.EvaluateAllStrategyEdits(input, shortCircuit: false).Should().Be(matches);

        strategy.StrategyEdits[0].CurrentState.Should().Be(matches);
        strategy.StrategyEdits[1].CurrentState.Should().BeTrue();
    }

    [Fact]
    public void EvaluateAllStrategyEdits_supports_a_null_provider_with_real_parameter_and_missing_FIX_edits()
    {
        var strategy = Load(
            """
            <val:StrategyEdit errorMessage="Quantity"><val:Edit field="Qty" operator="EQ" value="10" /></val:StrategyEdit>
            <val:StrategyEdit errorMessage="Missing"><val:Edit field="FIX_OrderQty" operator="NX" /></val:StrategyEdit>
            """
        );
        strategy.Parameters["Qty"].WireValue = "10";
        var result = false;
        var evaluate = () =>
        {
            result = strategy.EvaluateAllStrategyEdits(null!, shortCircuit: false);
        };

        evaluate.Should().NotThrow("a null host provider is supported through the empty FIX provider");
        result.Should().BeTrue();
        strategy.StrategyEdits[1].CurrentState.Should().BeTrue();
        strategy.Parameters["Qty"].WireValue = "11";
        strategy.EvaluateAllStrategyEdits(null!, shortCircuit: false).Should().BeFalse();
        strategy.StrategyEdits[0].CurrentState.Should().BeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EvaluateAllStrategyEdits_forwards_shortCircuit_to_actual_loaded_edits(bool shortCircuit)
    {
        var strategy = Load(FixEdits);
        var input = new Input();
        input.InputFixValues.Add(38, "10");
        input.InputFixValues.Add(111, "20");
        strategy.Parameters["Qty"].WireValue = "10";
        strategy.Parameters["Other"].WireValue = "20";
        strategy.EvaluateAllStrategyEdits(input, shortCircuit: false).Should().BeTrue();
        strategy.StrategyEdits[1].CurrentState.Should().BeTrue("the second edit must have a witnessed prior state");
        strategy.Parameters["Qty"].WireValue = "11";
        strategy.Parameters["Other"].WireValue = "21";

        strategy.EvaluateAllStrategyEdits(input, shortCircuit).Should().BeFalse();

        strategy.StrategyEdits[0].CurrentState.Should().BeFalse();
        strategy
            .StrategyEdits[1]
            .CurrentState.Should()
            .Be(shortCircuit, "only short-circuiting preserves its prior true state");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LoadInitialControlValues_forwards_the_real_provider_and_preserves_authored_fallbacks(bool supplied)
    {
        var strategy = Load("");
        var input = new Input();
        if (supplied)
        {
            input.InputFixValues.Add(38, "42");
            input.InputFixValues.Add(1, "YES");
        }
        var provider = new FixFieldValueProvider(input, strategy.Parameters);

        strategy.LoadInitialControlValues(provider);

        strategy.Controls["qty"].GetCurrentValue().Should().Be(supplied ? 42m : 7m);
        strategy.Controls["flag"].GetCurrentValue().Should().Be(supplied);
        strategy.Controls["text"].GetCurrentValue().Should().Be("authored fallback");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LoadParameterValues_forwards_values_and_the_reset_policy(bool reset)
    {
        var strategy = Load("");
        strategy.Parameters["Qty"].WireValue = "10";
        strategy.Parameters["Other"].WireValue = "20";
        var input = new Input();
        input.InputFixValues.Add(38, "42");

        strategy.LoadParameterValues(new FixFieldValueProvider(input, strategy.Parameters), reset);

        strategy.Parameters["Qty"].WireValue.Should().Be("42");
        strategy.Parameters["Other"].WireValue.Should().Be(reset ? null : "20");
    }

    private sealed class Input : IInitialFixValueProvider
    {
        public FixTagValuesCollection InputFixValues { get; } = [];
    }

    private static Strategy_t Load(string edits)
    {
        var xml = $$"""
            <Strategies xmlns="http://www.fixprotocol.org/FIXatdl-1-1/Core"
                        xmlns:val="http://www.fixprotocol.org/FIXatdl-1-1/Validation"
                        xmlns:lay="http://www.fixprotocol.org/FIXatdl-1-1/Layout"
                        xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" strategyIdentifierTag="5001">
              <Strategy name="Wrappers" version="1" wireValue="W" uiRep="Wrappers" providerID="TEST">
                <Parameter name="Qty" xsi:type="Int_t" fixTag="38" />
                <Parameter name="Other" xsi:type="Int_t" fixTag="9001" />
                <Parameter name="Flag" xsi:type="Boolean_t" fixTag="7001" trueWireValue="YES" falseWireValue="NO" />
                <lay:StrategyLayout><lay:StrategyPanel>
                  <lay:Control ID="qty" xsi:type="lay:SingleSpinner_t" parameterRef="Qty" initValue="7" initPolicy="UseFixField" initFixField="FIX_OrderQty" />
                  <lay:Control ID="flag" xsi:type="lay:CheckBox_t" parameterRef="Flag" initValue="false" initPolicy="UseFixField" initFixField="FIX_Account" />
                  <lay:Control ID="text" xsi:type="lay:TextField_t" initValue="authored fallback" />
                </lay:StrategyPanel></lay:StrategyLayout>
                {{edits}}
              </Strategy>
            </Strategies>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return new StrategiesReader().Load(stream).Strategies[0];
    }
}
