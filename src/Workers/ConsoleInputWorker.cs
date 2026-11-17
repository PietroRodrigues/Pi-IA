using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pi.Abstracts.Messaging;
using Pi.Models;
using Pi.Options;

namespace Pi.Workers;

/// <summary>
/// Lê mensagens digitadas no console e as publica no barramento.
/// </summary>
public sealed class ConsoleInputWorker : BackgroundService
{
    private readonly IMessageBus _messageBus;
    private readonly AssistantOptions _options;
    private readonly ChatPresenter _presenter;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<ConsoleInputWorker> _logger;

    public ConsoleInputWorker(
        IMessageBus messageBus,
        IOptions<AssistantOptions> options,
        ChatPresenter presenter,
        IHostApplicationLifetime lifetime,
        ILogger<ConsoleInputWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(messageBus);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(presenter);
        ArgumentNullException.ThrowIfNull(lifetime);
        ArgumentNullException.ThrowIfNull(logger);

        _messageBus = messageBus;
        _options = options.Value;
        _presenter = presenter;
        _lifetime = lifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Console.Out.WriteLineAsync(_presenter.Welcome());
        _logger.LogDebug("Chat iniciado. Aguardando mensagens.");

        while (!stoppingToken.IsCancellationRequested)
        {
            string? input;

            try
            {
                input = await Console.In.ReadLineAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Falha ao ler a entrada do console");
                break;
            }

            if (input is null)
            {
                _lifetime.StopApplication();
                break;
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            if (_options.ExitKeywords.Any(keyword =>
                    input.Equals(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                _lifetime.StopApplication();
                break;
            }

            await _messageBus.PublishAsync(new AssistantMessage(input), stoppingToken);
        }
    }
}
