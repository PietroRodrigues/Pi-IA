using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pi.Abstracts.Messaging;
using Pi.UseCases;

namespace Pi.Workers;

/// <summary>
/// Consome mensagens do barramento e imprime as respostas do assistente.
/// </summary>
public sealed class MessageProcessingWorker : BackgroundService
{
    private readonly IMessageBus _messageBus;
    private readonly ProcessMessageUseCase _processMessage;
    private readonly ChatPresenter _presenter;
    private readonly ILogger<MessageProcessingWorker> _logger;

    public MessageProcessingWorker(
        IMessageBus messageBus,
        ProcessMessageUseCase processMessage,
        ChatPresenter presenter,
        ILogger<MessageProcessingWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(messageBus);
        ArgumentNullException.ThrowIfNull(processMessage);
        ArgumentNullException.ThrowIfNull(presenter);
        ArgumentNullException.ThrowIfNull(logger);

        _messageBus = messageBus;
        _processMessage = processMessage;
        _presenter = presenter;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await _messageBus.ReadAsync(stoppingToken);

                string response;
                await using (new ThinkingIndicator(_presenter.Thinking(), Console.Out))
                {
                    response = await _processMessage.ExecuteAsync(message, stoppingToken);
                }

                await Console.Out.WriteLineAsync(_presenter.Reply(response));
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
