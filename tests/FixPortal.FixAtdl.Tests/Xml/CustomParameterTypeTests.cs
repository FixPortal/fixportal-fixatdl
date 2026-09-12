using System.Text;
using FixPortal.FixAtdl.Diagnostics.Exceptions;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Model.Types;
using FixPortal.FixAtdl.Model.Types.Support;
using FixPortal.FixAtdl.Xml;
using FixPortal.FixAtdl.Xml.Serialization;

namespace FixPortal.FixAtdl.Tests.Xml;

/// <summary>
/// A minimal vendor-extension parameter type, standing in for a real host's custom implementation.
/// Extends the standard <see cref="Int_t"/> rather than reimplementing every <see cref="IParameterType"/>
/// member from scratch - the registration mechanism does not care how the type is built, only that it
/// satisfies <c>Parameter_t&lt;T&gt;</c>'s existing <c>where T : IParameterType, new()</c> constraint.
/// </summary>
public class TestVendorAmountType : Int_t
{
    /// <summary>A vendor-specific attribute with no standard-type equivalent.</summary>
    public string? VendorTag { get; set; }
}

/// <summary>Implements <see cref="IParameterType"/> but deliberately has no parameterless constructor,
/// for <see cref="CustomParameterTypeTests.ClrType_with_no_public_parameterless_constructor_is_rejected_eagerly"/>.</summary>
public class NoParameterlessCtorType : Int_t
{
    public NoParameterlessCtorType(int requiredArg) => _ = requiredArg;
}

/// <summary>
/// Proves a host can register its own CLR type for a custom (vendor-extension) <c>Parameter</c>
/// <c>xsi:type</c> - the FIXatdl 1.1-permitted construct this library cannot itself define, since a
/// vendor extension's semantics are vendor-specific by definition.
/// </summary>
public class CustomParameterTypeTests
{
    private static readonly ElementAttribute[] VendorAmountAttributes =
    [
        new("minValue", "Value.MinValue", typeof(int), Required.Optional),
        new("maxValue", "Value.MaxValue", typeof(int), Required.Optional),
        new("vendorTag", "Value.VendorTag", typeof(string), Required.Optional),
    ];

    private static string StrategyXml(string xsiType, string extraAttribute = "") =>
        $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <Strategies xmlns="http://www.fixprotocol.org/FIXatdl-1-1/Core"
                        xmlns:lay="http://www.fixprotocol.org/FIXatdl-1-1/Layout"
                        xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                        strategyIdentifierTag="5001">
              <Strategy name="Test" version="1" wireValue="Test" uiRep="Test" providerID="DEMO">
                <Parameter name="X" xsi:type="{xsiType}" fixTag="9999" use="required" {extraAttribute}/>
                <lay:StrategyLayout>
                  <lay:StrategyPanel title="Panel" orientation="VERTICAL" collapsible="false" border="Line" />
                </lay:StrategyLayout>
              </Strategy>
            </Strategies>
            """;

    private static MemoryStream StreamOf(string xml) => new(Encoding.UTF8.GetBytes(xml));

    [Fact]
    public void Registered_custom_type_parses_and_sets_its_vendor_attribute()
    {
        using var stream = StreamOf(StrategyXml("TestVendorAmountType", "vendorTag=\"ABC\""));

        var reader = new StrategiesReader(
            customParameterTypes:
            [
                new CustomParameterType("TestVendorAmountType", typeof(TestVendorAmountType), VendorAmountAttributes),
            ]
        );

        Strategies_t strategies = reader.Load(stream);

        var parameter = (Parameter_t<TestVendorAmountType>)strategies["Test"].Parameters["X"];
        parameter.Value.VendorTag.Should().Be("ABC");
    }

    [Fact]
    public void Unregistered_custom_type_still_throws()
    {
        using var stream = StreamOf(StrategyXml("TestVendorAmountType"));

        var act = () => new StrategiesReader().Load(stream);

        act.Should().Throw<InvalidFieldValueException>();
    }

    [Fact]
    public void Registration_does_not_leak_across_reader_instances()
    {
        using var stream = StreamOf(StrategyXml("TestVendorAmountType"));

        // One reader registers the custom type; a second, unrelated reader does not. The second must
        // behave exactly as if no registration had ever happened anywhere in the process.
        _ = new StrategiesReader(
            customParameterTypes:
            [
                new CustomParameterType("TestVendorAmountType", typeof(TestVendorAmountType), VendorAmountAttributes),
            ]
        );

        var act = () => new StrategiesReader().Load(stream);

        act.Should().Throw<InvalidFieldValueException>();
    }

    [Fact]
    public void Duplicate_XsiTypeName_registrations_throw_a_descriptive_exception()
    {
        var act = () =>
            new StrategiesReader(
                customParameterTypes:
                [
                    new CustomParameterType("Dup_t", typeof(TestVendorAmountType), VendorAmountAttributes),
                    new CustomParameterType("Dup_t", typeof(TestVendorAmountType), VendorAmountAttributes),
                ]
            );

        act.Should().Throw<ArgumentException>().WithMessage("*Dup_t*");
    }

    [Fact]
    public void ClrType_not_implementing_IParameterType_is_rejected_eagerly()
    {
        var act = () => new CustomParameterType("Bad_t", typeof(string), VendorAmountAttributes);

        act.Should().Throw<ArgumentException>().WithMessage("*IParameterType*");
    }

    [Fact]
    public void ClrType_with_no_public_parameterless_constructor_is_rejected_eagerly()
    {
        var act = () => new CustomParameterType("Bad_t", typeof(NoParameterlessCtorType), VendorAmountAttributes);

        act.Should().Throw<ArgumentException>().WithMessage("*parameterless constructor*");
    }

    [Fact]
    public void Standard_types_are_unaffected_by_an_unrelated_custom_registration()
    {
        using var stream = StreamOf(StrategyXml("Int_t"));

        var reader = new StrategiesReader(
            customParameterTypes:
            [
                new CustomParameterType("TestVendorAmountType", typeof(TestVendorAmountType), VendorAmountAttributes),
            ]
        );

        var act = () => reader.Load(stream);

        act.Should().NotThrow();
    }
}
