namespace Pi.Backend.Chat;

/// <summary>
/// Saída de eventos durante uma pergunta. A CLI usa a implementação nula;
/// a ponte NDJSON usa uma implementação que escreve no stdout.
/// </summary>
public interface IAgentEventSink
{
    Task ThoughtAsync(string text, CancellationToken cancellationToken = default);

    Task ToolCallAsync(string name, string input, CancellationToken cancellationToken = default);

    Task ToolResultAsync(string name, bool ok, string text, CancellationToken cancellationToken = default);

    Task TokenAsync(string text, CancellationToken cancellationToken = default);

    Task ContextStatsAsync(int used, int limit, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementação padrão: descarta todos os eventos. Mantém o comportamento
/// da CLI textual exatamente como era antes da ponte existir.
/// </summary>
public sealed class NullAgentEventSink : IAgentEventSink
{
    public static NullAgentEventSink Instance { get; } = new();

    private NullAgentEventSink()
    {
    }

    public Task ThoughtAsync(string text, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ToolCallAsync(string name, string input, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ToolResultAsync(string name, bool ok, string text, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task TokenAsync(string text, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ContextStatsAsync(int used, int limit, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
