using System.Text;
using System.Text.Json;
using FixPortal.FixAtdl.Contracts;
using FixPortal.FixAtdl.Xml;

const string xml = """
    <Strategies xmlns="http://www.fixprotocol.org/FIXatdl-1-1/Core" strategyIdentifierTag="5001">
      <Strategy name="PackedConsumer" version="1" wireValue="P" uiRep="Packed consumer" providerID="DEMO"/>
    </Strategies>
    """;
using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
var parsed = new StrategiesReader().Load(stream);
Require(parsed.Count == 1 && parsed.Strategies[0].Name == "PackedConsumer", "Packed parser result differs");
var dto = new AtdlDtoMapper().Map(parsed);
using var document = JsonDocument.Parse(JsonSerializer.Serialize(dto, AtdlContractJson.Options));
var strategies = document.RootElement.GetProperty("strategies");
Require(strategies.ValueKind == JsonValueKind.Array && strategies.GetArrayLength() == 1, "Expected one strategy");
var strategy = strategies[0];
Require(strategy.GetProperty("name").GetString() == "PackedConsumer", "Packed JSON name differs");
RequireEmptyArray(strategy.GetProperty("parameters"), "parameters");
RequireEmptyArray(strategy.GetProperty("panel").GetProperty("children"), "panel.children");
Console.WriteLine("Fresh Core and Contracts PackageReference consumer: parser and literal JSON verified");

static void RequireEmptyArray(JsonElement value, string name) =>
    Require(value.ValueKind == JsonValueKind.Array && value.GetArrayLength() == 0, $"Expected empty {name}");

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
