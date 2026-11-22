namespace Pi.Backend.Chat;

/// <summary>
/// Processa uma mensagem digitada e devolve a resposta do assistente.
/// </summary>
public sealed class ProcessMessage
{
    private readonly AgentService _agent;

    public ProcessMessage(AgentService agent)
    {
        ArgumentNullException.ThrowIfNull(agent);
        _agent = agent;
    }

    public Task<string> ExecuteAsync(string message, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return _agent.AskAsync(message, cancellationToken);
    }
}
