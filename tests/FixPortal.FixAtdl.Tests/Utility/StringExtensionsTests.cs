using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Model.Reference;
using FixPortal.FixAtdl.Utility;

namespace FixPortal.FixAtdl.Tests.Utility;

/// <summary>
/// Tests for <see cref="StringExtensions.ParseAsEnum{T}"/>, focused on the comma-list and numeric
/// guards: <see cref="Enum.Parse{T}(string, bool)"/> treats a comma-separated list as a flags
/// combination for every enum, [Flags] or not, and accepts a bare numeric value that matches a
/// defined member, so both spellings must be rejected explicitly rather than silently adopted.
/// </summary>
public class StringExtensionsTests
{
    [Fact]
    public void ParseAsEnum_rejects_comma_list_for_non_flags_enum()
    {
        // "AED,AFN" would otherwise parse to 1 | 2 = 3 = IsoCurrencyCode.ALL — a defined member that
        // passes the undefined-value guard while being a silently different currency.
        var act = () => "AED,AFN".ParseAsEnum<IsoCurrencyCode>();

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ParseAsEnum_still_allows_comma_list_for_flags_enum()
    {
        // Region is [Flags]: a comma combination is a legitimate composite, and the result (5) is not
        // itself a defined member — pinning both the comma path and the IsDefined exemption.
        Region result = "TheAmericas,AsiaPacificJapan".ParseAsEnum<Region>();

        result.Should().Be(Region.TheAmericas | Region.AsiaPacificJapan);
    }

    [Fact]
    public void ParseAsEnum_remains_case_insensitive_for_single_values()
    {
        IsoCurrencyCode result = "aed".ParseAsEnum<IsoCurrencyCode>();

        result.Should().Be(IsoCurrencyCode.AED);
    }

    [Theory]
    [InlineData("3")]
    [InlineData("036")]
    [InlineData("840")]
    [InlineData(" 3 ")]
    [InlineData("+3")]
    public void ParseAsEnum_rejects_bare_numeric_strings_for_non_flags_enum(string wire)
    {
        // "3" is a defined member's value (IsoCurrencyCode.ALL) and "036" parses to 36: both slip
        // past the undefined-value guard as silently wrong currencies. Enum.Parse's numeric surface
        // includes surrounding whitespace and a leading sign, so those spellings are rejected too.
        var act = () => wire.ParseAsEnum<IsoCurrencyCode>();

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("3")]
    [InlineData("7")]
    public void ParseAsEnum_rejects_bare_numeric_strings_for_flags_enum(string wire)
    {
        // "3" is a legitimate composite for [Flags] Region (TheAmericas | EuropeMiddleEastAfrica) and
        // "7" is the defined member All, but a region value is a name, never a number — the numeric
        // rejection applies to flags enums too.
        var act = () => wire.ParseAsEnum<Region>();

        act.Should().Throw<ArgumentException>();
    }
}
