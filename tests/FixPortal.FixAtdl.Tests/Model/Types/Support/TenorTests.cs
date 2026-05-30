using FixPortal.FixAtdl.Model.Types.Support;

namespace FixPortal.FixAtdl.Tests.Model.Types.Support;

/// <summary>
/// Characterizes Tenor struct parsing and round-trip serialization.
/// </summary>
public class TenorTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // Valid round-trips
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("D5")]
    [InlineData("D1")]
    [InlineData("D365")]
    public void Parse_roundtrips_day_tenors(string wire)
        => Tenor.Parse(wire).ToString().Should().Be(wire);

    [Theory]
    [InlineData("W1")]
    [InlineData("W13")]
    [InlineData("W52")]
    public void Parse_roundtrips_week_tenors(string wire)
        => Tenor.Parse(wire).ToString().Should().Be(wire);

    [Theory]
    [InlineData("M1")]
    [InlineData("M3")]
    [InlineData("M12")]
    public void Parse_roundtrips_month_tenors(string wire)
        => Tenor.Parse(wire).ToString().Should().Be(wire);

    [Theory]
    [InlineData("Y1")]
    [InlineData("Y2")]
    [InlineData("Y10")]
    public void Parse_roundtrips_year_tenors(string wire)
        => Tenor.Parse(wire).ToString().Should().Be(wire);

    // ──────────────────────────────────────────────────────────────────────────
    // Equality and comparison
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Two_identical_Tenor_values_are_equal()
    {
        var a = Tenor.Parse("M3");
        var b = Tenor.Parse("M3");
        a.Should().Be(b);
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }

    [Fact]
    public void Shorter_tenor_is_less_than_longer_same_unit()
    {
        var shorter = Tenor.Parse("M1");
        var longer = Tenor.Parse("M6");
        (shorter < longer).Should().BeTrue();
        (longer > shorter).Should().BeTrue();
    }

    [Fact]
    public void Cross_unit_comparison_uses_approximate_days()
    {
        // D7 (~7 days) < M1 (~30 days)
        var days = Tenor.Parse("D7");
        var months = Tenor.Parse("M1");
        (days < months).Should().BeTrue();
    }

    [Fact]
    public void Tenor_CompareTo_null_returns_positive()
        => Tenor.Parse("M3").CompareTo(null).Should().BePositive();

    // ──────────────────────────────────────────────────────────────────────────
    // Invalid inputs → ArgumentException
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("X")]
    [InlineData("X5")]
    [InlineData("M")]
    public void Parse_throws_ArgumentException_for_invalid_input(string bad)
    {
        var act = () => Tenor.Parse(bad);
        act.Should().Throw<ArgumentException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GetHashCode and Equals (struct contract)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void GetHashCode_is_consistent_with_equality()
    {
        var a = Tenor.Parse("W4");
        var b = Tenor.Parse("W4");
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equals_false_for_non_Tenor_object()
        => Tenor.Parse("M3").Equals("M3").Should().BeFalse();
}
