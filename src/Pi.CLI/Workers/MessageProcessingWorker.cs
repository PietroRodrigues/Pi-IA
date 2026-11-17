using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pi.Application.Abstractions.Messaging;
using Pi.Application.UseCases;

namespace Pi.CLI.Workers;

/// <summary>
/// Consome mensagens do barramento e imprime as respostas do assistente.
/// </summary>
public sealed class MessageProcessingWorker : BackgroundService
{
    private readonly IMessageBus _messageBus;
    private readonly ProcessMessageUseCase _processMessage;
    private readonly ILogger<MessageProcessingWorker> _logger;

    public MessageProcessingWorker(
        IMessageBus messageBus,
        ProcessMessageUseCase processMessage,
        ILogger<MessageProcessingWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(messageBus);
        ArgumentNullException.ThrowIfNull(processMessage);
        ArgumentNullException.ThrowIfNull(logger);

        _messageBus = messageBus;
        _processMessage = processMessage;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await _messageBus.ReadAsync(stoppingToken);
                var response = await _processMessage.ExecuteAsync(message, stoppingToken);

                await Console.Out.WriteLineAsync($"\nPi: {response}");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Operação cancelada durante o processamento da mensagem");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem");
            }
        }
    }
}
