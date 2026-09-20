using FixPortal.FixAtdl.Model.Collections;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Enumerations;
using FixPortal.FixAtdl.Model.Reference;

namespace FixPortal.FixAtdl.Tests.Model.Reference;

/// <summary>
/// Conformance tests for <see cref="Regions"/>: the three region sets transcribe
/// fixatdl-regions-1-1.xsd (FIX Protocol Limited, 2010, build 2.7.2e20101221) verbatim —
/// 50 + 133 + 59 = 242 pairwise-disjoint country codes whose union is exactly the non-None
/// <see cref="IsoCountryCode"/> membership. The 242 expected mappings are carried here as the
/// citable oracle; the schema itself is not vendored (FPL-licensed). The grouping is a FIXatdl
/// business partition, not geography — the schema itself places LK and SR in
/// EuropeMiddleEastAfrica — and must not be "corrected" toward geography (adversarial review
/// 2026-09-12, chunk L06b).
/// </summary>
public class RegionCountriesTests
{
    public static TheoryData<IsoCountryCode, Region> SchemaRegionMappings =>
        new()
        {
            // TheAmericas (50) — schema enumeration lines 29-81
            { IsoCountryCode.AI, Region.TheAmericas },
            { IsoCountryCode.AG, Region.TheAmericas },
            { IsoCountryCode.AR, Region.TheAmericas },
            { IsoCountryCode.AW, Region.TheAmericas },
            { IsoCountryCode.BS, Region.TheAmericas },
            { IsoCountryCode.BB, Region.TheAmericas },
            { IsoCountryCode.BZ, Region.TheAmericas },
            { IsoCountryCode.BM, Region.TheAmericas },
            { IsoCountryCode.BO, Region.TheAmericas },
            { IsoCountryCode.BR, Region.TheAmericas },
            { IsoCountryCode.CA, Region.TheAmericas },
            { IsoCountryCode.KY, Region.TheAmericas },
            { IsoCountryCode.CL, Region.TheAmericas },
            { IsoCountryCode.CO, Region.TheAmericas },
            { IsoCountryCode.CR, Region.TheAmericas },
            { IsoCountryCode.CU, Region.TheAmericas },
            { IsoCountryCode.DM, Region.TheAmericas },
            { IsoCountryCode.DO, Region.TheAmericas },
            { IsoCountryCode.EC, Region.TheAmericas },
            { IsoCountryCode.SV, Region.TheAmericas },
            { IsoCountryCode.FK, Region.TheAmericas },
            { IsoCountryCode.GD, Region.TheAmericas },
            { IsoCountryCode.GP, Region.TheAmericas },
            { IsoCountryCode.GT, Region.TheAmericas },
            { IsoCountryCode.GY, Region.TheAmericas },
            { IsoCountryCode.HT, Region.TheAmericas },
            { IsoCountryCode.HN, Region.TheAmericas },
            { IsoCountryCode.JM, Region.TheAmericas },
            { IsoCountryCode.MQ, Region.TheAmericas },
            { IsoCountryCode.MX, Region.TheAmericas },
            { IsoCountryCode.MS, Region.TheAmericas },
            { IsoCountryCode.AN, Region.TheAmericas },
            { IsoCountryCode.NI, Region.TheAmericas },
            { IsoCountryCode.PA, Region.TheAmericas },
            { IsoCountryCode.PY, Region.TheAmericas },
            { IsoCountryCode.PE, Region.TheAmericas },
            { IsoCountryCode.PR, Region.TheAmericas },
            { IsoCountryCode.BL, Region.TheAmericas },
            { IsoCountryCode.KN, Region.TheAmericas },
            { IsoCountryCode.LC, Region.TheAmericas },
            { IsoCountryCode.MF, Region.TheAmericas },
            { IsoCountryCode.PM, Region.TheAmericas },
            { IsoCountryCode.VC, Region.TheAmericas },
            { IsoCountryCode.TT, Region.TheAmericas },
            { IsoCountryCode.TC, Region.TheAmericas },
            { IsoCountryCode.US, Region.TheAmericas },
            { IsoCountryCode.UY, Region.TheAmericas },
            { IsoCountryCode.VG, Region.TheAmericas },
            { IsoCountryCode.VI, Region.TheAmericas },
            { IsoCountryCode.VE, Region.TheAmericas },
            // EuropeMiddleEastAfrica (133) — schema enumeration lines 85-220
            { IsoCountryCode.AX, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.AL, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.DZ, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.AD, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.AO, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.AM, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.AT, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.AZ, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.BH, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.BY, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.BE, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.BJ, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.BA, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.BW, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.BG, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.BF, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.BI, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.CM, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.CV, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.CF, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.TD, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.KM, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.CG, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.CD, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.CI, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.HR, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.CY, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.CZ, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.DK, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.DJ, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.EG, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.GQ, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.ER, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.EE, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.ET, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.FO, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.FI, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.FR, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.GF, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.GA, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.GM, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.GE, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.DE, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.GH, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.GI, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.GR, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.GL, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.GG, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.GN, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.GW, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.VA, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.HU, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.IS, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.IR, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.IQ, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.IE, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.IM, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.IL, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.IT, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.JE, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.JO, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.KE, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.KW, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.LV, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.LB, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.LS, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.LR, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.LY, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.LI, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.LT, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.LU, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.MK, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.MG, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.MW, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.ML, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.MT, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.MR, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.MU, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.YT, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.MD, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.MC, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.ME, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.MA, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.MZ, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.NA, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.NL, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.NE, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.NG, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.NO, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.OM, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.PS, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.PN, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.PL, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.PT, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.QA, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.RE, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.RO, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.RU, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.RW, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.SH, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.SM, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.ST, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.SA, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.SN, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.RS, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.SC, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.SL, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.SK, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.SI, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.SO, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.ZA, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.GS, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.ES, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.LK, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.SD, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.SR, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.SJ, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.SZ, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.SE, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.CH, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.SY, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.EH, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.TZ, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.TG, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.TN, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.TR, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.UG, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.UA, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.AE, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.GB, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.YE, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.ZM, Region.EuropeMiddleEastAfrica },
            { IsoCountryCode.ZW, Region.EuropeMiddleEastAfrica },
            // AsiaPacificJapan (59) — schema enumeration lines 224-285
            { IsoCountryCode.AF, Region.AsiaPacificJapan },
            { IsoCountryCode.AS, Region.AsiaPacificJapan },
            { IsoCountryCode.AU, Region.AsiaPacificJapan },
            { IsoCountryCode.BD, Region.AsiaPacificJapan },
            { IsoCountryCode.BT, Region.AsiaPacificJapan },
            { IsoCountryCode.IO, Region.AsiaPacificJapan },
            { IsoCountryCode.BN, Region.AsiaPacificJapan },
            { IsoCountryCode.KH, Region.AsiaPacificJapan },
            { IsoCountryCode.CN, Region.AsiaPacificJapan },
            { IsoCountryCode.CX, Region.AsiaPacificJapan },
            { IsoCountryCode.CC, Region.AsiaPacificJapan },
            { IsoCountryCode.CK, Region.AsiaPacificJapan },
            { IsoCountryCode.FJ, Region.AsiaPacificJapan },
            { IsoCountryCode.PF, Region.AsiaPacificJapan },
            { IsoCountryCode.GU, Region.AsiaPacificJapan },
            { IsoCountryCode.HK, Region.AsiaPacificJapan },
            { IsoCountryCode.IN, Region.AsiaPacificJapan },
            { IsoCountryCode.ID, Region.AsiaPacificJapan },
            { IsoCountryCode.JP, Region.AsiaPacificJapan },
            { IsoCountryCode.KZ, Region.AsiaPacificJapan },
            { IsoCountryCode.KI, Region.AsiaPacificJapan },
            { IsoCountryCode.KP, Region.AsiaPacificJapan },
            { IsoCountryCode.KR, Region.AsiaPacificJapan },
            { IsoCountryCode.KG, Region.AsiaPacificJapan },
            { IsoCountryCode.LA, Region.AsiaPacificJapan },
            { IsoCountryCode.MO, Region.AsiaPacificJapan },
            { IsoCountryCode.MY, Region.AsiaPacificJapan },
            { IsoCountryCode.MV, Region.AsiaPacificJapan },
            { IsoCountryCode.MH, Region.AsiaPacificJapan },
            { IsoCountryCode.FM, Region.AsiaPacificJapan },
            { IsoCountryCode.MN, Region.AsiaPacificJapan },
            { IsoCountryCode.MM, Region.AsiaPacificJapan },
            { IsoCountryCode.NR, Region.AsiaPacificJapan },
            { IsoCountryCode.NP, Region.AsiaPacificJapan },
            { IsoCountryCode.NC, Region.AsiaPacificJapan },
            { IsoCountryCode.NZ, Region.AsiaPacificJapan },
            { IsoCountryCode.NU, Region.AsiaPacificJapan },
            { IsoCountryCode.NF, Region.AsiaPacificJapan },
            { IsoCountryCode.MP, Region.AsiaPacificJapan },
            { IsoCountryCode.PK, Region.AsiaPacificJapan },
            { IsoCountryCode.PW, Region.AsiaPacificJapan },
            { IsoCountryCode.PG, Region.AsiaPacificJapan },
            { IsoCountryCode.PH, Region.AsiaPacificJapan },
            { IsoCountryCode.WS, Region.AsiaPacificJapan },
            { IsoCountryCode.SG, Region.AsiaPacificJapan },
            { IsoCountryCode.SB, Region.AsiaPacificJapan },
            { IsoCountryCode.TW, Region.AsiaPacificJapan },
            { IsoCountryCode.TJ, Region.AsiaPacificJapan },
            { IsoCountryCode.TH, Region.AsiaPacificJapan },
            { IsoCountryCode.TL, Region.AsiaPacificJapan },
            { IsoCountryCode.TK, Region.AsiaPacificJapan },
            { IsoCountryCode.TO, Region.AsiaPacificJapan },
            { IsoCountryCode.TM, Region.AsiaPacificJapan },
            { IsoCountryCode.TV, Region.AsiaPacificJapan },
            { IsoCountryCode.UM, Region.AsiaPacificJapan },
            { IsoCountryCode.UZ, Region.AsiaPacificJapan },
            { IsoCountryCode.VU, Region.AsiaPacificJapan },
            { IsoCountryCode.VN, Region.AsiaPacificJapan },
            { IsoCountryCode.WF, Region.AsiaPacificJapan },
        };

    [Theory]
    [MemberData(nameof(SchemaRegionMappings))]
    public void GetRegionForCountry_maps_each_schema_code_to_its_schema_region(IsoCountryCode country, Region expected)
    {
        Regions.GetRegionForCountry(country).Should().Be(expected);
    }

    [Fact]
    public void GetRegionForCountry_None_returns_None()
    {
        Regions.GetRegionForCountry(IsoCountryCode.None).Should().Be(Region.None);
    }

    [Fact]
    public void Region_sets_partition_the_242_schema_codes()
    {
        Regions.TheAmericasCountries.Should().HaveCount(50);
        Regions.EuropeMiddleEastAfricaCountries.Should().HaveCount(133);
        Regions.AsiaPacificJapanCountries.Should().HaveCount(59);

        // Pairwise-disjoint, and their union is exactly the non-None IsoCountryCode membership.
        Regions
            .TheAmericasCountries.Concat(Regions.EuropeMiddleEastAfricaCountries)
            .Concat(Regions.AsiaPacificJapanCountries)
            .Should()
            .OnlyHaveUniqueItems()
            .And.BeEquivalentTo(Enum.GetValues<IsoCountryCode>().Where(c => c != IsoCountryCode.None));
    }

    [Fact]
    public void Region_t_accepts_a_counter_geographic_country_the_schema_places_there()
    {
        // Load-path pin for the adjudicated L06b refutation: the schema places LK in
        // EuropeMiddleEastAfrica (fixatdl-regions-1-1.xsd line 198), so a Region declaring it must
        // not throw — "correcting" the table toward geography would reject schema-valid documents.
        var region = new Region_t { Name = Region.EuropeMiddleEastAfrica, Inclusion = Inclusion_t.Include };
        var countries = new CountryCollection(region);

        var act = () => countries.Add(new Country_t { CountryCode = IsoCountryCode.LK });

        act.Should().NotThrow();
    }
}
