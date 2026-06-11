using System.Globalization;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Types;
using FixPortal.FixAtdl.Model.Types.Support;
using Country_t = FixPortal.FixAtdl.Model.Types.Country_t;

namespace FixPortal.FixAtdl.Tests.Model.Types;

/// <summary>
/// Characterizes shared Parameter_t features (ConstValue, Reset, IsSet, Min/MaxValue validation)
/// and the IControlConvertible interface methods on each type.
/// </summary>
public class ParameterTypeFeatureTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // IsSet / Reset shared contract
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void IsSet_is_false_on_new_parameter()
        => new Parameter_t<Int_t>("X").IsSet.Should().BeFalse();

    [Fact]
    public void IsSet_is_true_after_WireValue_set()
    {
        var p = new Parameter_t<Int_t>("X") { WireValue = "5" };
        p.IsSet.Should().BeTrue();
    }

    [Fact]
    public void Reset_clears_value()
    {
        var p = new Parameter_t<Int_t>("X") { WireValue = "5" };
        p.Reset();
        p.IsSet.Should().BeFalse();
    }

    [Fact]
    public void String_t_IsSet_is_true_after_WireValue_set()
    {
        var p = new Parameter_t<String_t>("X") { WireValue = "hello" };
        p.IsSet.Should().BeTrue();
    }

    [Fact]
    public void String_t_Reset_clears_value()
    {
        var p = new Parameter_t<String_t>("X") { WireValue = "hello" };
        p.Reset();
        p.IsSet.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // ConstValue (AtdlValueType)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ConstValue_is_returned_by_GetCurrentValue()
    {
        var p = new Parameter_t<Int_t>("X");
        p.Value.ConstValue = 42;
        p.GetCurrentValue().Should().Be(42);
    }

    [Fact]
    public void ConstValue_is_returned_as_WireValue()
    {
        var p = new Parameter_t<Int_t>("X");
        p.Value.ConstValue = 99;
        p.WireValue.Should().Be("99");
    }

    [Fact]
    public void Setting_WireValue_equal_to_ConstValue_is_allowed()
    {
        var p = new Parameter_t<Int_t>("X");
        p.Value.ConstValue = 99;
        var act = () => p.WireValue = "99";
        act.Should().NotThrow();
    }

    [Fact]
    public void Setting_WireValue_different_from_ConstValue_throws()
    {
        var p = new Parameter_t<Int_t>("X");
        p.Value.ConstValue = 99;
        var act = () => p.WireValue = "100";
        act.Should().Throw<InvalidOperationException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // ConstValue (AtdlReferenceType — String_t)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void String_t_ConstValue_is_returned_as_WireValue()
    {
        var p = new Parameter_t<String_t>("X");
        p.Value.ConstValue = "CONST";
        p.WireValue.Should().Be("CONST");
    }

    [Fact]
    public void String_t_setting_WireValue_different_from_ConstValue_throws()
    {
        var p = new Parameter_t<String_t>("X");
        p.Value.ConstValue = "CONST";
        var act = () => p.WireValue = "OTHER";
        act.Should().Throw<InvalidOperationException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Int_t MinValue / MaxValue validation
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Int_t_rejects_value_exceeding_MaxValue()
    {
        var p = new Parameter_t<Int_t>("X");
        p.Value.MaxValue = 10;
        var act = () => p.WireValue = "11";
        act.Should().Throw<InvalidFieldValueException>();
    }

    [Fact]
    public void Int_t_rejects_value_below_MinValue()
    {
        var p = new Parameter_t<Int_t>("X");
        p.Value.MinValue = 5;
        var act = () => p.WireValue = "4";
        act.Should().Throw<InvalidFieldValueException>();
    }

    [Fact]
    public void Int_t_accepts_value_at_MaxValue_boundary()
    {
        var p = new Parameter_t<Int_t>("X");
        p.Value.MaxValue = 10;
        var act = () => p.WireValue = "10";
        act.Should().NotThrow();
    }

    [Fact]
    public void Int_t_accepts_value_at_MinValue_boundary()
    {
        var p = new Parameter_t<Int_t>("X");
        p.Value.MinValue = 5;
        var act = () => p.WireValue = "5";
        act.Should().NotThrow();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Float_t MinValue / MaxValue / Precision
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Float_t_rejects_value_exceeding_MaxValue()
    {
        var p = new Parameter_t<Float_t>("X");
        p.Value.MaxValue = 100m;
        var act = () => p.WireValue = "100.01";
        act.Should().Throw<InvalidFieldValueException>();
    }

    [Fact]
    public void Float_t_rejects_value_below_MinValue()
    {
        var p = new Parameter_t<Float_t>("X");
        p.Value.MinValue = 0m;
        var act = () => p.WireValue = "-0.01";
        act.Should().Throw<InvalidFieldValueException>();
    }

    [Fact]
    public void Float_t_with_Precision_rounds_on_output()
    {
        var p = new Parameter_t<Float_t>("X");
        p.Value.Precision = 2;
        p.WireValue = "1.23456";
        // NOTE: Precision applies on ConvertToWireValueFormat. 1.23456 rounds to 1.23 at 2dp.
        p.WireValue.Should().Be("1.23");
    }

    // Pins MidpointRounding.AwayFromZero on Precision output: 2.5 -> 3 (not banker's 2),
    // -2.5 -> -3, 2.345 @2dp -> 2.35. Characterization for batch 5 M4 / S2325.
    [Theory]
    [InlineData("2.5", 0, "3")]
    [InlineData("-2.5", 0, "-3")]
    [InlineData("2.345", 2, "2.35")]
    [InlineData("2.344", 2, "2.34")]
    public void Float_t_Precision_rounds_away_from_zero(string input, int precision, string expected)
    {
        var p = new Parameter_t<Float_t>("X");
        p.Value.Precision = precision;
        p.WireValue = input;

        p.WireValue.Should().Be(expected);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IControlConvertible — Int_t
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Int_t_IControlConvertible_ToDecimal_returns_int_as_decimal()
    {
        var p = new Parameter_t<Int_t>("X") { WireValue = "42" };
        var cc = p.Value;
        cc.ToDecimal().Should().Be(42m);
    }

    [Fact]
    public void Int_t_IControlConvertible_ToString_returns_formatted_int()
    {
        var p = new Parameter_t<Int_t>("X") { WireValue = "42" };
        var cc = p.Value;
        cc.ToString(CultureInfo.InvariantCulture).Should().Be("42");
    }

    [Fact]
    public void Int_t_IControlConvertible_ToBoolean_throws()
    {
        var p = new Parameter_t<Int_t>("X") { WireValue = "1" };
        var cc = p.Value;
        var act = cc.ToBoolean;
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void Int_t_IControlConvertible_ToDateTime_throws()
    {
        var p = new Parameter_t<Int_t>("X") { WireValue = "1" };
        var cc = p.Value;
        var act = cc.ToDateTime;
        act.Should().Throw<InvalidCastException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IControlConvertible — Float_t
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Float_t_IControlConvertible_ToDecimal_returns_decimal()
    {
        var p = new Parameter_t<Float_t>("X") { WireValue = "3.14" };
        var cc = p.Value;
        cc.ToDecimal().Should().Be(3.14m);
    }

    [Fact]
    public void Float_t_IControlConvertible_ToBoolean_throws()
    {
        var p = new Parameter_t<Float_t>("X") { WireValue = "1.0" };
        var cc = p.Value;
        var act = cc.ToBoolean;
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void Float_t_IControlConvertible_ToDateTime_throws()
    {
        var p = new Parameter_t<Float_t>("X") { WireValue = "1.0" };
        var cc = p.Value;
        var act = cc.ToDateTime;
        act.Should().Throw<InvalidCastException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IControlConvertible — Boolean_t
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Boolean_t_IControlConvertible_ToBoolean_returns_bool()
    {
        var p = new Parameter_t<Boolean_t>("X") { WireValue = "Y" };
        var cc = p.Value;
        cc.ToBoolean().Should().Be(true);
    }

    [Fact]
    public void Boolean_t_IControlConvertible_ToDecimal_throws()
    {
        var p = new Parameter_t<Boolean_t>("X") { WireValue = "Y" };
        var cc = p.Value;
        var act = cc.ToDecimal;
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void Boolean_t_IControlConvertible_ToDateTime_throws()
    {
        var p = new Parameter_t<Boolean_t>("X") { WireValue = "Y" };
        var cc = p.Value;
        var act = cc.ToDateTime;
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void Boolean_t_IControlConvertible_ToString_returns_wire_value()
    {
        var p = new Parameter_t<Boolean_t>("X") { WireValue = "Y" };
        var cc = p.Value;
        // NOTE: Boolean_t.ToString(provider) returns the wire value "Y"/"N".
        cc.ToString(null).Should().Be("Y");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IControlConvertible — Char_t
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Char_t_IControlConvertible_ToString_returns_char_string()
    {
        var p = new Parameter_t<Char_t>("X") { WireValue = "A" };
        var cc = p.Value;
        cc.ToString(null).Should().Be("A");
    }

    [Fact]
    public void Char_t_IControlConvertible_ToBoolean_throws()
    {
        var p = new Parameter_t<Char_t>("X") { WireValue = "A" };
        var cc = p.Value;
        var act = cc.ToBoolean;
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void Char_t_IControlConvertible_ToDecimal_throws()
    {
        var p = new Parameter_t<Char_t>("X") { WireValue = "A" };
        var cc = p.Value;
        var act = cc.ToDecimal;
        act.Should().Throw<InvalidCastException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IControlConvertible — String_t
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void String_t_IControlConvertible_ToString_returns_string()
    {
        var p = new Parameter_t<String_t>("X") { WireValue = "hello" };
        var cc = p.Value;
        cc.ToString(null).Should().Be("hello");
    }

    [Fact]
    public void String_t_IControlConvertible_ToBoolean_throws()
    {
        var p = new Parameter_t<String_t>("X") { WireValue = "hello" };
        var cc = p.Value;
        var act = cc.ToBoolean;
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void String_t_IControlConvertible_ToDecimal_throws()
    {
        var p = new Parameter_t<String_t>("X") { WireValue = "hello" };
        var cc = p.Value;
        var act = cc.ToDecimal;
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void String_t_IControlConvertible_ToDateTime_throws()
    {
        var p = new Parameter_t<String_t>("X") { WireValue = "hello" };
        var cc = p.Value;
        var act = cc.ToDateTime;
        act.Should().Throw<InvalidCastException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IControlConvertible — NonNegativeIntegerTypeBase (via SeqNum_t)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SeqNum_t_IControlConvertible_ToDecimal_returns_uint_as_decimal()
    {
        var p = new Parameter_t<SeqNum_t>("X") { WireValue = "100" };
        var cc = p.Value;
        cc.ToDecimal().Should().Be(100m);
    }

    [Fact]
    public void SeqNum_t_IControlConvertible_ToString_returns_formatted_uint()
    {
        var p = new Parameter_t<SeqNum_t>("X") { WireValue = "100" };
        var cc = p.Value;
        cc.ToString(CultureInfo.InvariantCulture).Should().Be("100");
    }

    [Fact]
    public void SeqNum_t_IControlConvertible_ToBoolean_throws()
    {
        var p = new Parameter_t<SeqNum_t>("X") { WireValue = "1" };
        var cc = p.Value;
        var act = cc.ToBoolean;
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void SeqNum_t_IControlConvertible_ToDateTime_throws()
    {
        var p = new Parameter_t<SeqNum_t>("X") { WireValue = "1" };
        var cc = p.Value;
        var act = cc.ToDateTime;
        act.Should().Throw<InvalidCastException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IControlConvertible — EnumTypeBase<T> (via Country_t)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Country_t_IControlConvertible_ToString_returns_code_string()
    {
        var p = new Parameter_t<Country_t>("X") { WireValue = "US" };
        var cc = p.Value;
        cc.ToString(null).Should().Be("US");
    }

    [Fact]
    public void Country_t_IControlConvertible_ToBoolean_throws()
    {
        var p = new Parameter_t<Country_t>("X") { WireValue = "US" };
        var cc = p.Value;
        var act = cc.ToBoolean;
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void Country_t_IControlConvertible_ToDecimal_throws()
    {
        var p = new Parameter_t<Country_t>("X") { WireValue = "US" };
        var cc = p.Value;
        var act = cc.ToDecimal;
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void Country_t_IControlConvertible_ToDateTime_throws()
    {
        var p = new Parameter_t<Country_t>("X") { WireValue = "US" };
        var cc = p.Value;
        var act = cc.ToDateTime;
        act.Should().Throw<InvalidCastException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IControlConvertible — DateTimeTypeBase (via UTCTimestamp_t)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void UTCTimestamp_t_IControlConvertible_ToDateTime_returns_DateTime()
    {
        var p = new Parameter_t<UTCTimestamp_t>("X") { WireValue = "20260101-09:30:00" };
        var cc = p.Value;
        cc.ToDateTime().Should().NotBeNull();
    }

    [Fact]
    public void UTCTimestamp_t_IControlConvertible_ToBoolean_throws()
    {
        var p = new Parameter_t<UTCTimestamp_t>("X") { WireValue = "20260101-09:30:00" };
        var cc = p.Value;
        var act = cc.ToBoolean;
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void UTCTimestamp_t_IControlConvertible_ToDecimal_throws()
    {
        var p = new Parameter_t<UTCTimestamp_t>("X") { WireValue = "20260101-09:30:00" };
        var cc = p.Value;
        var act = cc.ToDecimal;
        act.Should().Throw<InvalidCastException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IControlConvertible — MonthYear_t
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void MonthYear_t_IControlConvertible_ToString_returns_wire_form()
    {
        var p = new Parameter_t<MonthYear_t>("X") { WireValue = "202601" };
        var cc = p.Value;
        cc.ToString(null).Should().Be("202601");
    }

    [Fact]
    public void MonthYear_t_IControlConvertible_ToBoolean_throws()
    {
        var p = new Parameter_t<MonthYear_t>("X") { WireValue = "202601" };
        var cc = p.Value;
        var act = cc.ToBoolean;
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void MonthYear_t_IControlConvertible_ToDecimal_throws()
    {
        var p = new Parameter_t<MonthYear_t>("X") { WireValue = "202601" };
        var cc = p.Value;
        var act = cc.ToDecimal;
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void MonthYear_t_IControlConvertible_ToDateTime_throws()
    {
        var p = new Parameter_t<MonthYear_t>("X") { WireValue = "202601" };
        var cc = p.Value;
        var act = cc.ToDateTime;
        act.Should().Throw<InvalidCastException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IControlConvertible — Tenor_t
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Tenor_t_IControlConvertible_ToString_returns_wire_form()
    {
        var p = new Parameter_t<Tenor_t>("X") { WireValue = "M3" };
        var cc = p.Value;
        cc.ToString(null).Should().Be("M3");
    }

    [Fact]
    public void Tenor_t_IControlConvertible_ToBoolean_throws()
    {
        var p = new Parameter_t<Tenor_t>("X") { WireValue = "M3" };
        var cc = p.Value;
        var act = cc.ToBoolean;
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void Tenor_t_IControlConvertible_ToDecimal_throws()
    {
        var p = new Parameter_t<Tenor_t>("X") { WireValue = "M3" };
        var cc = p.Value;
        var act = cc.ToDecimal;
        act.Should().Throw<InvalidCastException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IControlConvertible — Data_t
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Data_t_IControlConvertible_ToString_returns_data_string()
    {
        var p = new Parameter_t<Data_t>("X") { WireValue = "abc" };
        var cc = p.Value;
        cc.ToString(null).Should().Be("abc");
    }

    [Fact]
    public void Data_t_IControlConvertible_ToBoolean_throws()
    {
        var p = new Parameter_t<Data_t>("X") { WireValue = "abc" };
        var cc = p.Value;
        var act = cc.ToBoolean;
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void Data_t_IControlConvertible_ToDecimal_throws()
    {
        var p = new Parameter_t<Data_t>("X") { WireValue = "abc" };
        var cc = p.Value;
        var act = cc.ToDecimal;
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void Data_t_IControlConvertible_ToDateTime_throws()
    {
        var p = new Parameter_t<Data_t>("X") { WireValue = "abc" };
        var cc = p.Value;
        var act = cc.ToDateTime;
        act.Should().Throw<InvalidCastException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // String_t MinLength / MaxLength validation
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void String_t_rejects_value_exceeding_MaxLength()
    {
        var p = new Parameter_t<String_t>("X");
        p.Value.MaxLength = 3;
        var act = () => p.WireValue = "ABCD";
        act.Should().Throw<InvalidFieldValueException>();
    }

    [Fact]
    public void String_t_rejects_value_below_MinLength()
    {
        var p = new Parameter_t<String_t>("X");
        p.Value.MinLength = 5;
        var act = () => p.WireValue = "ABC";
        act.Should().Throw<InvalidFieldValueException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Percentage_t IControlConvertible.ToDecimal returns scaled whole-percent value
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Percentage_t_IControlConvertible_ToDecimal_scales_by_100()
    {
        // NOTE: Percentage_t stores fractional internally (0.75) but ToDecimal
        // scales to whole-percent units (75) for display.
        var p = new Parameter_t<Percentage_t>("X") { WireValue = "0.75" };
        var cc = p.Value;
        cc.ToDecimal().Should().Be(75m);
    }

    [Fact]
    public void Percentage_t_IControlConvertible_ToString_returns_whole_percent()
    {
        var p = new Parameter_t<Percentage_t>("X") { WireValue = "0.75" };
        var cc = p.Value;
        cc.ToString(CultureInfo.InvariantCulture).Should().Be("75");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // MonthYear_t MinValue / MaxValue validation
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void MonthYear_t_rejects_value_exceeding_MaxValue()
    {
        var p = new Parameter_t<MonthYear_t>("X");
        p.Value.MaxValue = MonthYear.Parse("202601");
        var act = () => p.WireValue = "202602";
        act.Should().Throw<InvalidFieldValueException>();
    }

    [Fact]
    public void MonthYear_t_rejects_value_below_MinValue()
    {
        var p = new Parameter_t<MonthYear_t>("X");
        p.Value.MinValue = MonthYear.Parse("202601");
        var act = () => p.WireValue = "202512";
        act.Should().Throw<InvalidFieldValueException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Tenor_t MinValue / MaxValue validation
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Tenor_t_rejects_value_exceeding_MaxValue()
    {
        var p = new Parameter_t<Tenor_t>("X");
        p.Value.MaxValue = Tenor.Parse("M3");
        var act = () => p.WireValue = "M6";
        act.Should().Throw<InvalidFieldValueException>();
    }

    [Fact]
    public void Tenor_t_rejects_value_below_MinValue()
    {
        var p = new Parameter_t<Tenor_t>("X");
        p.Value.MinValue = Tenor.Parse("M3");
        var act = () => p.WireValue = "M1";
        act.Should().Throw<InvalidFieldValueException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UTCTimestamp_t MaxValue / MinValue validation
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void UTCTimestamp_t_rejects_value_exceeding_MaxValue()
    {
        var p = new Parameter_t<UTCTimestamp_t>("X");
        p.Value.MaxValue = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var act = () => p.WireValue = "20260102-00:00:00";
        act.Should().Throw<InvalidFieldValueException>();
    }

    [Fact]
    public void Percentage_t_ConstValue_returns_correct_native_value_without_extra_division()
    {
        var p = new Parameter_t<Percentage_t>("X");
        p.Value.ConstValue = 0.75m; // 75% in native units (fraction)
        p.Value.MultiplyBy100 = true;

        // GetNativeValue(applyWireValueFormat: false) should return the native fraction as-is (0.75)
        p.Value.GetNativeValue(applyWireValueFormat: false).Should().Be(0.75m);

        // GetNativeValue(applyWireValueFormat: true) should apply the wire format multiplier and return 75
        p.Value.GetNativeValue(applyWireValueFormat: true).Should().Be(75m);

        // And WireValue should be correct (75)
        p.WireValue.Should().Be("75");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(29)]
    public void Float_t_Precision_out_of_range_throws_InvalidFieldValueException(int badPrecision)
    {
        var p = new Parameter_t<Float_t>("X");
        var act = () => p.Value.Precision = badPrecision;
        act.Should().Throw<InvalidFieldValueException>();
    }

    [Fact]
    public void Char_t_invalid_wire_value_throws_InvalidFieldValueException()
    {
        var p = new Parameter_t<Char_t>("C");
        var act1 = () => p.WireValue = "";
        act1.Should().Throw<InvalidFieldValueException>();

        var act2 = () => p.WireValue = "AB";
        act2.Should().Throw<InvalidFieldValueException>();
    }

    [Fact]
    public void Boolean_t_cannot_have_equal_wire_values()
    {
        var strategies = new Strategies_t();
        var strategy = new Strategy_t { Name = "Test" };
        var p = new Parameter_t<Boolean_t>("X") { Type = "Boolean_t" };
        strategy.Parameters.Add(p);
        strategies.Strategies.Add(strategy);

        // Setting them to different values is fine
        p.Value.TrueWireValue = "Y";
        p.Value.FalseWireValue = "N";
        strategies.ResolveAll();

        // Setting them to the same value throws ArgumentException during ResolveAll
        p.Value.TrueWireValue = "N";
        var act1 = () => strategies.ResolveAll();
        act1.Should().Throw<ArgumentException>();

        p.Value.TrueWireValue = "Y";
        p.Value.FalseWireValue = "Y";
        var act2 = () => strategies.ResolveAll();
        act2.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Boolean_t_inbound_null_wire_value_returns_null()
    {
        var p = new Parameter_t<Boolean_t>("X") { WireValue = "{NULL}" };
        p.Value.ToBoolean().Should().BeNull();
    }

    [Fact]
    public void ValidationResult_formatting_safety_on_zero_args_with_literal_braces()
    {
        // Zero args, string contains literal braces (e.g. from XML)
        var result = new FixPortal.FixAtdl.Validation.ValidationResult(FixPortal.FixAtdl.Validation.ValidationResult.ResultType.Invalid, "This has literal braces {NULL} and Nullable{Int32}");
        result.ErrorText.Should().Be("This has literal braces {NULL} and Nullable{Int32}");
    }

    [Fact]
    public void RegionCountries_are_read_only()
    {
        FixPortal.FixAtdl.Model.Reference.Regions.TheAmericasCountries.Should().BeAssignableTo<System.Collections.Frozen.FrozenSet<FixPortal.FixAtdl.Model.Reference.IsoCountryCode>>();
        FixPortal.FixAtdl.Model.Reference.Regions.EuropeMiddleEastAfricaCountries.Should().BeAssignableTo<System.Collections.Frozen.FrozenSet<FixPortal.FixAtdl.Model.Reference.IsoCountryCode>>();
        FixPortal.FixAtdl.Model.Reference.Regions.AsiaPacificJapanCountries.Should().BeAssignableTo<System.Collections.Frozen.FrozenSet<FixPortal.FixAtdl.Model.Reference.IsoCountryCode>>();

        var americasColl = (System.Collections.Generic.ICollection<FixPortal.FixAtdl.Model.Reference.IsoCountryCode>)FixPortal.FixAtdl.Model.Reference.Regions.TheAmericasCountries;
        Action addAct = () => americasColl.Add(FixPortal.FixAtdl.Model.Reference.IsoCountryCode.US);
        addAct.Should().Throw<NotSupportedException>();
    }
}
