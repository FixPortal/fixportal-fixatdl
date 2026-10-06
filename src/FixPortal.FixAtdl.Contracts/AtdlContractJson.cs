using System.Text.Json;
using System.Text.Json.Serialization;

namespace FixPortal.FixAtdl.Contracts;

/// <summary>
/// Serializer settings for the contract JSON consumed by @fix-portal/fixatdl-react.
/// </summary>
public static class AtdlContractJson
{
    /// <summary>
    /// camelCase property names, null members omitted, enums as strings. The React package's
    /// optional fields rely on null members being absent rather than <c>null</c>. The instance
    /// is read-only; copy it with <c>new JsonSerializerOptions(AtdlContractJson.Options)</c> to customise.
    /// </summary>
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
