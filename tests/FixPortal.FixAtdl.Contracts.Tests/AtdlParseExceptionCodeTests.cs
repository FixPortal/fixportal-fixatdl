using AwesomeAssertions;
using Xunit;

namespace FixPortal.FixAtdl.Contracts.Tests;

/// <summary>
/// The code strings are a machine-readable contract with hosts, so their values are pinned
/// literally. Tests elsewhere compare against the constants, which cannot see a changed value.
/// </summary>
public class AtdlParseExceptionCodeTests
{
    [Theory]
    [InlineData(AtdlParseExceptionCode.MalformedXml, "ATDL_MALFORMED_XML")]
    [InlineData(AtdlParseExceptionCode.NotStrategiesRoot, "ATDL_NOT_STRATEGIES_ROOT")]
    [InlineData(AtdlParseExceptionCode.ParseFailed, "ATDL_PARSE_FAILED")]
    [InlineData(AtdlParseExceptionCode.UnresolvedEditRef, "ATDL_UNRESOLVED_EDIT_REF")]
    [InlineData(AtdlParseExceptionCode.UnknownStateRuleOperator, "ATDL_UNKNOWN_STATE_RULE_OPERATOR")]
    [InlineData(AtdlParseExceptionCode.InvalidEditValue, "ATDL_INVALID_EDIT_VALUE")]
    [InlineData(AtdlParseExceptionCode.UnknownStateRuleKind, "ATDL_UNKNOWN_STATE_RULE_KIND")]
    [InlineData(AtdlParseExceptionCode.MaxDepthExceeded, "ATDL_MAX_DEPTH_EXCEEDED")]
    public void Code_values_are_the_published_strings(string actual, string published) => actual.Should().Be(published);
}
