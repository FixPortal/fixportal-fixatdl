using System.Globalization;
using FixPortal.FixAtdl.Fix;

namespace FixPortal.FixAtdl.Tests.Fix;

public class FixPrimitivesTests
{
    // FixTag -------------------------------------------------------------------

    [Fact]
    public void FixTag_rejects_zero()
    {
        var act = () => new FixTag(0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FixTag_rejects_negative()
    {
        var act = () => new FixTag(-1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FixTag_accepts_positive_value()
    {
        var tag = new FixTag(1);
        ((int)tag).Should().Be(1);
    }

    [Fact]
    public void FixTag_implicitly_converts_from_int()
    {
        FixTag tag = 168;
        ((int)tag).Should().Be(168);
    }

    [Fact]
    public void FixTag_implicitly_converts_to_int()
    {
        FixTag tag = 49;
        int value = tag;
        value.Should().Be(49);
    }

    [Fact]
    public void FixTag_implicitly_converts_from_FixField()
    {
        FixField field = (FixField)168;
        FixTag tag = field;
        ((int)tag).Should().Be(168);
    }

    [Fact]
    public void FixTag_implicitly_converts_to_FixField()
    {
        FixTag tag = 168;
        FixField field = tag;
        ((int)field).Should().Be(168);
    }

    [Fact]
    public void FixTag_ToString_returns_invariant_integer_string()
    {
        FixTag tag = 168;
        tag.ToString().Should().Be("168");
    }

    // NumInGroup --------------------------------------------------------------

    [Fact]
    public void NumInGroup_rejects_negative()
    {
        var act = () => new NumInGroup(-1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void NumInGroup_accepts_zero()
    {
        // NOTE: zero is a valid NumInGroup (empty repeating block)
        var n = new NumInGroup(0);
        ((int)n).Should().Be(0);
    }

    [Fact]
    public void NumInGroup_accepts_positive_value()
    {
        var n = new NumInGroup(5);
        ((int)n).Should().Be(5);
    }

    [Fact]
    public void NumInGroup_implicitly_converts_from_int()
    {
        NumInGroup n = 3;
        ((int)n).Should().Be(3);
    }

    [Fact]
    public void NumInGroup_implicitly_converts_to_int()
    {
        NumInGroup n = 7;
        int value = n;
        value.Should().Be(7);
    }

    [Fact]
    public void NumInGroup_ToString_returns_invariant_integer_string()
    {
        NumInGroup n = 42;
        n.ToString().Should().Be("42");
    }

    [Fact]
    public void NumInGroup_zero_ToString_returns_zero()
    {
        NumInGroup n = 0;
        n.ToString().Should().Be("0");
    }

    // FixDateTime -------------------------------------------------------------

    [Theory]
    [InlineData("20260101-09:30:00", 2026, 1, 1, 9, 30, 0, 0)]
    [InlineData("20260101-09:30:00.123", 2026, 1, 1, 9, 30, 0, 123)]
    [InlineData("09:30:00", 0, 0, 0, 9, 30, 0, 0)]
    [InlineData("09:30:00.456", 0, 0, 0, 9, 30, 0, 456)]
    [InlineData("20260101", 2026, 1, 1, 0, 0, 0, 0)]
    public void FixDateTime_TryParse_returns_expected_value_for_valid_FIX_formats(
        string input,
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second,
        int millisecond
    )
    {
        var result = FixDateTime.TryParse(input, CultureInfo.InvariantCulture, out var dt);

        result.Should().BeTrue();
        dt.Kind.Should().Be(DateTimeKind.Utc);
        if (year != 0)
        {
            dt.Year.Should().Be(year);
            dt.Month.Should().Be(month);
            dt.Day.Should().Be(day);
        }
        dt.TimeOfDay.Should().Be(new TimeSpan(0, hour, minute, second, millisecond));
    }

    [Fact]
    public void FixDateTime_TryParse_full_datetime_produces_correct_year_and_month()
    {
        FixDateTime.TryParse("20260530-14:45:00", CultureInfo.InvariantCulture, out var dt).Should().BeTrue();
        dt.Year.Should().Be(2026);
        dt.Month.Should().Be(5);
        dt.Day.Should().Be(30);
        dt.Hour.Should().Be(14);
        dt.Minute.Should().Be(45);
    }

    [Fact]
    public void FixDateTime_TryParse_returns_false_for_completely_invalid_input()
    {
        var result = FixDateTime.TryParse("not-a-date", CultureInfo.InvariantCulture, out var dt);
        result.Should().BeFalse();
        dt.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public void FixDateTime_Parse_returns_value_for_valid_input()
    {
        var dt = FixDateTime.Parse("20260101-09:30:00", CultureInfo.InvariantCulture);
        dt.Year.Should().Be(2026);
    }

    [Fact]
    public void FixDateTime_Parse_throws_InvalidCastException_for_invalid_input()
    {
        var act = () => FixDateTime.Parse("not-a-date", CultureInfo.InvariantCulture);
        act.Should().Throw<InvalidCastException>();
    }
}
