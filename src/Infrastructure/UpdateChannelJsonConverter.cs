using System.Text.Json;
using System.Text.Json.Serialization;
using TokaZerkUIConfig.Domain;

namespace TokaZerkUIConfig.Infrastructure;

public sealed class UpdateChannelJsonConverter : JsonConverter<UpdateChannel>
{
    public override bool HandleNull => true;

    public override UpdateChannel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (string.Equals(value, nameof(UpdateChannel.Beta), StringComparison.OrdinalIgnoreCase))
            {
                return UpdateChannel.Beta;
            }

            return UpdateChannel.Stable;
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.TryGetInt32(out var numericValue)
                && Enum.IsDefined((UpdateChannel)numericValue)
                ? (UpdateChannel)numericValue
                : UpdateChannel.Stable;
        }

        if (reader.TokenType == JsonTokenType.Null)
        {
            return UpdateChannel.Stable;
        }

        throw new JsonException($"Unexpected token type for update channel: {reader.TokenType}.");
    }

    public override void Write(Utf8JsonWriter writer, UpdateChannel value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(Enum.IsDefined(value) ? value.ToString() : UpdateChannel.Stable.ToString());
    }
}
