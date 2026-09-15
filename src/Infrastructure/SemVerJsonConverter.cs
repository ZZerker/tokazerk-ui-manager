using System.Text.Json;
using System.Text.Json.Serialization;
using TokaZerkUIConfig.Domain;

namespace TokaZerkUIConfig.Infrastructure;

public sealed class SemVerJsonConverter : JsonConverter<SemVer>
{
    public override SemVer Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString();
        if (!SemVer.TryParse(text, out var version))
        {
            throw new JsonException($"Invalid SemVer value: '{text}'.");
        }

        return version;
    }

    public override void Write(Utf8JsonWriter writer, SemVer value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
