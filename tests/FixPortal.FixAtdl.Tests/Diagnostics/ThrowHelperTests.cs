using System.Xml.Linq;
using FixPortal.FixAtdl.Diagnostics;
using FixPortal.FixAtdl.Diagnostics.Exceptions;

namespace FixPortal.FixAtdl.Tests.Diagnostics;

/// <summary>
/// Tests for <see cref="ThrowHelper"/> exception construction, focused on the
/// ArgumentException-family ParamName threading (G-D).
/// </summary>
public class ThrowHelperTests
{
    [Fact]
    public void NewWithParamName_threads_supplied_param_name()
    {
        // The two-string constructor of ArgumentException-family types takes the parameter name
        // first and the message second; ThrowHelper must surface the real parameter name here,
        // not only the historical synthetic placeholder name.
        ArgumentOutOfRangeException ex = ThrowHelper.NewWithParamName<ArgumentOutOfRangeException>(
            source: null,
            paramName: "tenorOffset",
            message: "out of range"
        );

        ex.ParamName.Should().Be("tenorOffset");
    }

    [Fact]
    public void New_without_param_name_defaults_to_Value_for_argument_exceptions()
    {
        // Back-compat: the plain New<T> path keeps the historical synthetic "Value" name.
        ArgumentOutOfRangeException ex = ThrowHelper.New<ArgumentOutOfRangeException>(null, "out of range");

        ex.ParamName.Should().Be("Value");
    }

    [Fact]
    public void New_params_overload_preserves_literal_braces_when_no_arguments_are_supplied()
    {
        string message = new(['{', 'N', 'U', 'L', 'L', '}']);

        var ex = ThrowHelper.New<InvalidOperationException>(null, message, []);

        ex.Message.Should().Be(message);
    }

    [Fact]
    public void Rethrow_formats_the_outer_message_once()
    {
        var inner = new InvalidOperationException("inner");

        var ex = ThrowHelper.Rethrow(null, inner, "Could not parse {0}", "{NULL}", "unused");

        ex.Should().BeOfType<InvalidOperationException>();
        ex.Message.Should().Be("Could not parse {NULL}");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void Rethrow_single_argument_overload_preserves_inner_exception()
    {
        var inner = new InvalidOperationException("inner");

        var ex = ThrowHelper.Rethrow(null, inner, "Could not parse {0}", "field");

        ex.Message.Should().Be("Could not parse field");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void Rethrow_xml_overload_preserves_inner_exception()
    {
        var inner = new InvalidOperationException("inner");
        var xml = new XElement("Root");

        var ex = ThrowHelper.Rethrow(null, inner, xml, "Could not parse {0}: {1}", "field");

        ex.Message.Should().Be("Could not parse field: inner");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void InternalErrorException_is_catchable_as_FixAtdlException()
    {
        // InternalErrorException signals the library itself malfunctioning; it must belong to the
        // documented FixAtdlException family so a consumer catching the family does not miss it.
        Action act = () => throw new InternalErrorException("boom");

        act.Should().Throw<InternalErrorException>().Which.Should().BeAssignableTo<FixAtdlException>();
    }
}
