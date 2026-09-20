using System.Globalization;
using System.Text;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Elements.Support;
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

    [Fact]
    public void Omits_a_parameter_whose_wire_value_is_empty()
    {
        // Low 6: an empty WireValue is filtered alongside a missing one, so no "960=" is emitted.
        var strategy = Load();
        var parameter = Substitute.For<IParameter>();
        parameter.Name.Returns("Empty");
        parameter.IsSet.Returns(true);
        parameter.WireValue.Returns("");
        strategy.Parameters.Add(parameter);

        StrategyParametersGrpEmitter.Emit(strategy).Should().BeEmpty();
    }

    [Theory]
    // Parameter names come from broker-supplied ATDL XML and nothing upstream constrains them the way
    // String_t constrains a value. A name or wire value carrying SOH splits one field into two and injects
    // arbitrary FIX fields once the host joins these tuples onto the wire.
    [InlineData("Injected\u0001100", "5", "tag 958")]
    [InlineData("Injected", "5\u0001100=1", "tag 960")]
    public void Rejects_a_parameter_whose_name_or_wire_value_contains_the_field_delimiter(
        string name,
        string wireValue,
        string expectedTagInMessage
    )
    {
        var strategy = Load();
        var parameter = Substitute.For<IParameter>();
        parameter.Name.Returns(name);
        parameter.IsSet.Returns(true);
        parameter.WireValue.Returns(wireValue);
        strategy.Parameters.Add(parameter);

        var act = () => StrategyParametersGrpEmitter.Emit(strategy);

        act.Should().Throw<InvalidOperationException>().WithMessage($"*{expectedTagInMessage}*");
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

    // R27: codes 25-29 read off the FIX 5.0 SP2 enumeration for tag 959 (QuickFIX/n data dictionary
    // spec XML, FIX50SP2.xml field 959); FIX 5.0/SP1 stop at 24 and FIX 4.4 has no tag 959 at all.
    [Theory]
    [InlineData("Country_t", 25)]
    [InlineData("Language_t", 26)]
    [InlineData("TZTimeOnly_t", 27)]
    [InlineData("TZTimestamp_t", 28)]
    [InlineData("Tenor_t", 29)]
    public void Resolves_codes_25_to_29_per_the_fix_5_0_sp2_enumeration(string typeName, int expectedCode)
    {
        FixStrategyParameterTypeCodes.Resolve(typeName).Should().Be(expectedCode);
    }

    [Theory]
    // Neither identifier names a type in the FIXatdl model; their old dedicated arms (26, 29) were
    // dead and collided with the SP2 codes for LANGUAGE and TENOR.
    [InlineData("NumInMsg_t")]
    [InlineData("XMLData_t")]
    [InlineData("CustomThing_t")]
    public void Unregistered_type_names_fall_back_to_string(string typeName)
    {
        FixStrategyParameterTypeCodes.Resolve(typeName).Should().Be(14);
    }
}
