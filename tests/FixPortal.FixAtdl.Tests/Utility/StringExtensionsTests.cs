using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Model.Reference;
using FixPortal.FixAtdl.Utility;

namespace FixPortal.FixAtdl.Tests.Utility;

/// <summary>
/// Tests for <see cref="StringExtensions.ParseAsEnum{T}"/>, focused on the comma-list guard:
/// <see cref="Enum.Parse{T}(string, bool)"/> treats a comma-separated list as a flags combination for
/// every enum, [Flags] or not, so a non-[Flags] enum must reject such values explicitly rather than
/// silently adopting the OR-ed member.
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
}
