using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Types;
using Country_t = FixPortal.FixAtdl.Model.Types.Country_t;

namespace FixPortal.FixAtdl.Tests.Model.Types;

/// <summary>
/// Characterizes the string-wire ⇄ native round-trip for the scalar FIXatdl value types,
/// exercised through Parameter_t&lt;T&gt; (WireValue set/get drives ConvertFrom/ConvertToWireValueFormat).
/// </summary>
public class ValueTypeConversionTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // Int_t
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("100", 100)]
    [InlineData("-5", -5)]
    [InlineData("0", 0)]
    public void Int_t_round_trips_valid_wire_values(string wire, int expected)
    {
        var p = new Parameter_t<Int_t>("Qty") { WireValue = wire };
        p.GetCurrentValue().Should().Be(expected);
        p.WireValue.Should().Be(wire);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("9999999999999999999999")]
    public void Int_t_rejects_unparseable_wire_value(string bad)
    {
        var p = new Parameter_t<Int_t>("Qty");
        var act = () => p.WireValue = bad;
        act.Should().Throw<InvalidFieldValueException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Boolean_t
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Y", true)]
    [InlineData("N", false)]
    public void Boolean_t_maps_default_wire_tokens(string wire, bool expected)
    {
        var p = new Parameter_t<Boolean_t>("Flag") { WireValue = wire };
        p.GetCurrentValue().Should().Be(expected);
    }

    [Fact]
    public void Boolean_t_rejects_unknown_token()
    {
        var p = new Parameter_t<Boolean_t>("Flag");
        var act = () => p.WireValue = "X";
        act.Should().Throw<InvalidFieldValueException>();
    }

    [Fact]
    public void Boolean_t_roundtrips_custom_true_and_false_wire_values()
    {
        // NOTE: TrueWireValue/FalseWireValue are properties on the type instance inside Parameter_t.
        // We access the inner type via the Value property (the typed inner instance).
        var p = new Parameter_t<Boolean_t>("Flag");
        p.Value.TrueWireValue = "1";
        p.Value.FalseWireValue = "0";

        p.WireValue = "1";
        p.GetCurrentValue().Should().Be(true);
        p.WireValue.Should().Be("1");

        p.WireValue = "0";
        p.GetCurrentValue().Should().Be(false);
        p.WireValue.Should().Be("0");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Char_t
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Char_t_requires_exactly_one_character()
    {
        // NOTE: Char_t.ConvertFromWireValueFormat throws ArgumentException, which is caught and
        // translated by AtdlValueType.SetWireValue to InvalidFieldValueException.
        var p = new Parameter_t<Char_t>("Side");
        var act = () => p.WireValue = "AB";
        act.Should().Throw<InvalidFieldValueException>();
    }

    [Theory]
    [InlineData("1")]
    [InlineData("A")]
    [InlineData("Z")]
    public void Char_t_round_trips_single_char(string wire)
    {
        var p = new Parameter_t<Char_t>("Side") { WireValue = wire };
        p.WireValue.Should().Be(wire);
        p.GetCurrentValue().Should().Be(wire[0]);
    }

    [Fact]
    public void Char_t_rejects_empty_string()
    {
        var p = new Parameter_t<Char_t>("Side");
        var act = () => p.WireValue = "";
        act.Should().Throw<InvalidFieldValueException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Float_t
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("123.45")]
    [InlineData("0.1")]
    [InlineData("-99.9")]
    public void Float_t_round_trips_decimals(string wire)
    {
        var p = new Parameter_t<Float_t>("Px") { WireValue = wire };
        p.WireValue.Should().Be(wire);
    }

    [Fact]
    public void Float_t_rejects_non_numeric_wire_value()
    {
        var p = new Parameter_t<Float_t>("Px");
        var act = () => p.WireValue = "notanumber";
        act.Should().Throw<InvalidFieldValueException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Amt_t (inherits Float_t with no overrides)
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("1000.00")]
    [InlineData("0.01")]
    public void Amt_t_round_trips_decimals(string wire)
    {
        var p = new Parameter_t<Amt_t>("Amount") { WireValue = wire };
        p.WireValue.Should().Be(wire);
    }

    [Fact]
    public void Amt_t_rejects_non_numeric_wire_value()
    {
        var p = new Parameter_t<Amt_t>("Amount");
        var act = () => p.WireValue = "abc";
        act.Should().Throw<InvalidFieldValueException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Percentage_t  (inherits Float_t; ConvertFromWire divides by 100 if MultiplyBy100)
    // NOTE: Without MultiplyBy100, wire "0.75" round-trips as "0.75".
    // With MultiplyBy100, wire "75" is stored as 0.75 internally and emitted as "75".
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Percentage_t_round_trips_fraction_wire_value()
    {
        var p = new Parameter_t<Percentage_t>("Pct") { WireValue = "0.75" };
        p.WireValue.Should().Be("0.75");
    }

    [Fact]
    public void Percentage_t_with_MultiplyBy100_round_trips_whole_number()
    {
        var p = new Parameter_t<Percentage_t>("Pct");
        p.Value.MultiplyBy100 = true;
        p.WireValue = "75";
        // NOTE: MultiplyBy100 divides by 100 on input (0.75 stored), then multiplies back on output.
        p.WireValue.Should().Be("75");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Qty_t (inherits Float_t)
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("500")]
    [InlineData("1000.5")]
    public void Qty_t_round_trips_quantity(string wire)
    {
        var p = new Parameter_t<Qty_t>("Qty") { WireValue = wire };
        p.WireValue.Should().Be(wire);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Price_t (inherits Float_t)
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("100.25")]
    [InlineData("0.001")]
    public void Price_t_round_trips_price(string wire)
    {
        var p = new Parameter_t<Price_t>("Price") { WireValue = wire };
        p.WireValue.Should().Be(wire);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PriceOffset_t (inherits Float_t)
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("0.5")]
    [InlineData("-0.25")]
    public void PriceOffset_t_round_trips_offset(string wire)
    {
        var p = new Parameter_t<PriceOffset_t>("Offset") { WireValue = wire };
        p.WireValue.Should().Be(wire);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // SeqNum_t (NonZeroPositiveIntegerTypeBase → uint > 0)
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("1", 1u)]
    [InlineData("9999", 9999u)]
    public void SeqNum_t_round_trips_positive_integers(string wire, uint expected)
    {
        var p = new Parameter_t<SeqNum_t>("SeqNum") { WireValue = wire };
        p.GetCurrentValue().Should().Be(expected);
        p.WireValue.Should().Be(wire);
    }

    [Fact]
    public void SeqNum_t_rejects_zero()
    {
        var p = new Parameter_t<SeqNum_t>("SeqNum");
        var act = () => p.WireValue = "0";
        act.Should().Throw<InvalidFieldValueException>();
    }

    [Fact]
    public void SeqNum_t_rejects_non_numeric()
    {
        var p = new Parameter_t<SeqNum_t>("SeqNum");
        var act = () => p.WireValue = "abc";
        act.Should().Throw<InvalidFieldValueException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // NumInGroup_t (NonZeroPositiveIntegerTypeBase → uint > 0)
    // NOTE: Despite the name, NumInGroup_t inherits NonZeroPositiveIntegerTypeBase,
    // so zero is rejected (FIX spec says "value must be positive").
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("1", 1u)]
    [InlineData("5", 5u)]
    public void NumInGroup_t_round_trips_positive_integers(string wire, uint expected)
    {
        var p = new Parameter_t<NumInGroup_t>("Count") { WireValue = wire };
        p.GetCurrentValue().Should().Be(expected);
        p.WireValue.Should().Be(wire);
    }

    [Fact]
    public void NumInGroup_t_rejects_zero()
    {
        // NOTE: NumInGroup_t inherits NonZeroPositiveIntegerTypeBase, so zero is invalid.
        var p = new Parameter_t<NumInGroup_t>("Count");
        var act = () => p.WireValue = "0";
        act.Should().Throw<InvalidFieldValueException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Length_t (NonZeroPositiveIntegerTypeBase → uint > 0)
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("1", 1u)]
    [InlineData("256", 256u)]
    public void Length_t_round_trips_positive_integers(string wire, uint expected)
    {
        var p = new Parameter_t<Length_t>("Len") { WireValue = wire };
        p.GetCurrentValue().Should().Be(expected);
        p.WireValue.Should().Be(wire);
    }

    [Fact]
    public void Length_t_rejects_zero()
    {
        var p = new Parameter_t<Length_t>("Len");
        var act = () => p.WireValue = "0";
        act.Should().Throw<InvalidFieldValueException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // TagNum_t (NonZeroPositiveIntegerTypeBase → uint > 0)
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("35", 35u)]
    [InlineData("8", 8u)]
    public void TagNum_t_round_trips_tag_numbers(string wire, uint expected)
    {
        var p = new Parameter_t<TagNum_t>("Tag") { WireValue = wire };
        p.GetCurrentValue().Should().Be(expected);
        p.WireValue.Should().Be(wire);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // String_t (identity / empty→omitted)
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("hello", "hello")]
    [InlineData("FIX", "FIX")]
    public void String_t_round_trips_non_empty_values(string wire, string expected)
    {
        var p = new Parameter_t<String_t>("Text") { WireValue = wire };
        p.GetCurrentValue().Should().Be(expected);
        p.WireValue.Should().Be(wire);
    }

    [Fact]
    public void String_t_empty_input_returns_null_wire_value()
    {
        // NOTE: String_t.ConvertToWireValueFormat maps empty → null (omitted from FIX message).
        var p = new Parameter_t<String_t>("Text") { WireValue = "" };
        // WireValue getter calls ConvertToWireValueFormat which maps empty → null.
        p.WireValue.Should().BeNull();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Exchange_t (String_t subtype; validates 4-char MIC codes)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Exchange_t_round_trips_valid_MIC()
    {
        var p = new Parameter_t<Exchange_t>("Exch") { WireValue = "XNAS" };
        p.WireValue.Should().Be("XNAS");
    }

    [Fact]
    public void Exchange_t_rejects_code_shorter_than_4_chars()
    {
        var p = new Parameter_t<Exchange_t>("Exch");
        var act = () => p.WireValue = "NY";
        act.Should().Throw<InvalidFieldValueException>();
    }

    [Fact]
    public void Exchange_t_rejects_SOH_delimiter()
    {
        var p = new Parameter_t<Exchange_t>("Exch");
        var act = () => p.WireValue = "A" + FixPortal.FixAtdl.Fix.FixMessage.SOH + "BC";
        act.Should().Throw<InvalidFieldValueException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // MultipleCharValue_t / MultipleStringValue_t (both subclass String_t)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void MultipleCharValue_t_round_trips_space_delimited_chars()
    {
        var p = new Parameter_t<MultipleCharValue_t>("Vals") { WireValue = "2 A F" };
        p.WireValue.Should().Be("2 A F");
    }

    [Fact]
    public void MultipleStringValue_t_round_trips_space_delimited_strings()
    {
        var p = new Parameter_t<MultipleStringValue_t>("Vals") { WireValue = "AV AN A" };
        p.WireValue.Should().Be("AV AN A");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Country_t (IsoCountryCode enum-backed)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Country_t_round_trips_valid_iso_country_code()
    {
        var p = new Parameter_t<Country_t>("Cty") { WireValue = "US" };
        p.WireValue.Should().Be("US");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Currency_t (IsoCurrencyCode enum-backed)
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("USD")]
    [InlineData("GBP")]
    [InlineData("EUR")]
    public void Currency_t_round_trips_valid_iso_currency_codes(string wire)
    {
        var p = new Parameter_t<Currency_t>("Ccy") { WireValue = wire };
        p.WireValue.Should().Be(wire);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Language_t (IsoLanguageCode enum-backed)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Language_t_round_trips_valid_iso_language_code()
    {
        var p = new Parameter_t<Language_t>("Lang") { WireValue = "EN" };
        p.WireValue.Should().Be("EN");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Data_t (raw char[] round-trip)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Data_t_round_trips_raw_data()
    {
        var p = new Parameter_t<Data_t>("Raw") { WireValue = "hello world" };
        p.WireValue.Should().Be("hello world");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // MonthYear_t (wraps MonthYear struct via Parameter_t)
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("202601")]
    [InlineData("20260115")]
    public void MonthYear_t_round_trips_valid_formats(string wire)
    {
        var p = new Parameter_t<MonthYear_t>("Expiry") { WireValue = wire };
        p.WireValue.Should().Be(wire);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2026")]
    [InlineData("202613")]
    public void MonthYear_t_rejects_invalid_wire_values(string bad)
    {
        // NOTE: MonthYear.Parse throws ArgumentException, which is caught and translated
        // by AtdlValueType.SetWireValue to InvalidFieldValueException.
        var p = new Parameter_t<MonthYear_t>("Expiry");
        var act = () => p.WireValue = bad;
        act.Should().Throw<InvalidFieldValueException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Tenor_t (wraps Tenor struct via Parameter_t)
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("D5")]
    [InlineData("W13")]
    [InlineData("M3")]
    [InlineData("Y1")]
    public void Tenor_t_round_trips_valid_tenors(string wire)
    {
        var p = new Parameter_t<Tenor_t>("Tenor") { WireValue = wire };
        p.WireValue.Should().Be(wire);
    }

    [Theory]
    [InlineData("")]
    [InlineData("X5")]
    public void Tenor_t_rejects_invalid_wire_values(string bad)
    {
        // NOTE: Tenor.Parse throws ArgumentException, which is caught and translated
        // by AtdlValueType.SetWireValue to InvalidFieldValueException.
        var p = new Parameter_t<Tenor_t>("Tenor");
        var act = () => p.WireValue = bad;
        act.Should().Throw<InvalidFieldValueException>();
    }
}
