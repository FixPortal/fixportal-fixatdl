using FixPortal.FixAtdl.Model.Types.Support;

namespace FixPortal.FixAtdl.Tests.Model.Types.Support;

/// <summary>
/// Characterizes MonthYear struct parsing and round-trip serialization.
/// </summary>
public class MonthYearTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // Valid round-trips
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("202601")]
    [InlineData("202612")]
    [InlineData("000001")]
    public void Parse_roundtrips_YYYYMM_format(string wire)
        => MonthYear.Parse(wire).ToString().Should().Be(wire);

    [Theory]
    [InlineData("20260115")]
    [InlineData("20261231")]
    [InlineData("20260101")]
    public void Parse_roundtrips_YYYYMMDD_format(string wire)
        => MonthYear.Parse(wire).ToString().Should().Be(wire);

    [Fact]
    public void Parse_roundtrips_YYYYMMWW_format()
    {
        // Week suffix: w1..w5
        MonthYear.Parse("202601w3").ToString().Should().Be("202601w3");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Equality and comparison
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Two_identical_MonthYear_values_are_equal()
    {
        var a = MonthYear.Parse("202601");
        var b = MonthYear.Parse("202601");
        a.Should().Be(b);
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }

    [Fact]
    public void Earlier_MonthYear_is_less_than_later()
    {
        var earlier = MonthYear.Parse("202601");
        var later = MonthYear.Parse("202612");
        (earlier < later).Should().BeTrue();
        (later > earlier).Should().BeTrue();
        (earlier <= later).Should().BeTrue();
        (later >= earlier).Should().BeTrue();
    }

    [Fact]
    public void MonthYear_CompareTo_null_returns_positive()
        => MonthYear.Parse("202601").CompareTo(null).Should().BePositive();

    // ──────────────────────────────────────────────────────────────────────────
    // Invalid inputs → ArgumentException
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("2026")]
    [InlineData("202613")]
    [InlineData("20260132")]
    [InlineData("20260230")]
    public void Parse_throws_ArgumentException_for_invalid_input(string bad)
    {
        var act = () => MonthYear.Parse(bad);
        act.Should().Throw<ArgumentException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GetHashCode and Equals (struct contract)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void GetHashCode_is_consistent_with_equality()
    {
        var a = MonthYear.Parse("202601");
        var b = MonthYear.Parse("202601");
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equals_false_for_different_type()
        => MonthYear.Parse("202601").Equals("202601").Should().BeFalse();
}
