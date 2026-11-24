// A View do MVC no backend: projeta o estado do Model como linhas NDJSON.
// É a única porta de apresentação — nenhuma outra camada serializa eventos.
namespace Pi.View;

// Fachada tipada sobre o escritor de NDJSON. Os DTOs do protocolo continuam
// idênticos; aqui só mudou o lugar onde eles são construídos.
public sealed class BridgeView
{
    private readonly JsonLinesWriter _writer;

    public BridgeView(JsonLinesWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        _writer = writer;
    }

    public Task ReadyAsync(
        string model,
        string agent,
        string session,
        string cwd,
        string branch,
        CancellationToken cancellationToken = default) =>
        _writer.WriteAsync(
            new ReadyEvent(model, agent, session, cwd, branch),
            cancellationToken);

    public Task ContextStatsAsync(int used, int limit, CancellationToken cancellationToken = default)
    {
        var percent = limit <= 0 ? 0 : Math.Min(100, used * 100.0 / limit);
        return _writer.WriteAsync(new ContextStatsEvent(used, limit, percent), cancellationToken);
    }

    public Task TokenAsync(string requestId, string text, CancellationToken cancellationToken = default) =>
        _writer.WriteAsync(new TokenEvent(requestId, text), cancellationToken);

    public Task ThoughtAsync(string requestId, string text, CancellationToken cancellationToken = default) =>
        _writer.WriteAsync(new ThoughtEvent(requestId, text), cancellationToken);

    public Task ToolCallAsync(
        string requestId,
        string name,
        string input,
        CancellationToken cancellationToken = default) =>
        _writer.WriteAsync(new ToolCallEvent(requestId, name, input), cancellationToken);

    public Task ToolResultAsync(
        string requestId,
        string name,
        bool ok,
        string text,
        CancellationToken cancellationToken = default) =>
        _writer.WriteAsync(new ToolResultEvent(requestId, name, ok, text), cancellationToken);

    public Task DoneAsync(
        string requestId,
        string text,
        bool cancelled = false,
        CancellationToken cancellationToken = default) =>
        _writer.WriteAsync(new DoneEvent(requestId, text, cancelled), cancellationToken);

    public Task ErrorAsync(
        string requestId,
        string message,
        CancellationToken cancellationToken = default) =>
        _writer.WriteAsync(new ErrorEvent(requestId, message), cancellationToken);
}
