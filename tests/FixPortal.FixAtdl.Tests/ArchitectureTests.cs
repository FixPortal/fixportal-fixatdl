using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using FixPortal.FixAtdl.Xml;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace FixPortal.FixAtdl.Tests;

public class ArchitectureTests
{
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(typeof(StrategiesReader).Assembly)
        .Build();

    [Fact]
    public void Interfaces_must_have_I_prefix()
    {
        Interfaces().Should().HaveNameStartingWith("I").Check(Architecture);
    }

    [Fact]
    public void Exception_types_must_inherit_from_Exception()
    {
        Classes().That().HaveNameEndingWith("Exception").Should().BeAssignableTo(typeof(Exception)).Check(Architecture);
    }
}
