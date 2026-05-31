using System.Globalization;
using FixPortal.FixAtdl.Fix;

namespace FixPortal.FixAtdl.Tests.Fix;

public class FixDateTimeTests
{
    [Fact]
    public void Offsetless_fix_timestamp_parses_as_utc_kind()
    {
        FixDateTime.TryParse("20260601-08:00:00", CultureInfo.InvariantCulture, out DateTime result)
            .Should().BeTrue();

        result.Kind.Should().Be(DateTimeKind.Utc);
        result.Should().Be(new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Fallback_parsed_value_is_also_utc_kind()
    {
        // An ISO-8601 value is not one of the exact FIX formats, so it falls through to the loose parse.
        // That fallback must still yield a canonical Kind=Utc result (previously it returned Unspecified).
        FixDateTime.TryParse("2026-06-01T08:00:00", CultureInfo.InvariantCulture, out DateTime result)
            .Should().BeTrue();

        result.Kind.Should().Be(DateTimeKind.Utc);
    }
}
