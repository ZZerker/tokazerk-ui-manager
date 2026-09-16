using System.Text.Json.Serialization;
using TokaZerkUIConfig.Domain;

namespace TokaZerkUIConfig.Infrastructure;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    Converters = [typeof(UpdateChannelJsonConverter)])]
[JsonSerializable(typeof(ToolConfig))]
internal partial class ToolConfigJsonContext : JsonSerializerContext
{
}
