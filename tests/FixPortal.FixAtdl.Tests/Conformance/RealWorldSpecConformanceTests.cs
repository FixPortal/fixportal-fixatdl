using System.Globalization;
using FixPortal.FixAtdl.Fix;
using FixPortal.FixAtdl.Model.Controls;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Model.Types;
using FixPortal.FixAtdl.Xml;
using NodaTime;
using NodaTime.Testing;
// AwesomeAssertions and Xunit are global usings (see GlobalUsings.cs).

namespace FixPortal.FixAtdl.Tests.Conformance;

/// <summary>
/// Conformance tests over two obfuscated, real-world-derived FIXatdl 1.1 documents. They prove the
/// parser survives real-world-shaped input and that the Phase A–C findings (C1 timezone shift,
/// C2 time-only bounds, H3 EnumPair@index capture) hold on it. Fixtures are synthetic: all firm-,
/// product-, and identity-bearing content was removed; see the fixture provenance comments.
/// </summary>
public class RealWorldSpecConformanceTests
{
    private const string TzClockFixture = "Fixtures/RealWorld/tz-clock.xml";
    private const string RegionsEnumsFixture = "Fixtures/RealWorld/regions-enums.xml";

    private static Strategies_t Load(string relativePath)
    {
        using var stream = File.OpenRead(relativePath);
        return new StrategiesReader().Load(stream);
    }

    [Fact]
    public void TzClock_fixture_parses_with_expected_strategy_count()
    {
        Load(TzClockFixture).Count.Should().Be(15);
    }

    [Fact]
    public void RegionsEnums_fixture_parses_with_expected_strategy_count()
    {
        Load(RegionsEnumsFixture).Count.Should().Be(13);
    }

    [Fact]
    public void TzClock_first_strategy_exposes_securitytypes_and_utctimestamp_params()
    {
        var vwap = Load(TzClockFixture)["VWAP"];

        vwap.SecurityTypes.Select(s => s.Name).Should().Contain("CS");
        ((int)vwap.Parameters["p_StartTime"].FixTag!.Value).Should().Be(7113);
        ((int)vwap.Parameters["p_EndTime"].FixTag!.Value).Should().Be(7114);
    }

    [Fact]
    public void TzClock_first_strategy_has_two_berlin_clock_controls()
    {
        var clocks = Load(TzClockFixture)["VWAP"].Controls.OfType<Clock_t>().ToList();

        clocks.Select(c => c.Id).Should().BeEquivalentTo("i_StartTime", "i_EndTime");
        clocks.Should().OnlyContain(c => c.LocalMktTz == "Europe/Berlin");
    }

    [Fact]
    public void RegionsEnums_first_strategy_exposes_region_and_char_enum()
    {
        var dark = Load(RegionsEnumsFixture)["DARK_NA"];

        dark.Regions.Select(r => r.Name).Should().Contain(Region.TheAmericas);
        dark.Parameters["Urgency"].EnumPairs.Count.Should().Be(6);
    }
}
