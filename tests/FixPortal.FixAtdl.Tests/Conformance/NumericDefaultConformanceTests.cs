using System.Text;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Xml;

namespace FixPortal.FixAtdl.Tests.Conformance;

public class NumericDefaultConformanceTests
{
    [Theory]
    [InlineData("Amt_t")]
    [InlineData("Price_t")]
    [InlineData("PriceOffset_t")]
    [InlineData("Qty_t")]
    [InlineData("Percentage_t")]
    public void Documented_default_zero_minimum_can_be_overridden(string type)
    {
        // FIXatdl 1.1 with Errata 20101221 p.32, Parameter/@minValue default table.
        var xml = $"""
            <Strategies xmlns="http://www.fixprotocol.org/FIXatdl-1-1/Core" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" strategyIdentifierTag="5001">
              <Strategy name="Default" version="1" wireValue="D" uiRep="Default" providerID="FIXPORTAL">
                <Parameter name="DefaultMinimum" xsi:type="{type}" fixTag="9001"/>
                <Parameter name="ExplicitMinimum" xsi:type="{type}" fixTag="9002" minValue="-1"/>
              </Strategy>
            </Strategies>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var parameters = new StrategiesReader().Load(stream).Strategies[0].Parameters;

        var act = () => parameters["DefaultMinimum"].WireValue = "-0.25";

        act.Should().Throw<InvalidFieldValueException>();
        parameters["DefaultMinimum"].WireValue = "0";
        parameters["DefaultMinimum"].WireValue.Should().Be("0");
        parameters["ExplicitMinimum"].WireValue = "-0.25";
        parameters["ExplicitMinimum"].WireValue.Should().Be("-0.25");
    }
}
