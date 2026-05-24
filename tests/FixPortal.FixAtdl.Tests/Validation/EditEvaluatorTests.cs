using System.IO;
using System.Text;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Utility;
using FixPortal.FixAtdl.Xml;

namespace FixPortal.FixAtdl.Tests.Validation;

/// <summary>
/// Characterisation tests for Edit_t comparison operators.
/// Because Edit_t&lt;IParameter&gt; requires IResolvable.Resolve() to wire up the
/// field sources, each test loads the TWAP strategy and resolves the edit against
/// its ParameterCollection before calling Evaluate().
/// </summary>
public class EditEvaluatorTests
{
    private static FixPortal.FixAtdl.Model.Elements.Strategies_t Load(string xml)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return new StrategiesReader().Load(stream);
    }

    private static FixPortal.FixAtdl.Model.Elements.Strategy_t LoadTwap(string xml)
    {
        return Load(xml).Strategies[0];
    }

    [Theory]
    [InlineData(Operator_t.Equal, "100", "100", true)]
    [InlineData(Operator_t.Equal, "100", "101", false)]
    [InlineData(Operator_t.NotEqual, "100", "101", true)]
    [InlineData(Operator_t.GreaterThan, "101", "100", true)]
    [InlineData(Operator_t.LessThan, "99", "100", true)]
    [InlineData(Operator_t.GreaterThanOrEqual, "100", "100", true)]
    [InlineData(Operator_t.LessThanOrEqual, "100", "100", true)]
    public async Task Single_edit_on_parameter_evaluates_comparison_correctly(
        Operator_t op, string paramValue, string editValue, bool expected)
    {
        var xml = await File.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        var twap = LoadTwap(xml);

        // Wire up: set a numeric wire value on the Participation parameter (tag 7700).
        twap.Parameters["Participation"].WireValue = paramValue;

        var edit = new Edit_t<IParameter>
        {
            Field = "Participation",
            Operator = op,
            Value = editValue
        };

        // Resolve the edit against the strategy so _fieldSource is populated.
        ((IResolvable<Strategy_t, IParameter>)edit).Resolve(twap, twap.Parameters);

        edit.Evaluate();

        edit.CurrentState.Should().Be(expected);
    }
}

