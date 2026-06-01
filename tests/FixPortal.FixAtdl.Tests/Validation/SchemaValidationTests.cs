using System.Text;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Xml;

namespace FixPortal.FixAtdl.Tests.Validation;

/// <summary>
/// Characterisation tests verifying that canonical fixtures are accepted and
/// invalid ones are rejected by the StrategiesReader deserializer.
/// Note: StrategiesReader uses a code-driven schema (SchemaDefinitions), not XSD
/// validation. "Schema-valid" here means "accepted by the deserializer without
/// throwing"; "schema-invalid" means "rejected with an exception".
/// </summary>
public class SchemaValidationTests
{
    private static Strategies_t Load(string xml)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return new StrategiesReader().Load(stream);
    }

    [Theory]
    [InlineData("Fixtures/twap.xml")]
    [InlineData("Fixtures/vwap.xml")]
    [InlineData("Fixtures/pov.xml")]
    public async Task Canonical_fixture_deserializes_without_throwing(string path)
    {
        var xml = await FixtureFiles.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
        var act = () => Load(xml);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task Schema_invalid_fixture_throws_on_load()
    {
        var xml = await FixtureFiles.ReadAllTextAsync("Fixtures/invalid-schema.xml", TestContext.Current.CancellationToken);
        var act = () => Load(xml);
        act.Should().Throw<FixAtdlException>();
    }

    [Fact]
    public async Task Missing_constructor_fed_parameter_name_throws_MissingMandatoryValueException()
    {
        var xml = await FixtureFiles.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);
        xml = xml.Replace("<Parameter name=\"StartTime\"", "<Parameter", StringComparison.Ordinal);

        var act = () => Load(xml);

        act.Should().Throw<MissingMandatoryValueException>();
    }
}
