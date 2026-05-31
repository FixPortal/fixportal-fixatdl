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
}
