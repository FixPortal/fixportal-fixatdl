using System.Globalization;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Controls.Support;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Types;
using FixPortal.FixAtdl.Model.Types.Support;
using FixPortal.FixAtdl.Validation;

namespace FixPortal.FixAtdl.Tests.Model.Types;

/// <summary>
/// Targets the specific coverage gaps that keep Model.Types.* below the 65 % floor:
///   • MultipleCharValue_t / MultipleStringValue_t — InvertOnWire property (0 % classes)
///   • String_t — SOH-delimiter rejection, ToEnumState
///   • EnumTypeBase — ToEnumState (Country_t as the concrete vehicle)
///   • NonNegativeIntegerTypeBase — ToEnumState (SeqNum_t as the concrete vehicle)
///   • AtdlReferenceType / AtdlValueType — GetNativeValue, SetValueFromControl paths
/// </summary>
public class TypeCoverageGapTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // Helper: build a minimal EnumPairCollection
    // ──────────────────────────────────────────────────────────────────────────

    private static EnumPairCollection BuildEnumPairs(params (string enumId, string wireValue)[] pairs)
    {
        var col = new EnumPairCollection();
        foreach (var (enumId, wireValue) in pairs)
        {
            col.Add(new EnumPair_t { EnumId = enumId, WireValue = wireValue });
        }
        return col;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // MultipleCharValue_t — InvertOnWire property (covers the 0% class)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void MultipleCharValue_t_InvertOnWire_defaults_to_null()
    {
        var p = new Parameter_t<MultipleCharValue_t>("Vals");
        p.Value.InvertOnWire.Should().BeNull();
    }

    [Fact]
    public void MultipleCharValue_t_InvertOnWire_can_be_set_true()
    {
        var p = new Parameter_t<MultipleCharValue_t>("Vals");
        p.Value.InvertOnWire = true;
        p.Value.InvertOnWire.Should().Be(true);
    }

    [Fact]
    public void MultipleCharValue_t_InvertOnWire_can_be_set_false()
    {
        var p = new Parameter_t<MultipleCharValue_t>("Vals");
        p.Value.InvertOnWire = false;
        p.Value.InvertOnWire.Should().Be(false);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // MultipleStringValue_t — InvertOnWire property (covers the 0% class)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void MultipleStringValue_t_InvertOnWire_defaults_to_null()
    {
        var p = new Parameter_t<MultipleStringValue_t>("Vals");
        p.Value.InvertOnWire.Should().BeNull();
    }

    [Fact]
    public void MultipleStringValue_t_InvertOnWire_can_be_set_true()
    {
        var p = new Parameter_t<MultipleStringValue_t>("Vals");
        p.Value.InvertOnWire = true;
        p.Value.InvertOnWire.Should().Be(true);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // String_t — SOH delimiter rejection (lines 62-63 in String_t.ValidateValue)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void String_t_rejects_value_containing_SOH_delimiter()
    {
        // NOTE: String_t.ValidateValue explicitly rejects strings containing the FIX field
        // delimiter (SOH, \x01). Accepting SOH would corrupt FIX framing on wire emission.
        // AtdlReferenceType.SetWireValue wraps validation failures as InvalidFieldValueException.
        var p = new Parameter_t<String_t>("Text");
        var act = () => p.WireValue = "bad\x01value";
        act.Should().Throw<InvalidFieldValueException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // String_t — ToEnumState (lines 161-172 in String_t / AtdlReferenceType)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void String_t_ToEnumState_returns_matching_enum_state_when_wire_value_found()
    {
        var p = new Parameter_t<String_t>("Side") { WireValue = "BUY" };
        var pairs = BuildEnumPairs(("Buy", "BUY"), ("Sell", "SELL"));
        var state = ((IControlConvertible)p.Value).ToEnumState(pairs);
        state["Buy"].Should().Be(true);
        state["Sell"].Should().Be(false);
    }

    [Fact]
    public void String_t_ToEnumState_with_null_value_returns_all_false_state()
    {
        // NOTE: When _value is null, ToEnumState returns a new EnumState initialised from
        // enumPairs.EnumIds — all entries are false (no value selected).
        var p = new Parameter_t<String_t>("Side");
        var pairs = BuildEnumPairs(("Buy", "BUY"), ("Sell", "SELL"));
        var state = ((IControlConvertible)p.Value).ToEnumState(pairs);
        state["Buy"].Should().Be(false);
        state["Sell"].Should().Be(false);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // EnumTypeBase — ToEnumState (lines 69-79 via Country_t)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Country_t_ToEnumState_returns_matching_enum_state()
    {
        // NOTE: EnumTypeBase.ToEnumState converts the stored enum value to a name via
        // Enum.GetName, then looks it up in the EnumPairCollection by wire value.
        // Country_t stores an IsoCountryCode enum; EnumPairCollection.WireValue
        // must match the enum member name exactly for TryParseWireValue to find it.
        var p = new Parameter_t<FixPortal.FixAtdl.Model.Types.Country_t>("Cty") { WireValue = "US" };
        // The stored enum name is the IsoCountryCode member name (e.g. "US").
        var pairs = BuildEnumPairs(("United States", "US"), ("Great Britain", "GB"));
        var state = ((IControlConvertible)p.Value).ToEnumState(pairs);
        // ToEnumState finds the EnumPair whose WireValue equals the Enum.GetName result ("US").
        state["United States"].Should().Be(true);
        state["Great Britain"].Should().Be(false);
    }

    [Fact]
    public void Country_t_ToEnumState_with_no_matching_wire_returns_all_false()
    {
        // When the stored enum name does not match any wire value, TryParseWireValue returns
        // false and all enum state entries remain false.
        var p = new Parameter_t<FixPortal.FixAtdl.Model.Types.Country_t>("Cty") { WireValue = "US" };
        var pairs = BuildEnumPairs(("SomeOther", "ZZ"));
        var state = ((IControlConvertible)p.Value).ToEnumState(pairs);
        state["SomeOther"].Should().Be(false);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // NonNegativeIntegerTypeBase — ToEnumState (lines 136-147 via SeqNum_t)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SeqNum_t_ToEnumState_returns_matching_enum_state()
    {
        // NOTE: NonNegativeIntegerTypeBase.ToEnumState formats the uint value as a string and
        // calls EnumPairCollection.TryParseWireValue with that string.
        var p = new Parameter_t<SeqNum_t>("Seq") { WireValue = "1" };
        var pairs = BuildEnumPairs(("One", "1"), ("Two", "2"));
        var state = ((IControlConvertible)p.Value).ToEnumState(pairs);
        state["One"].Should().Be(true);
        state["Two"].Should().Be(false);
    }

    [Fact]
    public void SeqNum_t_ToEnumState_with_null_value_returns_all_false()
    {
        // NOTE: When _value is null, ToEnumState returns a new EnumState with all false.
        var p = new Parameter_t<SeqNum_t>("Seq");
        var pairs = BuildEnumPairs(("One", "1"), ("Two", "2"));
        var state = ((IControlConvertible)p.Value).ToEnumState(pairs);
        state["One"].Should().Be(false);
        state["Two"].Should().Be(false);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // AtdlReferenceType — GetNativeValue (line 184-187 in AtdlReferenceType)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void String_t_GetNativeValue_returns_stored_string()
    {
        var p = new Parameter_t<String_t>("X") { WireValue = "hello" };
        p.Value.GetNativeValue(false).Should().Be("hello");
    }

    [Fact]
    public void String_t_GetNativeValue_returns_ConstValue_when_set()
    {
        var p = new Parameter_t<String_t>("X");
        p.Value.ConstValue = "CONST";
        p.Value.GetNativeValue(false).Should().Be("CONST");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // AtdlReferenceType — SetValueFromControl path (lines 76-102)
    // Uses a NSubstitute mock for IParameterConvertible.
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void String_t_SetValueFromControl_stores_converted_string()
    {
        // NOTE: SetValueFromControl calls ConvertToNativeType (String_t calls value.ToString(hostParameter)).
        // We supply a substitute that returns a known string.
        var p = new Parameter_t<String_t>("X");
        var convertible = Substitute.For<IParameterConvertible>();
        convertible.ToString(p).Returns("from-control");

        var result = p.Value.SetValueFromControl(p, convertible);

        result.IsValid.Should().BeTrue();
        p.WireValue.Should().Be("from-control");
    }

    [Fact]
    public void String_t_SetValueFromControl_blocks_when_ConstValue_is_set()
    {
        // NOTE: SetValueFromControl returns an Invalid ValidationResult (not a throw) when
        // ConstValue is already set. This matches AtdlReferenceType.SetValueFromControl line 77-80.
        var p = new Parameter_t<String_t>("X");
        p.Value.ConstValue = "CONST";
        var convertible = Substitute.For<IParameterConvertible>();
        convertible.ToString(p).Returns("other");

        var result = p.Value.SetValueFromControl(p, convertible);

        result.IsValid.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // AtdlValueType — SetValueFromControl path (lines 76-102 of AtdlValueType)
    // Uses a NSubstitute mock for IParameterConvertible.
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Int_t_SetValueFromControl_stores_converted_int()
    {
        // NOTE: SetValueFromControl calls ConvertToNativeType (Int_t calls value.ToInt32).
        var p = new Parameter_t<Int_t>("X");
        var convertible = Substitute.For<IParameterConvertible>();
        convertible.ToInt32(p, Arg.Any<IFormatProvider>()).Returns(42);

        var result = p.Value.SetValueFromControl(p, convertible);

        result.IsValid.Should().BeTrue();
        p.GetCurrentValue().Should().Be(42);
    }

    [Fact]
    public void Int_t_SetValueFromControl_blocks_when_ConstValue_is_set()
    {
        // NOTE: SetValueFromControl returns Invalid ValidationResult when ConstValue is set.
        var p = new Parameter_t<Int_t>("X");
        p.Value.ConstValue = 99;
        var convertible = Substitute.For<IParameterConvertible>();
        convertible.ToInt32(p, Arg.Any<IFormatProvider>()).Returns(1);

        var result = p.Value.SetValueFromControl(p, convertible);

        result.IsValid.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // AtdlValueType — GetNativeValue (line ~186 of AtdlValueType)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Int_t_GetNativeValue_returns_stored_int()
    {
        var p = new Parameter_t<Int_t>("X") { WireValue = "7" };
        p.Value.GetNativeValue(false).Should().Be(7);
    }

    [Fact]
    public void Int_t_GetNativeValue_returns_ConstValue_when_set()
    {
        var p = new Parameter_t<Int_t>("X");
        p.Value.ConstValue = 55;
        p.Value.GetNativeValue(false).Should().Be(55);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // NonNegativeIntegerTypeBase — ConvertToNativeType via SetValueFromControl
    // (lines 74-77 of NonNegativeIntegerTypeBase)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SeqNum_t_SetValueFromControl_stores_converted_uint()
    {
        // NOTE: NonNegativeIntegerTypeBase.ConvertToNativeType calls value.ToUInt32.
        var p = new Parameter_t<SeqNum_t>("X");
        var convertible = Substitute.For<IParameterConvertible>();
        convertible.ToUInt32(p, Arg.Any<IFormatProvider>()).Returns((uint?)5u);

        var result = p.Value.SetValueFromControl(p, convertible);

        result.IsValid.Should().BeTrue();
        p.GetCurrentValue().Should().Be(5u);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UTCTimestamp_t — MinValue validation (line not previously covered)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void UTCTimestamp_t_rejects_value_below_MinValue()
    {
        var p = new Parameter_t<UTCTimestamp_t>("X");
        p.Value.MinValue = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var act = () => p.WireValue = "20260101-00:00:00";
        act.Should().Throw<InvalidFieldValueException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // EnumTypeBase — ToString returns enum member name (line 40 of EnumTypeBase)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Country_t_IControlConvertible_ToString_returns_enum_member_name()
    {
        // NOTE: EnumTypeBase<T>.ToString(provider) returns Enum.GetName(typeof(T), value).
        // For Country_t, that is the IsoCountryCode member name (e.g. "US" not "United States").
        var p = new Parameter_t<FixPortal.FixAtdl.Model.Types.Country_t>("X") { WireValue = "US" };
        var cc = (IControlConvertible)p.Value;
        // Enum.GetName returns the member name as declared in the IsoCountryCode enum.
        // We pin the actual behaviour: the return value matches the enum identifier for "US".
        cc.ToString(null).Should().Be("US");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // AtdlReferenceType — GetWireValue throws when required and value is null
    // (lines 162-169 — the IsMissing branch)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void String_t_GetWireValue_throws_MissingMandatoryValueException_when_required_and_null()
    {
        // NOTE: AtdlReferenceType.GetWireValue checks ValidateValue. When Use = Required and
        // no value is set, it throws MissingMandatoryValueException (not InvalidFieldValueException).
        var p = new Parameter_t<String_t>("X") { Use = FixPortal.FixAtdl.Model.Enumerations.Use_t.Required };
        var act = () => _ = p.WireValue;
        act.Should().Throw<MissingMandatoryValueException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Characterization — batch-3 finding F7 (CLOSED)
    // ──────────────────────────────────────────────────────────────────────────

    // Characterization (batch-3 finding F7, deliberately CLOSED — see docs/batch-3-findings-disposition.md):
    // TagNum_t parses leading-zero wire values to their numeric value (no leading-zero rejection).
    // Rejecting them is enforcement-tightening, not a correctness fix; the parsed value is correct.
    [Fact]
    public void TagNum_t_parses_leading_zero_wire_value_to_numeric_value()
    {
        var parameter = new Parameter_t<TagNum_t>("p") { WireValue = "0044" };
        parameter.GetCurrentValue().Should().Be(44u);
    }
}
