// Liga os eventos do agente à View da ponte e carrega o requestId em cada um.
using Pi.Model.Chat;

namespace Pi.View;

// Converte os eventos do Model em mensagens NDJSON com o requestId da pergunta.
public sealed class BridgeEventSink : IAgentEventSink
{
    private readonly BridgeView _view;
    private readonly string _requestId;

    public BridgeEventSink(BridgeView view, string requestId)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestId);

        _view = view;
        _requestId = requestId;
    }

    public Task ThoughtAsync(string text, CancellationToken cancellationToken = default) =>
        _view.ThoughtAsync(_requestId, text, cancellationToken);

    public Task ToolCallAsync(string name, string input, CancellationToken cancellationToken = default) =>
        _view.ToolCallAsync(_requestId, name, input, cancellationToken);

    public Task ToolResultAsync(string name, bool ok, string text, CancellationToken cancellationToken = default) =>
        _view.ToolResultAsync(_requestId, name, ok, text, cancellationToken);

    public Task TokenAsync(string text, CancellationToken cancellationToken = default) =>
        _view.TokenAsync(_requestId, text, cancellationToken);

    public Task ContextStatsAsync(int used, int limit, CancellationToken cancellationToken = default) =>
        _view.ContextStatsAsync(used, limit, cancellationToken);
}
