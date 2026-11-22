using Pi.Backend.Chat;

namespace Pi.Frontend.Bridge;

/// <summary>
/// View inerte para o modo bridge: a transcrição é desenhada pela TUI a partir
/// dos eventos NDJSON. Registrada para que a validação de serviços do ambiente
/// Development encontre um <see cref="IChatView"/> sem iniciar a interface textual.
/// </summary>
public sealed class NullChatView : IChatView
{
    public void ShowWelcome(string model, string agent)
    {
    }

    public Task<string?> ReadInputAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);

    public IAsyncDisposable BeginThinking(string agent, string model) => NoopDisposable.Instance;

    public void ShowReply(string response, string agent)
    {
    }

    public void ShowError(string message)
    {
    }

    private sealed class NoopDisposable : IAsyncDisposable
    {
        public static readonly NoopDisposable Instance = new();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
