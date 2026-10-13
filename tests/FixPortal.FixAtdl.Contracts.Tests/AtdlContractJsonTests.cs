using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using Xunit;

namespace FixPortal.FixAtdl.Contracts.Tests;

public class AtdlContractJsonTests
{
    [Fact]
    public void Options_are_read_only()
    {
        // Serialising once freezes any JsonSerializerOptions, so a setter-throws check on the shared
        // static passes whenever another test has already serialised. Build a fresh instance
        // through the factory instead, so only an explicit MakeReadOnly can make it read-only.
        var create = typeof(AtdlContractJson).GetMethod(
            "Create",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
        );
        create.Should().NotBeNull();

        var fresh = (JsonSerializerOptions)create.Invoke(null, null)!;

        fresh.IsReadOnly.Should().BeTrue();
        FluentActions
            .Invoking(() => fresh.DefaultIgnoreCondition = JsonIgnoreCondition.Never)
            .Should()
            .Throw<InvalidOperationException>();
    }

    [Fact]
    public void Null_members_are_omitted_and_names_are_camel_case()
    {
        var node = new StateRuleAstNodeDto(StateRuleAstKind.Not, null, null, null, [], null, null);

        var json = JsonSerializer.Serialize(node, AtdlContractJson.Options);

        json.Should().Be("{\"kind\":\"not\",\"children\":[]}");
    }
}
