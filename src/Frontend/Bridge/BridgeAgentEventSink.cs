using Pi.Backend.Chat;

namespace Pi.Frontend.Bridge;

/// <summary>
/// Converte os eventos do agente em mensagens NDJSON com o requestId da pergunta.
/// </summary>
public sealed class BridgeAgentEventSink : IAgentEventSink
{
    private readonly JsonLinesWriter _writer;
    private readonly string _requestId;

    public BridgeAgentEventSink(JsonLinesWriter writer, string requestId)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestId);

        _writer = writer;
        _requestId = requestId;
    }

    public Task ThoughtAsync(string text, CancellationToken cancellationToken = default) =>
        _writer.WriteAsync(new ThoughtEvent(_requestId, text), cancellationToken);

    public Task ToolCallAsync(string name, string input, CancellationToken cancellationToken = default) =>
        _writer.WriteAsync(new ToolCallEvent(_requestId, name, input), cancellationToken);

    public Task ToolResultAsync(string name, bool ok, string text, CancellationToken cancellationToken = default) =>
        _writer.WriteAsync(new ToolResultEvent(_requestId, name, ok, text), cancellationToken);

    public Task TokenAsync(string text, CancellationToken cancellationToken = default) =>
        _writer.WriteAsync(new TokenEvent(_requestId, text), cancellationToken);

    public Task ContextStatsAsync(int used, int limit, CancellationToken cancellationToken = default)
    {
        var percent = limit <= 0 ? 0 : Math.Min(100, used * 100.0 / limit);
        return _writer.WriteAsync(new ContextStatsEvent(used, limit, percent), cancellationToken);
    }
}
