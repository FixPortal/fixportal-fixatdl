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

    private static Clock_t StartTimeClock(Instant now)
    {
        var clock = Load(TzClockFixture)["VWAP"].Controls
            .OfType<Clock_t>().First(c => c.Id == "i_StartTime");
        clock.Clock = new FakeClock(now);
        clock.InitValueMode = 0;
        clock.LoadInitValue(FixFieldValueProvider.Empty);
        return clock;
    }

    [Fact]
    public void C1_berlin_0800_initValue_emits_0700_utc_in_winter()
    {
        // 2026-01-15 CET (UTC+1): 08:00 Berlin -> 07:00Z.
        StartTimeClock(Instant.FromUtc(2026, 1, 15, 12, 0, 0))
            .ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should().Be(new DateTime(2026, 1, 15, 7, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void C1_berlin_0800_initValue_emits_0600_utc_in_summer()
    {
        // 2026-07-15 CEST (UTC+2): 08:00 Berlin -> 06:00Z.
        StartTimeClock(Instant.FromUtc(2026, 7, 15, 12, 0, 0))
            .ToDateTime(null!, CultureInfo.InvariantCulture)
            .Should().Be(new DateTime(2026, 7, 15, 6, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void C2_time_only_maxValue_is_captured_as_text()
    {
        var endTime = (Parameter_t<UTCTimestamp_t>)Load(TzClockFixture)["VWAP"].Parameters["p_EndTime"];
        endTime.Value.MaxValueText.Should().Be("23:59:59");
    }

    [Theory]
    [InlineData("20260101-23:59:59")]
    [InlineData("20991231-23:59:59")]
    public void C2_time_only_maxValue_accepts_valid_time_on_any_date(string wireValue)
    {
        var endTime = Load(TzClockFixture)["VWAP"].Parameters["p_EndTime"];
        var act = () => endTime.WireValue = wireValue;
        act.Should().NotThrow();
    }

    [Fact]
    public void H3_enumPair_index_is_captured_including_non_contiguous_values()
    {
        var ordType = Load(RegionsEnumsFixture)["SOR_NA"].Parameters["OrdType"];

        ordType.EnumPairs["o1"].Index.Should().Be(0);
        ordType.EnumPairs["o2"].Index.Should().Be(1);
        ordType.EnumPairs["o3"].Index.Should().Be(3); // non-contiguous: index 2 is skipped
        ordType.EnumPairs["o4"].Index.Should().Be(4);
    }

    [Fact]
    public void H3_enumPair_index_does_not_perturb_wire_value()
    {
        var ordType = Load(RegionsEnumsFixture)["SOR_NA"].Parameters["OrdType"];
        ordType.EnumPairs["o1"].WireValue.Should().Be("1");
    }

    [Fact]
    public void H3_enumPair_index_is_null_when_attribute_absent()
    {
        var benchmark = Load(TzClockFixture)["VWAP"].Parameters["p_Benchmark"];
        benchmark.EnumPairs["e_Default"].Index.Should().BeNull();
    }
}
