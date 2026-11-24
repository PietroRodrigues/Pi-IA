// O caminho por onde o agente avisa o que acontece. A ponte escreve no stdout;
// quem não mostra progresso usa a versão nula.
namespace Pi.Model.Chat;

// Saída de eventos durante uma pergunta. A ponte escreve no stdout;
// quem não mostra progresso usa a versão nula.
public interface IAgentEventSink
{
    Task ThoughtAsync(string text, CancellationToken cancellationToken = default);

    Task ToolCallAsync(string name, string input, CancellationToken cancellationToken = default);

    Task ToolResultAsync(string name, bool ok, string text, CancellationToken cancellationToken = default);

    Task TokenAsync(string text, CancellationToken cancellationToken = default);

    Task ContextStatsAsync(int used, int limit, CancellationToken cancellationToken = default);
}

// Descarta os eventos. Serve para testes e para quem só quer o texto final.
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
