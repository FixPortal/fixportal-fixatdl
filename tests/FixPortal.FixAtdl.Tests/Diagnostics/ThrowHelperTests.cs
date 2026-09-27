using System.Xml;
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
    public void New_falls_back_to_string_constructor_for_argument_exception_subclasses()
    {
        var exception = ThrowHelper.New<StringOnlyArgumentException>(null, "invalid");

        exception.Message.Should().Be("invalid");
    }

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
    public void Rethrow_single_argument_overload_returns_raw_template_for_brace_bearing_format()
    {
        // A template carrying a literal "{NULL}" alongside "{0}" makes string.Format throw
        // FormatException; the error-reporting path must surface the raw template instead (F1/K).
        var inner = new InvalidOperationException("inner");

        var ex = ThrowHelper.Rethrow(null, inner, "Could not parse {NULL} field {0}", "tenor");

        ex.Message.Should().Be("Could not parse {NULL} field {0}");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void Rethrow_params_overload_returns_raw_template_for_brace_bearing_format()
    {
        // The same brace-bearing template through the params overload must likewise return the raw
        // template rather than throwing FormatException (F1/K).
        var inner = new InvalidOperationException("inner");

        var ex = ThrowHelper.Rethrow(null, inner, "Could not parse {NULL} field {0}", "tenor", "unused");

        ex.Message.Should().Be("Could not parse {NULL} field {0}");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void Rethrow_without_message_inner_constructor_returns_same_instance_with_source_and_line_info()
    {
        // StringOnlyArgumentException has only a (string) constructor, so BuildRethrown cannot
        // rewrap it: the original instance is returned carrying what context can be attached (F4/C).
        var inner = new StringOnlyArgumentException("inner");
        var xml = XElement.Parse("<Root/>", LoadOptions.SetLineInfo);
        var lineInfo = (IXmlLineInfo)xml;

        var ex = ThrowHelper.Rethrow("Parser", inner, xml, "ctx {0}", "x");

        ex.Should().BeSameAs(inner);
        ex.Source.Should().Be("Parser");
        ex.Data["LineNumber"].Should().Be(lineInfo.LineNumber);
        ex.Data["LinePosition"].Should().Be(lineInfo.LinePosition);
    }

    [Fact]
    public void Rethrow_without_message_inner_constructor_preserves_existing_source()
    {
        var inner = new StringOnlyArgumentException("inner") { Source = "Original" };

        var ex = ThrowHelper.Rethrow("Parser", inner, (XObject?)null, "ctx {0}", "x");

        ex.Should().BeSameAs(inner);
        ex.Source.Should().Be("Original");
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

internal sealed class StringOnlyArgumentException(string message) : ArgumentException(message) { }
