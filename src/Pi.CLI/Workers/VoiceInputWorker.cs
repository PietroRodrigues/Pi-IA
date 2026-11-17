using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pi.Application.Abstractions.Messaging;
using Pi.Application.UseCases;

namespace Pi.CLI.Workers;

/// <summary>
/// Captura áudio continuamente e publica as transcrições válidas.
/// </summary>
public sealed class VoiceInputWorker : BackgroundService
{
    private readonly CaptureVoiceUseCase _captureVoice;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<VoiceInputWorker> _logger;

    public VoiceInputWorker(
        CaptureVoiceUseCase captureVoice,
        IMessageBus messageBus,
        ILogger<VoiceInputWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(captureVoice);
        ArgumentNullException.ThrowIfNull(messageBus);
        ArgumentNullException.ThrowIfNull(logger);

        _captureVoice = captureVoice;
        _messageBus = messageBus;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await _captureVoice.ExecuteAsync(stoppingToken);

                if (message is not null)
                {
                    await _messageBus.PublishAsync(message, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar entrada de voz");
            }

            await Task.Delay(300, stoppingToken);
        }
    }
}
