using AwesomeAssertions;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Types;
using NSubstitute;

namespace FixPortal.FixAtdl.Tests.Types;

/// <summary>
/// M1: the UTC value-type family must parse a wire value to a canonical <see cref="DateTimeKind.Utc"/>
/// (AssumeUniversal alone yields Kind=Local — a host-offset-dependent defect). Routing through
/// WireParseStyles with AdjustToUniversal fixes it.
/// </summary>
public class UtcTimestampWireParseTests
{
    [Fact]
    public void Utc_wire_value_parses_to_utc_kind()
    {
        var host = Substitute.For<IParameter>();
        var ts = new UTCTimestamp_t();

        ts.SetWireValue(host, "20260115-08:00:00");
        var native = (DateTime)ts.GetNativeValue(false);

        native.Kind.Should().Be(DateTimeKind.Utc);
        native.Should().Be(new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc));
    }
}
