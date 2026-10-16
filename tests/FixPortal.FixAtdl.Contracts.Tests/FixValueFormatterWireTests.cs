using System.Text.Json;
using AwesomeAssertions;
using Xunit;

namespace FixPortal.FixAtdl.Contracts.Tests;

/// <summary>Exercises the formatter directly, independently of mapper scalar shortcuts.</summary>
public sealed class FixValueFormatterWireTests
{
    /// <summary>Both CLR and JSON Boolean inputs use FIX Y/N literals.</summary>
    [Theory]
    [InlineData(true, "true", "Y")]
    [InlineData(false, "false", "N")]
    public void Boolean_values_use_FIX_literals(bool value, string json, string expected)
    {
        FixValueFormatter.Format(value, "Boolean_t", null).Should().Be(expected);
        using var document = JsonDocument.Parse(json);
        FixValueFormatter.Format(document.RootElement, "Boolean_t", null).Should().Be(expected);
    }

    public static TheoryData<object, string, string> TemporalCases =>
        new()
        {
            { new DateTime(2026, 10, 16, 8, 15, 30, 123, DateTimeKind.Utc), "UTCTimestamp_t", "20261016-08:15:30.123" },
            {
                new DateTimeOffset(2026, 10, 16, 0, 15, 30, 123, TimeSpan.FromHours(2)),
                "UTCTimestamp_t",
                "20261015-22:15:30.123"
            },
            { new DateTime(2026, 10, 16, 8, 15, 30, DateTimeKind.Utc), "UTCDateOnly_t", "20261016" },
            { new DateTimeOffset(2026, 10, 16, 0, 15, 30, TimeSpan.FromHours(2)), "LocalMktDate_t", "20261016" },
            { new DateOnly(2026, 10, 16), "UTCDateOnly_t", "20261016" },
            { new DateOnly(2026, 10, 16), "LocalMktDate_t", "20261016" },
            { new DateTime(2026, 10, 16, 8, 15, 30, 123, DateTimeKind.Utc), "UTCTimeOnly_t", "08:15:30.123" },
            {
                new DateTimeOffset(2026, 10, 16, 0, 15, 30, 123, TimeSpan.FromHours(2)),
                "UTCTimeOnly_t",
                "22:15:30.123"
            },
            { new TimeOnly(8, 15, 30, 123), "UTCTimeOnly_t", "08:15:30.123" },
            { new TimeSpan(0, 8, 15, 30, 123), "UTCTimeOnly_t", "08:15:30.123" },
            { new DateTime(2026, 10, 16, 8, 15, 30, DateTimeKind.Utc), "MonthYear_t", "202610" },
            { new DateTimeOffset(2026, 10, 16, 0, 15, 30, TimeSpan.FromHours(2)), "MonthYear_t", "202610" },
            { new DateOnly(2026, 10, 16), "MonthYear_t", "202610" },
        };

    /// <summary>Temporal CLR values produce exact FIX wire text including UTC rollover.</summary>
    [Theory]
    [MemberData(nameof(TemporalCases))]
    public void Temporal_values_use_exact_FIX_formats(object value, string type, string expected) =>
        FixValueFormatter.Format(value, type, null).Should().Be(expected);

    public static TheoryData<object, string> NonFiniteCases
    {
        get
        {
            var cases = new TheoryData<object, string>();
            object[] values =
            [
                double.NaN,
                double.PositiveInfinity,
                double.NegativeInfinity,
                float.NaN,
                float.PositiveInfinity,
                float.NegativeInfinity,
            ];
            foreach (var type in new[] { "Float_t", "Qty_t", "Price_t", "PriceOffset_t", "Amt_t", "Percentage_t" })
            {
                foreach (var value in values)
                {
                    cases.Add(value, type);
                }
            }
            return cases;
        }
    }

    /// <summary>Every FIX floating numeric type rejects each non-finite CLR representation.</summary>
    [Theory]
    [MemberData(nameof(NonFiniteCases))]
    public void Non_finite_numbers_are_rejected(object value, string type)
    {
        var format = () => FixValueFormatter.Format(value, type, null);
        format.Should().Throw<ArgumentOutOfRangeException>().WithParameterName(nameof(value));
    }
}
