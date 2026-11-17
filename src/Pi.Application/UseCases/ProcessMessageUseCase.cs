using Pi.Application.Models;
using Pi.Application.Services;

namespace Pi.Application.UseCases;

/// <summary>
/// Processa uma mensagem recebida do barramento e devolve a resposta do assistente.
/// </summary>
public sealed class ProcessMessageUseCase
{
    private readonly AgentService _agent;

    public ProcessMessageUseCase(AgentService agent)
    {
        ArgumentNullException.ThrowIfNull(agent);
        _agent = agent;
    }

    public Task<string> ExecuteAsync(AssistantMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return _agent.AskAsync(message.Text, cancellationToken);
    }
}
