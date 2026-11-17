using Pi.Services;

namespace Pi.UseCases;

/// <summary>
/// Processa uma mensagem digitada e devolve a resposta do assistente.
/// </summary>
public sealed class ProcessMessageUseCase
{
    private readonly AgentService _agent;

    public ProcessMessageUseCase(AgentService agent)
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
