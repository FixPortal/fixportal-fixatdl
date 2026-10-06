using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using Xunit;

namespace FixPortal.FixAtdl.Contracts.Tests;

public class AtdlContractJsonTests
{
    [Fact]
    public void Options_are_read_only() =>
        FluentActions
            .Invoking(() => AtdlContractJson.Options.DefaultIgnoreCondition = JsonIgnoreCondition.Never)
            .Should()
            .Throw<InvalidOperationException>();

    [Fact]
    public void Null_members_are_omitted_and_names_are_camel_case()
    {
        var node = new StateRuleAstNodeDto(StateRuleAstKind.Not, null, null, null, [], null, null);

        var json = JsonSerializer.Serialize(node, AtdlContractJson.Options);

        json.Should().Be("{\"kind\":\"not\",\"children\":[]}");
    }
}
