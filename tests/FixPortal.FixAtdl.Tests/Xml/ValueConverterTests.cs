using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Types.Support;
using FixPortal.FixAtdl.Xml.Serialization;

namespace FixPortal.FixAtdl.Tests.Xml;

public class ValueConverterTests
{
    [Fact]
    public void ConvertTo_supports_char_array_attributes()
    {
        var result = ValueConverter.ConvertTo<char[]>("ABC");

        result.Should().Equal('A', 'B', 'C');
    }

    [Fact]
    public void ConvertTo_supports_MonthYear_attributes()
    {
        var result = ValueConverter.ConvertTo<MonthYear>("202401");

        result.Should().Be(MonthYear.Parse("202401"));
    }

    [Fact]
    public void ConvertTo_supports_Tenor_attributes()
    {
        var result = ValueConverter.ConvertTo<Tenor>("M3");

        result.Should().Be(Tenor.Parse("M3"));
    }

    [Fact]
    public void ConvertTo_supports_NumInGroup_attributes()
    {
        var result = ValueConverter.ConvertTo<NumInGroup>("2");

        ((int)result).Should().Be(2);
    }

    [Fact]
    public void ConvertTo_wraps_invalid_MonthYear_values_in_InvalidFieldValueException()
    {
        var act = () => ValueConverter.ConvertTo<MonthYear>("not-a-month-year");

        act.Should().Throw<InvalidFieldValueException>();
    }

    [Fact]
    public void ConvertTo_wraps_invalid_Tenor_values_in_InvalidFieldValueException()
    {
        var act = () => ValueConverter.ConvertTo<Tenor>("not-a-tenor");

        act.Should().Throw<InvalidFieldValueException>();
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("0", false)]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("True", true)]
    [InlineData("False", false)]
    public void ConvertTo_supports_boolean_values_including_xs_boolean_literals(string value, bool expected)
    {
        var result = ValueConverter.ConvertTo<bool>(value);

        result.Should().Be(expected);
    }
}
