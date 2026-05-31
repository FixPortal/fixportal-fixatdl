using AwesomeAssertions;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Model.Controls;
using NodaTime;

namespace FixPortal.FixAtdl.Tests.Controls;

public class InitValueClockTests
{
    [Theory]
    [InlineData("08:00:00")]
    [InlineData("23:59:59")]
    [InlineData("08:00:00.250")]
    public void Time_only_value_is_parsed_as_time_of_day(string raw)
    {
        var iv = new InitValueClock(raw);

        iv.IsTimeOnly.Should().BeTrue();
        iv.TimeOfDay.Should().NotBeNull();
        iv.DateTime.Should().BeNull();
    }

    [Fact]
    public void Time_only_value_keeps_the_exact_time_of_day()
    {
        var iv = new InitValueClock("08:00:00");

        iv.TimeOfDay.Should().Be(new LocalTime(8, 0, 0));
    }

    [Theory]
    [InlineData("20260601-09:30:00")]
    [InlineData("20260601-09:30:00.500")]
    public void Full_datetime_value_is_parsed_as_local_datetime(string raw)
    {
        var iv = new InitValueClock(raw);

        iv.IsTimeOnly.Should().BeFalse();
        iv.DateTime.Should().NotBeNull();
        iv.TimeOfDay.Should().BeNull();
    }

    [Fact]
    public void Full_datetime_value_keeps_the_exact_local_datetime()
    {
        var iv = new InitValueClock("20260601-09:30:00");

        iv.DateTime.Should().Be(new LocalDateTime(2026, 6, 1, 9, 30, 0));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-time")]
    [InlineData("25:00:00")]
    [InlineData("2026-06-01T09:30:00")]
    public void Unparseable_value_throws(string raw)
    {
        var act = () => new InitValueClock(raw);

        act.Should().Throw<InvalidFieldValueException>();
    }
}
