using AwesomeAssertions;
using Xunit;

namespace FixPortal.FixAtdl.Contracts.Tests;

public class ScaffoldTests
{
    [Fact]
    public void Corpus_is_copied_to_output() =>
        File.Exists(Path.Join(AppContext.BaseDirectory, "contracts", "state-rule-cases.json")).Should().BeTrue();
}
