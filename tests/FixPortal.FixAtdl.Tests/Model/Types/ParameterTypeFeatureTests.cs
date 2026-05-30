using System.Globalization;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Types;
using FixPortal.FixAtdl.Model.Types.Support;

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

    // ──────────────────────────────────────────────────────────────────────────
    // IControlConvertible — Int_t
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Int_t_IControlConvertible_ToDecimal_returns_int_as_decimal()
    {
        var p = new Parameter_t<Int_t>("X") { WireValue = "42" };
        var cc = (IControlConvertible)p.Value;
        cc.ToDecimal().Should().Be(42m);
    }

    [Fact]
    public void Int_t_IControlConvertible_ToString_returns_formatted_int()
    {
        var p = new Parameter_t<Int_t>("X") { WireValue = "42" };
        var cc = (IControlConvertible)p.Value;
        cc.ToString(CultureInfo.InvariantCulture).Should().Be("42");
    }

    [Fact]
    public void Int_t_IControlConvertible_ToBoolean_throws()
    {
        var p = new Parameter_t<Int_t>("X") { WireValue = "1" };
        var cc = (IControlConvertible)p.Value;
        var act = () => cc.ToBoolean();
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void Int_t_IControlConvertible_ToDateTime_throws()
    {
        var p = new Parameter_t<Int_t>("X") { WireValue = "1" };
        var cc = (IControlConvertible)p.Value;
        var act = () => cc.ToDateTime();
        act.Should().Throw<InvalidCastException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IControlConvertible — Float_t
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Float_t_IControlConvertible_ToDecimal_returns_decimal()
    {
        var p = new Parameter_t<Float_t>("X") { WireValue = "3.14" };
        var cc = (IControlConvertible)p.Value;
        cc.ToDecimal().Should().Be(3.14m);
    }

    [Fact]
    public void Float_t_IControlConvertible_ToBoolean_throws()
    {
        var p = new Parameter_t<Float_t>("X") { WireValue = "1.0" };
        var cc = (IControlConvertible)p.Value;
        var act = () => cc.ToBoolean();
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void Float_t_IControlConvertible_ToDateTime_throws()
    {
        var p = new Parameter_t<Float_t>("X") { WireValue = "1.0" };
        var cc = (IControlConvertible)p.Value;
        var act = () => cc.ToDateTime();
        act.Should().Throw<InvalidCastException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IControlConvertible — Boolean_t
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Boolean_t_IControlConvertible_ToBoolean_returns_bool()
    {
        var p = new Parameter_t<Boolean_t>("X") { WireValue = "Y" };
        var cc = (IControlConvertible)p.Value;
        cc.ToBoolean().Should().Be(true);
    }

    [Fact]
    public void Boolean_t_IControlConvertible_ToDecimal_throws()
    {
        var p = new Parameter_t<Boolean_t>("X") { WireValue = "Y" };
        var cc = (IControlConvertible)p.Value;
        var act = () => cc.ToDecimal();
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void Boolean_t_IControlConvertible_ToDateTime_throws()
    {
        var p = new Parameter_t<Boolean_t>("X") { WireValue = "Y" };
        var cc = (IControlConvertible)p.Value;
        var act = () => cc.ToDateTime();
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void Boolean_t_IControlConvertible_ToString_returns_lower_bool()
    {
        var p = new Parameter_t<Boolean_t>("X") { WireValue = "Y" };
        var cc = (IControlConvertible)p.Value;
        // NOTE: Boolean_t.ToString(provider) returns "true"/"false" (lowercase),
        // not the wire value "Y"/"N".
        cc.ToString(null).Should().Be("true");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IControlConvertible — Char_t
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Char_t_IControlConvertible_ToString_returns_char_string()
    {
        var p = new Parameter_t<Char_t>("X") { WireValue = "A" };
        var cc = (IControlConvertible)p.Value;
        cc.ToString(null).Should().Be("A");
    }

    [Fact]
    public void Char_t_IControlConvertible_ToBoolean_throws()
    {
        var p = new Parameter_t<Char_t>("X") { WireValue = "A" };
        var cc = (IControlConvertible)p.Value;
        var act = () => cc.ToBoolean();
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void Char_t_IControlConvertible_ToDecimal_throws()
    {
        var p = new Parameter_t<Char_t>("X") { WireValue = "A" };
        var cc = (IControlConvertible)p.Value;
        var act = () => cc.ToDecimal();
        act.Should().Throw<InvalidCastException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IControlConvertible — String_t
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void String_t_IControlConvertible_ToString_returns_string()
    {
        var p = new Parameter_t<String_t>("X") { WireValue = "hello" };
        var cc = (IControlConvertible)p.Value;
        cc.ToString(null).Should().Be("hello");
    }

    [Fact]
    public void String_t_IControlConvertible_ToBoolean_throws()
    {
        var p = new Parameter_t<String_t>("X") { WireValue = "hello" };
        var cc = (IControlConvertible)p.Value;
        var act = () => cc.ToBoolean();
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void String_t_IControlConvertible_ToDecimal_throws()
    {
        var p = new Parameter_t<String_t>("X") { WireValue = "hello" };
        var cc = (IControlConvertible)p.Value;
        var act = () => cc.ToDecimal();
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void String_t_IControlConvertible_ToDateTime_throws()
    {
        var p = new Parameter_t<String_t>("X") { WireValue = "hello" };
        var cc = (IControlConvertible)p.Value;
        var act = () => cc.ToDateTime();
        act.Should().Throw<InvalidCastException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IControlConvertible — NonNegativeIntegerTypeBase (via SeqNum_t)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SeqNum_t_IControlConvertible_ToDecimal_returns_uint_as_decimal()
    {
        var p = new Parameter_t<SeqNum_t>("X") { WireValue = "100" };
        var cc = (IControlConvertible)p.Value;
        cc.ToDecimal().Should().Be(100m);
    }

    [Fact]
    public void SeqNum_t_IControlConvertible_ToString_returns_formatted_uint()
    {
        var p = new Parameter_t<SeqNum_t>("X") { WireValue = "100" };
        var cc = (IControlConvertible)p.Value;
        cc.ToString(CultureInfo.InvariantCulture).Should().Be("100");
    }

    [Fact]
    public void SeqNum_t_IControlConvertible_ToBoolean_throws()
    {
        var p = new Parameter_t<SeqNum_t>("X") { WireValue = "1" };
        var cc = (IControlConvertible)p.Value;
        var act = () => cc.ToBoolean();
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void SeqNum_t_IControlConvertible_ToDateTime_throws()
    {
        var p = new Parameter_t<SeqNum_t>("X") { WireValue = "1" };
        var cc = (IControlConvertible)p.Value;
        var act = () => cc.ToDateTime();
        act.Should().Throw<InvalidCastException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IControlConvertible — EnumTypeBase<T> (via Country_t)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Country_t_IControlConvertible_ToString_returns_code_string()
    {
        var p = new Parameter_t<FixPortal.FixAtdl.Model.Types.Country_t>("X") { WireValue = "US" };
        var cc = (IControlConvertible)p.Value;
        cc.ToString(null).Should().Be("US");
    }

    [Fact]
    public void Country_t_IControlConvertible_ToBoolean_throws()
    {
        var p = new Parameter_t<FixPortal.FixAtdl.Model.Types.Country_t>("X") { WireValue = "US" };
        var cc = (IControlConvertible)p.Value;
        var act = () => cc.ToBoolean();
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void Country_t_IControlConvertible_ToDecimal_throws()
    {
        var p = new Parameter_t<FixPortal.FixAtdl.Model.Types.Country_t>("X") { WireValue = "US" };
        var cc = (IControlConvertible)p.Value;
        var act = () => cc.ToDecimal();
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void Country_t_IControlConvertible_ToDateTime_throws()
    {
        var p = new Parameter_t<FixPortal.FixAtdl.Model.Types.Country_t>("X") { WireValue = "US" };
        var cc = (IControlConvertible)p.Value;
        var act = () => cc.ToDateTime();
        act.Should().Throw<InvalidCastException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IControlConvertible — DateTimeTypeBase (via UTCTimestamp_t)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void UTCTimestamp_t_IControlConvertible_ToDateTime_returns_DateTime()
    {
        var p = new Parameter_t<UTCTimestamp_t>("X") { WireValue = "20260101-09:30:00" };
        var cc = (IControlConvertible)p.Value;
        cc.ToDateTime().Should().NotBeNull();
    }

    [Fact]
    public void UTCTimestamp_t_IControlConvertible_ToBoolean_throws()
    {
        var p = new Parameter_t<UTCTimestamp_t>("X") { WireValue = "20260101-09:30:00" };
        var cc = (IControlConvertible)p.Value;
        var act = () => cc.ToBoolean();
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void UTCTimestamp_t_IControlConvertible_ToDecimal_throws()
    {
        var p = new Parameter_t<UTCTimestamp_t>("X") { WireValue = "20260101-09:30:00" };
        var cc = (IControlConvertible)p.Value;
        var act = () => cc.ToDecimal();
        act.Should().Throw<InvalidCastException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IControlConvertible — MonthYear_t
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void MonthYear_t_IControlConvertible_ToString_returns_wire_form()
    {
        var p = new Parameter_t<MonthYear_t>("X") { WireValue = "202601" };
        var cc = (IControlConvertible)p.Value;
        cc.ToString(null).Should().Be("202601");
    }

    [Fact]
    public void MonthYear_t_IControlConvertible_ToBoolean_throws()
    {
        var p = new Parameter_t<MonthYear_t>("X") { WireValue = "202601" };
        var cc = (IControlConvertible)p.Value;
        var act = () => cc.ToBoolean();
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void MonthYear_t_IControlConvertible_ToDecimal_throws()
    {
        var p = new Parameter_t<MonthYear_t>("X") { WireValue = "202601" };
        var cc = (IControlConvertible)p.Value;
        var act = () => cc.ToDecimal();
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void MonthYear_t_IControlConvertible_ToDateTime_throws()
    {
        var p = new Parameter_t<MonthYear_t>("X") { WireValue = "202601" };
        var cc = (IControlConvertible)p.Value;
        var act = () => cc.ToDateTime();
        act.Should().Throw<InvalidCastException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IControlConvertible — Tenor_t
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Tenor_t_IControlConvertible_ToString_returns_wire_form()
    {
        var p = new Parameter_t<Tenor_t>("X") { WireValue = "M3" };
        var cc = (IControlConvertible)p.Value;
        cc.ToString(null).Should().Be("M3");
    }

    [Fact]
    public void Tenor_t_IControlConvertible_ToBoolean_throws()
    {
        var p = new Parameter_t<Tenor_t>("X") { WireValue = "M3" };
        var cc = (IControlConvertible)p.Value;
        var act = () => cc.ToBoolean();
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void Tenor_t_IControlConvertible_ToDecimal_throws()
    {
        var p = new Parameter_t<Tenor_t>("X") { WireValue = "M3" };
        var cc = (IControlConvertible)p.Value;
        var act = () => cc.ToDecimal();
        act.Should().Throw<InvalidCastException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IControlConvertible — Data_t
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Data_t_IControlConvertible_ToString_returns_data_string()
    {
        var p = new Parameter_t<Data_t>("X") { WireValue = "abc" };
        var cc = (IControlConvertible)p.Value;
        cc.ToString(null).Should().Be("abc");
    }

    [Fact]
    public void Data_t_IControlConvertible_ToBoolean_throws()
    {
        var p = new Parameter_t<Data_t>("X") { WireValue = "abc" };
        var cc = (IControlConvertible)p.Value;
        var act = () => cc.ToBoolean();
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void Data_t_IControlConvertible_ToDecimal_throws()
    {
        var p = new Parameter_t<Data_t>("X") { WireValue = "abc" };
        var cc = (IControlConvertible)p.Value;
        var act = () => cc.ToDecimal();
        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void Data_t_IControlConvertible_ToDateTime_throws()
    {
        var p = new Parameter_t<Data_t>("X") { WireValue = "abc" };
        var cc = (IControlConvertible)p.Value;
        var act = () => cc.ToDateTime();
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
        var cc = (IControlConvertible)p.Value;
        cc.ToDecimal().Should().Be(75m);
    }

    [Fact]
    public void Percentage_t_IControlConvertible_ToString_returns_whole_percent()
    {
        var p = new Parameter_t<Percentage_t>("X") { WireValue = "0.75" };
        var cc = (IControlConvertible)p.Value;
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
}
