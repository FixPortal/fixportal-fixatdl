using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Model.Elements.Support;
using FixPortal.FixAtdl.Model.Types;

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

    [Fact]
    public void Leap_second_wire_value_rolls_forward_like_the_control_and_const_paths()
    {
        // The UTCTimestamp_t spec's worked example (19981231-23:59:60 -> 19990101-00:00:00) was
        // already honoured on the const/control paths; the wire path must agree (R05).
        var host = Substitute.For<IParameter>();
        var ts = new UTCTimestamp_t();

        ts.SetWireValue(host, "19981231-23:59:60");

        ((DateTime)ts.GetNativeValue(false)).Should().Be(new DateTime(1999, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        ts.GetWireValue(host).Should().Be("19990101-00:00:00");
    }

    [Fact]
    public void Leap_second_beyond_the_last_representable_instant_is_rejected_cleanly()
    {
        // 9999-12-31 23:59:60 cannot roll forward; the value must be rejected as invalid, not
        // escape as a raw overflow from AddSeconds.
        var host = Substitute.For<IParameter>();
        var ts = new UTCTimestamp_t();

        var act = () => ts.SetWireValue(host, "99991231-23:59:60");

        act.Should().Throw<InvalidFieldValueException>();
    }
}
