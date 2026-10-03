using System.Text;
using FixPortal.FixAtdl.Tests.TestDoubles;
using FixPortal.FixAtdl.Xml;
using FixPortal.FixAtdl.Xml.Serialization;

namespace FixPortal.FixAtdl.Tests.Diagnostics;

/// <summary>
/// Proves that a host-supplied ILoggerFactory is threaded through the deserialization pipeline:
/// both StrategiesReader (load-level) and ElementFactory (per-object construction) must emit records.
/// </summary>
public class LoggingWiringTests
{
    [Fact]
    public async Task Load_with_supplied_factory_records_from_reader_and_factory()
    {
        var recorder = new RecordingLoggerFactory();
        var xml = await FixtureFiles.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        new StrategiesReader(recorder).Load(stream);

        var categories = recorder.Records.Select(r => r.Category).ToList();
        categories.Should().Contain(typeof(StrategiesReader).FullName);
        categories.Should().Contain(typeof(ElementFactory).FullName);
    }

    [Fact]
    public async Task Load_without_factory_parses_successfully()
    {
        // The ILoggerFactory ctor parameter is optional: supplying nothing must keep the original
        // behaviour — a successful parse using the silent NullLoggerFactory default. Guards that the
        // additive ctor change did not break the default (no-factory) path.
        var xml = await FixtureFiles.ReadAllTextAsync("Fixtures/twap.xml", TestContext.Current.CancellationToken);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var strategies = new StrategiesReader().Load(stream);

        strategies.Count.Should().Be(1);
    }
}
