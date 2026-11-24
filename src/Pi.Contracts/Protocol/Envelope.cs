// Envelope único do protocolo v1. Todo comando e evento viaja com v, id,
// correlationId, timestamp, type e um payload tipado.
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pi.Contracts.Protocol;

public static class ProtocolVersion
{
    public const int Current = 1;
}

public sealed record ProtocolEnvelope(
    [property: JsonPropertyName("v")] int Version,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("correlationId")] string CorrelationId,
    [property: JsonPropertyName("timestamp")] string Timestamp,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("payload")] JsonElement Payload)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static ProtocolEnvelope Create<T>(
        string type,
        T payload,
        string correlationId = "",
        string? id = null) =>
        new(
            ProtocolVersion.Current,
            string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id,
            correlationId,
            DateTimeOffset.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture),
            type,
            JsonSerializer.SerializeToElement(payload, Options));

    public static ProtocolEnvelope Parse(string line)
    {
        var envelope = JsonSerializer.Deserialize<ProtocolEnvelope>(line, Options)
            ?? throw new JsonException("Mensagem NDJSON vazia.");

        if (envelope.Version != ProtocolVersion.Current)
        {
            throw new JsonException($"Versão do protocolo não suportada: {envelope.Version}.");
        }

        if (string.IsNullOrWhiteSpace(envelope.Type))
        {
            throw new JsonException("Campo 'type' ausente.");
        }

        return envelope;
    }

    public T Read<T>() =>
        Payload.Deserialize<T>(Options) ?? throw new JsonException($"Payload inválido para '{Type}'.");

    public string ToJson() => JsonSerializer.Serialize(this, Options);
}
