using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Model.Reference;
using FixPortal.FixAtdl.Model.Types.Support;
using FixPortal.FixAtdl.Validation;

namespace FixPortal.FixAtdl.Tests.Validation;

/// <summary>
/// Tests for <see cref="EditValueConverter.ConvertToComparableType"/> covering each type branch.
/// </summary>
public class EditValueConverterTests
{
    // ── Null prototype ───────────────────────────────────────────────────────

    [Fact]
    public void Null_prototype_returns_value_unchanged()
    {
        IComparable result = EditValueConverter.ConvertToComparableType(null!, "hello");
        result.Should().Be("hello");
    }

    // ── Decimal ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("1.5", 1.5)]
    [InlineData("0", 0.0)]
    [InlineData("-99.99", -99.99)]
    public void Converts_decimal_string(string input, double expected)
    {
        IComparable result = EditValueConverter.ConvertToComparableType(0m, input);
        result.Should().Be((decimal)expected);
    }

    [Fact]
    public void Throws_InvalidFieldValueException_for_bad_decimal()
    {
        var act = () => EditValueConverter.ConvertToComparableType(0m, "not-a-decimal");
        act.Should().Throw<InvalidFieldValueException>();
    }

    // ── Int32 ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("42", 42)]
    [InlineData("-1", -1)]
    [InlineData("0", 0)]
    public void Converts_int32_string(string input, int expected)
    {
        IComparable result = EditValueConverter.ConvertToComparableType(0, input);
        result.Should().Be(expected);
    }

    [Fact]
    public void Throws_InvalidFieldValueException_for_bad_int32()
    {
        var act = () => EditValueConverter.ConvertToComparableType(0, "not-a-number");
        act.Should().Throw<InvalidFieldValueException>();
    }

    [Fact]
    public void Throws_InvalidFieldValueException_for_int32_overflow()
    {
        var act = () => EditValueConverter.ConvertToComparableType(0, "99999999999999999");
        act.Should().Throw<InvalidFieldValueException>();
    }

    // ── UInt32 ───────────────────────────────────────────────────────────────

    [Fact]
    public void Converts_uint32_string()
    {
        IComparable result = EditValueConverter.ConvertToComparableType(0u, "100");
        result.Should().Be(100u);
    }

    [Fact]
    public void Throws_InvalidFieldValueException_for_bad_uint32()
    {
        var act = () => EditValueConverter.ConvertToComparableType(0u, "xyz");
        act.Should().Throw<InvalidFieldValueException>();
    }

    // ── Char ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Converts_char_string()
    {
        IComparable result = EditValueConverter.ConvertToComparableType('A', "B");
        result.Should().Be('B');
    }

    // ── Boolean ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Y", true)]
    [InlineData("N", false)]
    [InlineData("True", true)]
    [InlineData("False", false)]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public void Converts_bool_string(string input, bool expected)
    {
        IComparable result = EditValueConverter.ConvertToComparableType(false, input);
        result.Should().Be(expected);
    }

    [Fact]
    public void Throws_InvalidFieldValueException_for_bad_bool()
    {
        var act = () => EditValueConverter.ConvertToComparableType(false, "maybe");
        act.Should().Throw<InvalidFieldValueException>();
    }

    [Fact]
    public void Throws_InvalidFieldValueException_for_null_bool()
    {
        var act = () => EditValueConverter.ConvertToComparableType(false, null!);
        act.Should().Throw<InvalidFieldValueException>();
    }

    // ── String ───────────────────────────────────────────────────────────────

    [Fact]
    public void Converts_string_returns_same_value()
    {
        IComparable result = EditValueConverter.ConvertToComparableType("prototype", "actual");
        result.Should().Be("actual");
    }

    // ── DateTime ─────────────────────────────────────────────────────────────

    [Fact]
    public void Converts_datetime_string()
    {
        // FIX UTCTimestamp format: YYYYMMDD-HH:MM:SS
        IComparable result = EditValueConverter.ConvertToComparableType(DateTime.MinValue, "20240101-09:30:00");
        result.Should().BeAssignableTo<DateTime>();
    }

    // ── Enum codes ───────────────────────────────────────────────────────────

    [Fact]
    public void Converts_iso_country_code()
    {
        IComparable result = EditValueConverter.ConvertToComparableType(default(IsoCountryCode), "US");
        result.Should().BeAssignableTo<IsoCountryCode>();
    }

    [Fact]
    public void Converts_iso_currency_code()
    {
        IComparable result = EditValueConverter.ConvertToComparableType(default(IsoCurrencyCode), "USD");
        result.Should().BeAssignableTo<IsoCurrencyCode>();
    }

    [Fact]
    public void Converts_iso_language_code()
    {
        IComparable result = EditValueConverter.ConvertToComparableType(default(IsoLanguageCode), "en");
        result.Should().BeAssignableTo<IsoLanguageCode>();
    }

    // ── MonthYear / Tenor ────────────────────────────────────────────────────

    [Fact]
    public void Converts_month_year()
    {
        IComparable result = EditValueConverter.ConvertToComparableType(default(MonthYear), "202401");
        result.Should().BeAssignableTo<MonthYear>();
    }

    // ── Unknown type → InvalidCastException ──────────────────────────────────

    [Fact]
    public void Throws_InvalidCastException_for_unknown_type()
    {
        // Pass an instance of an unsupported type as the prototype.
        var act = () => EditValueConverter.ConvertToComparableType(new List<int>(), "value");
        act.Should().Throw<InvalidCastException>();
    }
}
