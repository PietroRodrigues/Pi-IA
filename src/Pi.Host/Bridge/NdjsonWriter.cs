// Único produtor de stdout: serializa envelopes e faz flush imediato sob semáforo.
using System.Text.Json;
using Pi.Contracts.Protocol;
using Pi.Core.Abstractions;

namespace Pi.Host.Bridge;

public sealed class NdjsonWriter(TextWriter writer)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task WriteAsync<T>(
        string type,
        T payload,
        string correlationId = "",
        CancellationToken cancellationToken = default)
    {
        var envelope = ProtocolEnvelope.Create(type, payload, correlationId);
        await WriteEnvelopeAsync(envelope, cancellationToken);
    }

    public async Task WriteEnvelopeAsync(
        ProtocolEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(envelope, Options);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await writer.WriteLineAsync(json);
            await writer.FlushAsync();
        }
        finally
        {
            _gate.Release();
        }
    }
}

// Adaptador do canal do domínio para o escritor NDJSON.
public sealed class NdjsonEventSink(NdjsonWriter writer) : IEventSink
{
    public Task PublishAsync<T>(
        string type,
        T payload,
        string correlationId = "",
        CancellationToken cancellationToken = default) =>
        writer.WriteAsync(type, payload, correlationId, cancellationToken);
}
