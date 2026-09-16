using System.Text.Json.Serialization;
using TokaZerkUIConfig.Domain;

namespace TokaZerkUIConfig.Infrastructure;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    Converters = [typeof(SemVerJsonConverter), typeof(JsonStringEnumConverter<UpdateChannel>)])]
[JsonSerializable(typeof(UiSettings))]
internal partial class SettingsJsonContext : JsonSerializerContext
{
}
