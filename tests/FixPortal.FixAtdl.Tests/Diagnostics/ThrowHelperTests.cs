using FixPortal.FixAtdl.Diagnostics;

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
        // The (string, string) ctor of ArgumentException-family types is (paramName, message);
        // ThrowHelper must be able to surface the real parameter name, not only the hard-coded "Value".
        ArgumentOutOfRangeException ex = ThrowHelper.NewWithParamName<ArgumentOutOfRangeException>(
            source: null, paramName: "tenorOffset", message: "out of range");

        ex.ParamName.Should().Be("tenorOffset");
    }

    [Fact]
    public void New_without_param_name_defaults_to_Value_for_argument_exceptions()
    {
        // Back-compat: the plain New<T> path keeps the historical synthetic "Value" name.
        ArgumentOutOfRangeException ex = ThrowHelper.New<ArgumentOutOfRangeException>(null, "out of range");

        ex.ParamName.Should().Be("Value");
    }
}
