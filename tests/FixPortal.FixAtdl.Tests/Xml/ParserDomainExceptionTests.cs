using System.Text;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Xml;

namespace FixPortal.FixAtdl.Tests.Xml;

/// <summary>
/// Parser-level exception surface: domain failures inside reflected property setters must reach the
/// host as the documented exception family rather than a raw TargetInvocationException (R20), and an
/// EditRef under a strategy- or strategies-level Edit must fail at load instead of being silently
/// discarded (R19).
/// </summary>
public class ParserDomainExceptionTests
{
    private static MemoryStream StreamOf(string xml) => new(Encoding.UTF8.GetBytes(xml));

    [Fact]
    public void Out_of_range_precision_surfaces_as_a_domain_exception_not_TargetInvocationException()
    {
        // R20: the Float_t.Precision setter rejects 30 (> 28); the parser must unwrap the reflection
        // wrapper so the host sees the documented exception family with element context.
        const string xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <Strategies xmlns="http://www.fixprotocol.org/FIXatdl-1-1/Core"
                        xmlns:lay="http://www.fixprotocol.org/FIXatdl-1-1/Layout"
                        xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                        strategyIdentifierTag="5001">
              <Strategy name="Test" version="1" wireValue="Test" uiRep="Test" providerID="DEMO">
                <Parameter name="Price" xsi:type="Float_t" fixTag="44" use="optional" precision="30"/>
                <lay:StrategyLayout>
                  <lay:StrategyPanel title="Panel" orientation="VERTICAL" collapsible="false" border="Line" />
                </lay:StrategyLayout>
              </Strategy>
            </Strategies>
            """;
        using var stream = StreamOf(xml);

        Action act = () => new StrategiesReader().Load(stream);

        act.Should().Throw<InvalidFieldValueException>().WithMessage("*Precision*30*");
    }

    [Fact]
    public void EditRef_under_a_strategies_level_Edit_fails_at_load()
    {
        // R19: the global Edit definition carries no EditRefs, so this document would otherwise load
        // with the referenced operand silently discarded.
        const string xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <Strategies xmlns="http://www.fixprotocol.org/FIXatdl-1-1/Core"
                        xmlns:lay="http://www.fixprotocol.org/FIXatdl-1-1/Layout"
                        xmlns:val="http://www.fixprotocol.org/FIXatdl-1-1/Validation"
                        xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                        strategyIdentifierTag="5001">
              <Strategy name="Test" version="1" wireValue="Test" uiRep="Test" providerID="DEMO">
                <Parameter name="Qty" xsi:type="Int_t" fixTag="38" use="required"/>
                <lay:StrategyLayout>
                  <lay:StrategyPanel title="Panel" orientation="VERTICAL" collapsible="false" border="Line" />
                </lay:StrategyLayout>
              </Strategy>
              <val:Edit id="globalEdit" logicOperator="AND">
                <val:Edit field="Qty" operator="GT" value="0"/>
                <val:EditRef id="otherEdit"/>
              </val:Edit>
            </Strategies>
            """;
        using var stream = StreamOf(xml);

        Action act = () => new StrategiesReader().Load(stream);

        act.Should().Throw<InconsistentStrategyException>().WithMessage("*EditRef*");
    }
}
