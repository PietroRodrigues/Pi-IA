using Pi.Models;

namespace Pi.Abstracts.Messaging;

/// <summary>
/// Porta para o barramento de mensagens entre workers.
/// </summary>
public interface IMessageBus
{
    ValueTask PublishAsync(AssistantMessage message, CancellationToken cancellationToken = default);

    Task<AssistantMessage> ReadAsync(CancellationToken cancellationToken = default);
}
