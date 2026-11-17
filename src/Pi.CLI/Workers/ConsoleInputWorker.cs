using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pi.Application.Abstractions.Messaging;
using Pi.Application.Models;
using Pi.Application.Options;

namespace Pi.CLI.Workers;

/// <summary>
/// Lê mensagens digitadas no console e as publica no barramento.
/// </summary>
public sealed class ConsoleInputWorker : BackgroundService
{
    private readonly IMessageBus _messageBus;
    private readonly AssistantOptions _options;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<ConsoleInputWorker> _logger;

    public ConsoleInputWorker(
        IMessageBus messageBus,
        IOptions<AssistantOptions> options,
        IHostApplicationLifetime lifetime,
        ILogger<ConsoleInputWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(messageBus);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(lifetime);
        ArgumentNullException.ThrowIfNull(logger);

        _messageBus = messageBus;
        _options = options.Value;
        _lifetime = lifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Pi iniciado. Digite uma mensagem ou use Ctrl+C para sair.");

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
