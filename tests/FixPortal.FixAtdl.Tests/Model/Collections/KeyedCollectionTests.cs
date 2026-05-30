using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Model.Reference;
using FixPortal.FixAtdl.Validation;

namespace FixPortal.FixAtdl.Tests.Model.Collections;

/// <summary>
/// Characterisation tests for the trivial KeyedCollection subclasses and the
/// CountryCollection / EditCollection helpers.
/// </summary>
public class KeyedCollectionTests
{
    // -----------------------------------------------------------------------
    // EnumPairCollection
    // -----------------------------------------------------------------------

    [Fact]
    public void EnumPairCollection_resolves_wire_value_by_enum_id()
    {
        var pairs = new EnumPairCollection
        {
            new EnumPair_t { EnumId = "BUY",  WireValue = "1" },
            new EnumPair_t { EnumId = "SELL", WireValue = "2" },
        };

        pairs.GetWireValueFromEnumId("SELL").Should().Be("2");
        pairs.TryParseWireValue("1", out var enumId).Should().BeTrue();
        enumId.Should().Be("BUY");
    }

    [Fact]
    public void EnumPairCollection_throws_for_unknown_enum_id()
    {
        var pairs = new EnumPairCollection();
        var act = () => pairs.GetWireValueFromEnumId("NOPE");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void EnumPairCollection_TryParseWireValue_returns_false_for_missing_wire_value()
    {
        var pairs = new EnumPairCollection
        {
            new EnumPair_t { EnumId = "BUY", WireValue = "1" },
        };

        pairs.TryParseWireValue("999", out var enumId).Should().BeFalse();
        enumId.Should().BeNull();
    }

    [Fact]
    public void EnumPairCollection_HasValues_reflects_count()
    {
        var empty = new EnumPairCollection();
        empty.HasValues.Should().BeFalse();

        var nonempty = new EnumPairCollection { new EnumPair_t { EnumId = "X", WireValue = "x" } };
        nonempty.HasValues.Should().BeTrue();
    }

    [Fact]
    public void EnumPairCollection_EnumIds_returns_all_ids()
    {
        var pairs = new EnumPairCollection
        {
            new EnumPair_t { EnumId = "A", WireValue = "1" },
            new EnumPair_t { EnumId = "B", WireValue = "2" },
        };

        pairs.EnumIds.Should().BeEquivalentTo(new[] { "A", "B" });
    }

    // -----------------------------------------------------------------------
    // MarketCollection
    // -----------------------------------------------------------------------

    [Fact]
    public void MarketCollection_add_and_retrieve_by_MICCode()
    {
        var markets = new MarketCollection();
        var xlon = new Market_t { MICCode = "XLON", Inclusion = Inclusion_t.Include };
        markets.Add(xlon);

        markets.Count.Should().Be(1);
        markets["XLON"].Should().BeSameAs(xlon);
        markets.Contains("XLON").Should().BeTrue();
        markets.Contains("XNAS").Should().BeFalse();
    }

    [Fact]
    public void MarketCollection_supports_multiple_markets()
    {
        var markets = new MarketCollection
        {
            new Market_t { MICCode = "XLON", Inclusion = Inclusion_t.Include },
            new Market_t { MICCode = "XNAS", Inclusion = Inclusion_t.Include },
        };

        markets.Count.Should().Be(2);
        markets.Contains("XNAS").Should().BeTrue();
    }

    // -----------------------------------------------------------------------
    // SecurityTypeCollection
    // -----------------------------------------------------------------------

    [Fact]
    public void SecurityTypeCollection_add_and_retrieve_by_name()
    {
        var types = new SecurityTypeCollection();
        var cs = new SecurityType_t { Name = "CS", Inclusion = Inclusion_t.Include };
        types.Add(cs);

        types.Count.Should().Be(1);
        types["CS"].Should().BeSameAs(cs);
        types.Contains("CS").Should().BeTrue();
        types.Contains("OPT").Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // RegionCollection
    // -----------------------------------------------------------------------

    [Fact]
    public void RegionCollection_empty_returns_All()
    {
        var regions = new RegionCollection();
        regions.GetApplicableRegions().Should().Be(Region.All);
    }

    [Fact]
    public void RegionCollection_single_include_returns_that_region()
    {
        var regions = new RegionCollection
        {
            new Region_t { Name = Region.TheAmericas, Inclusion = Inclusion_t.Include },
        };

        regions.GetApplicableRegions().Should().Be(Region.TheAmericas);
    }

    [Fact]
    public void RegionCollection_exclude_only_returns_no_applicable_region()
    {
        // NOTE: Only excluded entries → result is Region.None (no ORed-in includes).
        var regions = new RegionCollection
        {
            new Region_t { Name = Region.TheAmericas, Inclusion = Inclusion_t.Exclude },
        };

        regions.GetApplicableRegions().Should().Be(Region.None);
    }

    [Fact]
    public void RegionCollection_IsApplicableTo_matches_included_region()
    {
        var regions = new RegionCollection
        {
            new Region_t { Name = Region.TheAmericas, Inclusion = Inclusion_t.Include },
        };

        // US is in TheAmericas
        regions.IsApplicableTo(IsoCountryCode.US).Should().BeTrue();
    }

    [Fact]
    public void RegionCollection_IsApplicableTo_rejects_excluded_country()
    {
        var regions = new RegionCollection
        {
            new Region_t { Name = Region.TheAmericas, Inclusion = Inclusion_t.Include },
        };

        // AU is in AsiaPacificJapan, not TheAmericas
        regions.IsApplicableTo(IsoCountryCode.AU).Should().BeFalse();
    }

    [Fact]
    public void RegionCollection_empty_IsApplicableTo_always_true()
    {
        // Empty → All → every country matches
        var regions = new RegionCollection();
        regions.IsApplicableTo(IsoCountryCode.AU).Should().BeTrue();
        regions.IsApplicableTo(IsoCountryCode.GB).Should().BeTrue();
    }

    // -----------------------------------------------------------------------
    // CountryCollection
    // -----------------------------------------------------------------------

    [Fact]
    public void CountryCollection_add_country_in_region_succeeds()
    {
        var region = new Region_t { Name = Region.TheAmericas, Inclusion = Inclusion_t.Include };
        var countries = new CountryCollection(region);
        var us = new Country_t { CountryCode = IsoCountryCode.US };

        countries.Add(us).Should().BeTrue();
        countries.Count.Should().Be(1);
        countries.Contains(us).Should().BeTrue();
    }

    [Fact]
    public void CountryCollection_duplicate_add_returns_false()
    {
        var region = new Region_t { Name = Region.TheAmericas, Inclusion = Inclusion_t.Include };
        var countries = new CountryCollection(region);
        var us = new Country_t { CountryCode = IsoCountryCode.US };

        countries.Add(us);
        countries.Add(us).Should().BeFalse();  // HashSet returns false for duplicate
    }

    [Fact]
    public void CountryCollection_add_country_outside_region_throws()
    {
        var region = new Region_t { Name = Region.TheAmericas, Inclusion = Inclusion_t.Include };
        var countries = new CountryCollection(region);
        var au = new Country_t { CountryCode = IsoCountryCode.AU }; // APJ, not Americas

        var act = () => countries.Add(au);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CountryCollection_is_enumerable()
    {
        Region_t region = new() { Name = Region.TheAmericas, Inclusion = Inclusion_t.Include };
        CountryCollection countries = new(region)
        {
            new Country_t { CountryCode = IsoCountryCode.US },
            new Country_t { CountryCode = IsoCountryCode.CA },
        };

        var codes = countries.Select(c => c.CountryCode).ToList();
        codes.Should().Contain(IsoCountryCode.US);
        codes.Should().Contain(IsoCountryCode.CA);
    }

    // -----------------------------------------------------------------------
    // EditCollection
    // -----------------------------------------------------------------------

    [Fact]
    public void EditCollection_HasEdit_returns_true_when_present()
    {
        EditCollection edits =
        [
            new Edit_t { Id = "edit1", Field = "Qty", Operator = Operator_t.GreaterThan, Value = "0" },
        ];

        edits.HasEdit("edit1").Should().BeTrue();
        edits.HasEdit("missing").Should().BeFalse();
    }

    [Fact]
    public void EditCollection_Clone_returns_typed_copy()
    {
        var source = new Edit_t
        {
            Id = "myEdit",
            Field = "Participation",
            Operator = Operator_t.GreaterThan,
            Value = "0"
        };
        EditCollection edits = [source];

        var clone = edits.Clone<IParameter>("myEdit");

        clone.Should().NotBeNull();
        clone.Id.Should().Be("myEdit");
        clone.Field.Should().Be("Participation");
        clone.Operator.Should().Be(Operator_t.GreaterThan);
        clone.Value.Should().Be("0");
        clone.Should().NotBeSameAs(source); // it's a copy
    }

    [Fact]
    public void EditCollection_Clone_throws_ReferencedObjectNotFoundException_for_missing_id()
    {
        var edits = new EditCollection();
        var act = () => edits.Clone<IParameter>("doesNotExist");
        act.Should().Throw<ReferencedObjectNotFoundException>();
    }
}
