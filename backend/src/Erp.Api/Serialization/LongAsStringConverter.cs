using System.Text.Json;
using System.Text.Json.Serialization;

namespace Erp.Api.Serialization;

/// <summary>
/// Serializes <see cref="long"/> ids as JSON strings (and reads them back from
/// either a string or a number). BigInt values exceed JavaScript's safe integer
/// range (2^53), so emitting them as strings avoids precision loss on the SPA
/// while keeping the frontend's <c>id: string</c> typing intact.
/// </summary>
public sealed class LongAsStringConverter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.String => long.Parse(reader.GetString()!),
            JsonTokenType.Number => reader.GetInt64(),
            _ => throw new JsonException($"Cannot convert {reader.TokenType} to long."),
        };

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}

/// <summary>Nullable counterpart of <see cref="LongAsStringConverter"/>.</summary>
public sealed class NullableLongAsStringConverter : JsonConverter<long?>
{
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String => string.IsNullOrEmpty(reader.GetString()) ? null : long.Parse(reader.GetString()!),
            JsonTokenType.Number => reader.GetInt64(),
            _ => throw new JsonException($"Cannot convert {reader.TokenType} to long?."),
        };

    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        if (value is { } v) writer.WriteStringValue(v.ToString());
        else writer.WriteNullValue();
    }
}
