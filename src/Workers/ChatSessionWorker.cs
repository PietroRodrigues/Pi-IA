using Microsoft.Extensions.Hosting;
using Pi.Services;

namespace Pi.Workers;

/// <summary>
/// Adaptador entre o host genérico e a lógica da conversa.
/// </summary>
public sealed class ChatSessionWorker : BackgroundService
{
    private readonly ChatSession _session;

    public ChatSessionWorker(ChatSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        _session = session;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        _session.RunAsync(stoppingToken);
}
