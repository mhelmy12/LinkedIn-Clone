using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatService.Infrastructure;

/// <summary>
/// Serializes Snowflake <see cref="long"/> ids as JSON strings and accepts
/// either a string or a number when reading. JavaScript clients cannot safely
/// represent a 64-bit integer, so the REST and hub contracts expose ids as
/// strings (contracts/rest-api.md, contracts/signalr-hub.md).
/// </summary>
public sealed class LongToStringJsonConverter : JsonConverter<long>
{
    public override long Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var text = reader.GetString();

            if (long.TryParse(
                    text,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var parsed))
            {
                return parsed;
            }

            throw new JsonException($"Cannot convert \"{text}\" to a 64-bit integer.");
        }

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var value))
        {
            return value;
        }

        throw new JsonException(
            $"Expected a string or number for a 64-bit integer, found {reader.TokenType}.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        long value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
    }
}
