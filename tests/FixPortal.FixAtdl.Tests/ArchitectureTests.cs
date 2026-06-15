using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using FixPortal.CodeStyle.ArchRules;
using FixPortal.FixAtdl.Xml;

namespace FixPortal.FixAtdl.Tests;

public class ArchitectureTests
{
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            typeof(StrategiesReader).Assembly)
        .Build();

    [Fact]
    public void Interfaces_must_have_I_prefix()
    {
        FixPortalArchRules.InterfacesMustHaveIPrefix()
            .Check(Architecture);
    }

    [Fact]
    public void Exception_types_must_inherit_from_Exception()
    {
        FixPortalArchRules.ExceptionsMustInheritFromException()
            .Check(Architecture);
    }

}
