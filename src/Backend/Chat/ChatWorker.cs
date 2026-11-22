using Microsoft.Extensions.Hosting;

namespace Pi.Backend.Chat;

/// <summary>
/// Adaptador entre o host genérico e a lógica da conversa.
/// </summary>
public sealed class ChatWorker : BackgroundService
{
    private readonly ChatSession _session;

    public ChatWorker(ChatSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        _session = session;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        _session.RunAsync(stoppingToken);
}
