using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Types;

namespace FixPortal.FixAtdl.Tests.Fix;

public class FixMessageTests
{
    private static string Soh => FixMessage.SOH.ToString();
    private static char Sep => FixMessage.Separator;

    // FixMessage constants ---------------------------------------------------

    [Fact]
    public void SOH_is_ascii_001()
    {
        FixMessage.SOH.Should().Be('\x01');
    }

    [Fact]
    public void Separator_is_equals_sign()
    {
        FixMessage.Separator.Should().Be('=');
    }

    // FixMessage default constructor -----------------------------------------

    [Fact]
    public void Default_constructor_creates_empty_dictionary()
    {
        var msg = new FixMessage();
        msg.Should().BeEmpty();
    }

    // FixMessage string constructor ------------------------------------------

    [Fact]
    public void String_constructor_parses_single_tag()
    {
        var msg = new FixMessage($"35{Sep}D{Soh}");
        msg.Should().ContainKey((FixField)35);
        msg[(FixField)35].Should().Be("D");
    }

    [Fact]
    public void String_constructor_parses_multiple_tags()
    {
        var msg = new FixMessage($"35{Sep}D{Soh}49{Sep}SENDER{Soh}");
        msg.Should().ContainKey((FixField)35);
        msg.Should().ContainKey((FixField)49);
        msg[(FixField)35].Should().Be("D");
        msg[(FixField)49].Should().Be("SENDER");
    }

    [Fact]
    public void String_constructor_throws_FixParseException_for_empty_string()
    {
        var act = () => new FixMessage(string.Empty);
        act.Should().Throw<FixParseException>();
    }

    [Fact]
    public void String_constructor_throws_FixParseException_for_null()
    {
        var act = () => new FixMessage(null!);
        act.Should().Throw<FixParseException>();
    }

    [Fact]
    public void String_constructor_throws_FixParseException_for_missing_separator()
    {
        // "35D" — no '=' separator in the pair
        var act = () => new FixMessage($"35D{Soh}");
        act.Should().Throw<FixParseException>();
    }

    [Fact]
    public void String_constructor_throws_FixParseException_for_a_doubled_soh()
    {
        // Low 5: an empty field between two SOH delimiters is rejected, not silently skipped.
        var act = () => new FixMessage($"35=D{Soh}{Soh}10=123{Soh}");
        act.Should().Throw<FixParseException>();
    }

    [Fact]
    public void String_constructor_throws_FixParseException_for_non_integer_tag()
    {
        var act = () => new FixMessage($"ABC{Sep}D{Soh}");
        act.Should().Throw<FixParseException>();
    }

    [Fact]
    public void String_constructor_throws_FixParseException_for_non_positive_tag()
    {
        // NOTE: negative tags are rejected during parse to prevent uint cast corruption (e.g. -1 → 4294967295)
        var act = () => new FixMessage($"-1{Sep}D{Soh}");
        act.Should().Throw<FixParseException>();
    }

    [Fact]
    public void String_constructor_throws_FixParseException_for_duplicate_tag()
    {
        var act = () => new FixMessage($"35{Sep}D{Soh}35{Sep}8{Soh}");
        act.Should().Throw<FixParseException>();
    }

    // FixMessage FixFields property ------------------------------------------

    [Fact]
    public void FixFields_returns_keys_of_parsed_message()
    {
        var msg = new FixMessage($"35{Sep}D{Soh}49{Sep}SENDER{Soh}");
        msg.FixFields.Should().Contain((FixField)35);
        msg.FixFields.Should().Contain((FixField)49);
    }

    // FixMessage ToFix -------------------------------------------------------

    [Fact]
    public void ToFix_round_trip_preserves_tag_value_pairs()
    {
        var original = new FixMessage($"35{Sep}D{Soh}49{Sep}SENDER{Soh}");
        var wire = original.ToFix();
        var reparsed = new FixMessage(wire);

        reparsed[(FixField)35].Should().Be("D");
        reparsed[(FixField)49].Should().Be("SENDER");
    }

    [Fact]
    public void ToFix_uses_soh_delimiter_and_equals_separator()
    {
        var msg = new FixMessage($"35{Sep}D{Soh}");
        var wire = msg.ToFix();

        wire.Should().Contain($"35{Sep}D{Soh}");
    }

    [Fact]
    public void ToFix_empty_message_returns_empty_string()
    {
        var msg = new FixMessage();
        msg.ToFix().Should().BeEmpty();
    }

    // FixFieldValueProvider --------------------------------------------------

    [Fact]
    public void FixFieldValueProvider_Empty_has_null_Parameters()
    {
        FixFieldValueProvider.Empty.Parameters.Should().BeNull();
    }

    [Fact]
    public void FixFieldValueProvider_Empty_FixValues_is_empty()
    {
        FixFieldValueProvider.Empty.FixValues.Should().BeEmpty();
    }

    [Fact]
    public void FixFieldValueProvider_TryGetValue_returns_false_when_no_provider()
    {
        // FixFieldValueProvider with null initialValueProvider → TryGetValue always returns false
        var provider = new FixFieldValueProvider(null, null);
        provider.TryGetValue("FIX_MsgType", out var v).Should().BeFalse();
        v.Should().BeNull();
    }

    [Fact]
    public void FixFieldValueProvider_TryGetValue_returns_true_when_provider_has_matching_field()
    {
        FixTagValuesCollection fixValues = [];
        fixValues.Add(35, "D");

        var initialProvider = Substitute.For<IInitialFixValueProvider>();
        initialProvider.InputFixValues.Returns(fixValues);

        var provider = new FixFieldValueProvider(initialProvider, null);

        provider.TryGetValue("FIX_MsgType", out var v).Should().BeTrue();
        v.Should().Be("D");
    }

    [Fact]
    public void FixFieldValueProvider_TryGetValue_returns_false_for_missing_field()
    {
        FixTagValuesCollection fixValues = [];
        // tag 35 not present
        var initialProvider = Substitute.For<IInitialFixValueProvider>();
        initialProvider.InputFixValues.Returns(fixValues);

        var provider = new FixFieldValueProvider(initialProvider, null);

        provider.TryGetValue("FIX_MsgType", out _).Should().BeFalse();
    }

    [Fact]
    public void FixFieldValueProvider_TryGetValue_with_targetParameterName_returns_value_when_no_parameters()
    {
        FixTagValuesCollection fixValues = [];
        fixValues.Add(35, "D");

        var initialProvider = Substitute.For<IInitialFixValueProvider>();
        initialProvider.InputFixValues.Returns(fixValues);

        // null ParameterCollection → falls through without enum lookup
        var provider = new FixFieldValueProvider(initialProvider, null);

        provider.TryGetValue("FIX_MsgType", "MsgType", out var v).Should().BeTrue();
        v.Should().Be("D");
    }

    [Fact]
    public void FixFieldValueProvider_TryGetValue_with_targetParameterName_passes_through_when_parameter_has_no_enum_pairs()
    {
        FixTagValuesCollection fixValues = [];
        fixValues.Add(35, "D");

        var initialProvider = Substitute.For<IInitialFixValueProvider>();
        initialProvider.InputFixValues.Returns(fixValues);

        // Build a minimal ParameterCollection with a plain String_t parameter (no enum pairs)
        ParameterCollection parameters = [new Parameter_t<String_t>("MsgType") { FixTag = 35 }];

        var provider = new FixFieldValueProvider(initialProvider, parameters);

        provider.TryGetValue("FIX_MsgType", "MsgType", out var v).Should().BeTrue();
        v.Should().Be("D");
    }

    [Fact]
    public void FixFieldValueProvider_FixValues_returns_provider_values_when_present()
    {
        FixTagValuesCollection fixValues = [];
        fixValues.Add(35, "D");

        var initialProvider = Substitute.For<IInitialFixValueProvider>();
        initialProvider.InputFixValues.Returns(fixValues);

        var provider = new FixFieldValueProvider(initialProvider, null);

        provider.FixValues.Should().NotBeEmpty();
    }

    [Fact]
    public void FixFieldValueProvider_Parameters_returns_supplied_parameter_collection()
    {
        ParameterCollection parameters = [new Parameter_t<String_t>("MsgType") { FixTag = 35 }];

        var provider = new FixFieldValueProvider(null, parameters);

        provider.Parameters.Should().BeSameAs(parameters);
    }

    // FixMessage.ToFix tag guard (M4) ----------------------------------------

    [Fact]
    public void ToFix_throws_for_non_positive_tag_injected_via_indexer()
    {
        // The inherited Dictionary surface lets a caller inject a non-positive tag that the string ctor
        // would have rejected; ToFix must refuse to serialize it rather than emit a (uint)-corrupted tag.
        var message = new FixMessage { [(FixField)(-1)] = "x" };

        Func<string> act = message.ToFix;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ToFix_throws_for_soh_in_value_injected_via_indexer()
    {
        var message = new FixMessage { [(FixField)35] = $"A{FixMessage.SOH}B" };

        Func<string> act = message.ToFix;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ToFix_throws_for_empty_value_injected_via_indexer()
    {
        // An empty value emits "tag=" + SOH - byte-for-byte the output the null guard exists to
        // prevent, and one this class's own parse constructor rejects.
        var message = new FixMessage { [(FixField)35] = "" };

        Func<string> act = message.ToFix;

        act.Should().Throw<InvalidOperationException>();
    }

    // FixFieldValueProvider percentage init-value scaling (H3) ----------------

    // FixFieldValueProvider Boolean_t declared-wire-mapping translation (R12) ------------------

    [Theory]
    [InlineData("1", "Y")]
    [InlineData("0", "N")]
    public void FixFieldValueProvider_decodes_declared_boolean_wire_mapping(string wireValue, string expected)
    {
        FixTagValuesCollection fixValues = [];
        fixValues.Add(35, wireValue);

        var initialProvider = Substitute.For<IInitialFixValueProvider>();
        initialProvider.InputFixValues.Returns(fixValues);

        var booleanParameter = new Parameter_t<Boolean_t>("Flag");
        booleanParameter.Value.TrueWireValue = "1";
        booleanParameter.Value.FalseWireValue = "0";

        ParameterCollection parameters = [booleanParameter];

        var provider = new FixFieldValueProvider(initialProvider, parameters);

        provider.TryGetValue("FIX_MsgType", "Flag", out var value).Should().BeTrue();
        value.Should().Be(expected);
    }

    [Fact]
    public void FixFieldValueProvider_reports_failure_for_an_unrecognised_boolean_wire_value()
    {
        FixTagValuesCollection fixValues = [];
        fixValues.Add(35, "X");

        var initialProvider = Substitute.For<IInitialFixValueProvider>();
        initialProvider.InputFixValues.Returns(fixValues);

        var booleanParameter = new Parameter_t<Boolean_t>("Flag");
        booleanParameter.Value.TrueWireValue = "1";
        booleanParameter.Value.FalseWireValue = "0";

        ParameterCollection parameters = [booleanParameter];

        var provider = new FixFieldValueProvider(initialProvider, parameters);

        provider.TryGetValue("FIX_MsgType", "Flag", out _).Should().BeFalse();
    }

    // FixMessage tag alphabet (R21) --------------------------------------------

    [Theory]
    [InlineData("+35")]
    [InlineData(" 35")]
    public void String_constructor_throws_FixParseException_for_signed_or_whitespace_padded_tag(string tagText)
    {
        // Digits only: a FIX tag carries no sign or whitespace (R21).
        var act = () => new FixMessage($"{tagText}{Sep}D{Soh}");

        act.Should().Throw<FixParseException>();
    }

    // FixFieldValueProvider percentage scaling: numeric alphabet + full precision (R21, R28, Low 3) -------

    [Theory]
    [InlineData("1,234.5")] // thousands separator is outside the FIX numeric alphabet (R21)
    [InlineData("1E2")] // exponent is outside it too - this must fail, not read as 100 (R21)
    [InlineData("79228162514264337593543950335")] // decimal.MaxValue: the x100 scale-up overflows (Low 3)
    public void FixFieldValueProvider_reports_failure_for_unscalable_percentage_wire_values(string wireValue)
    {
        FixTagValuesCollection fixValues = [];
        fixValues.Add(35, wireValue);

        var initialProvider = Substitute.For<IInitialFixValueProvider>();
        initialProvider.InputFixValues.Returns(fixValues);

        ParameterCollection parameters = [new Parameter_t<Percentage_t>("Pct")];

        var provider = new FixFieldValueProvider(initialProvider, parameters);

        provider.TryGetValue("FIX_MsgType", "Pct", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("0.12345678", "12.345678")] // full precision: no four-decimal-place cap (R28)
    [InlineData("0.5", "50")] // trailing zeros from the x100 multiplication are stripped
    [InlineData("0.75", "75")]
    public void FixFieldValueProvider_scales_percentage_wire_values_at_full_precision(string wireValue, string expected)
    {
        // The parameter's own Precision governs rounding on the way back out, not this lookup (R28).
        FixTagValuesCollection fixValues = [];
        fixValues.Add(35, wireValue);

        var initialProvider = Substitute.For<IInitialFixValueProvider>();
        initialProvider.InputFixValues.Returns(fixValues);

        ParameterCollection parameters = [new Parameter_t<Percentage_t>("Pct")];

        var provider = new FixFieldValueProvider(initialProvider, parameters);

        provider.TryGetValue("FIX_MsgType", "Pct", out var value).Should().BeTrue();
        value.Should().Be(expected);
    }
}
