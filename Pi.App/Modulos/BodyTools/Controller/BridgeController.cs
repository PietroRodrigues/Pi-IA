// Controller do MVC no backend: transforma linhas lidas do stdin em pedidos ao
// Model e entrega o resultado à View. Logs vão para stderr; stdout só NDJSON.
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pi.Model.Chat;
using Pi.Options;
using Pi.View;

namespace Pi.Controller;

// Controller da ponte da TUI: lê NDJSON do stdin e devolve NDJSON no stdout.
// Todo log continua indo para stderr; nenhum texto de interface é escrito aqui.
public sealed class BridgeController : BackgroundService
{
    private readonly TextReader _input;
    private readonly BridgeView _view;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Conversation _conversation;
    private readonly OllamaOptions _ollamaOptions;
    private readonly AssistantOptions _options;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<BridgeController> _logger;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _active = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Task> _running = new(StringComparer.Ordinal);

    public BridgeController(
        TextReader input,
        BridgeView view,
        IServiceScopeFactory scopeFactory,
        Conversation conversation,
        IOptions<OllamaOptions> ollamaOptions,
        IOptions<AssistantOptions> options,
        IHostApplicationLifetime lifetime,
        ILogger<BridgeController> logger)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(conversation);
        ArgumentNullException.ThrowIfNull(ollamaOptions);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(lifetime);
        ArgumentNullException.ThrowIfNull(logger);

        _input = input;
        _view = view;
        _scopeFactory = scopeFactory;
        _conversation = conversation;
        _ollamaOptions = ollamaOptions.Value;
        _options = options.Value;
        _lifetime = lifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _view.ReadyAsync(
            _ollamaOptions.Model,
            _options.AgentName,
            "Sessão Pi",
            Directory.GetCurrentDirectory(),
            ReadGitBranch(),
            stoppingToken);

        await _view.ContextStatsAsync(0, _options.ContextWindowTokens, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            string? line;

            try
            {
                line = await _input.ReadLineAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Falha ao ler o stdin da ponte");
                break;
            }

            if (line is null)
            {
                // A TUI fechou o pipe: não faz sentido manter o host vivo.
                _lifetime.StopApplication();
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                await HandleLineAsync(line, stoppingToken);
            }
            catch (JsonException ex)
            {
                await _view.ErrorAsync(string.Empty, $"JSON inválido: {ex.Message}", stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao tratar mensagem da ponte");
                await _view.ErrorAsync(string.Empty, ex.Message, stoppingToken);
            }
        }

        CancelAll();

        var tasks = _running.Values.ToArray();
        if (tasks.Length > 0)
        {
            await Task.WhenAll(tasks);
        }
    }

    private async Task HandleLineAsync(string line, CancellationToken stoppingToken)
    {
        using var document = JsonDocument.Parse(line);
        var root = document.RootElement;

        if (!root.TryGetProperty("type", out var typeProperty) ||
            typeProperty.ValueKind != JsonValueKind.String)
        {
            throw new JsonException("Campo 'type' ausente.");
        }

        switch (typeProperty.GetString())
        {
            case "prompt":
            {
                var requestId = ReadString(root, "requestId") ?? Guid.NewGuid().ToString("N");
                var text = ReadString(root, "text") ?? string.Empty;

                if (string.IsNullOrWhiteSpace(text))
                {
                    await _view.ErrorAsync(requestId, "Prompt vazio.", stoppingToken);
                    return;
                }

                StartPrompt(requestId, text, stoppingToken);
                return;
            }

            case "cancel":
            {
                var requestId = ReadString(root, "requestId");

                if (string.IsNullOrWhiteSpace(requestId))
                {
                    CancelAll();
                }
                else if (_active.TryGetValue(requestId, out var source))
                {
                    source.Cancel();
                }

                return;
            }

            case "clear":
            {
                CancelAll();
                _conversation.Clear();

                await _view.ContextStatsAsync(0, _options.ContextWindowTokens, stoppingToken);
                return;
            }

            default:
                await _view.ErrorAsync(string.Empty, "Evento desconhecido vindo da TUI.", stoppingToken);
                return;
        }
    }

    private void StartPrompt(string requestId, string text, CancellationToken stoppingToken)
    {
        var requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

        if (!_active.TryAdd(requestId, requestCancellation))
        {
            requestCancellation.Dispose();
            return;
        }

        var task = Task.Run(async () =>
        {
            var reply = string.Empty;
            var cancelled = false;
            var failed = false;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var agent = scope.ServiceProvider.GetRequiredService<AgentService>();
                var sink = new BridgeEventSink(_view, requestId);

                reply = await agent.AskStreamAsync(text, sink, requestCancellation.Token);
            }
            catch (OperationCanceledException) when (requestCancellation.IsCancellationRequested)
            {
                cancelled = true;
            }
            catch (Exception ex)
            {
                failed = true;
                _logger.LogError(ex, "Erro ao responder o prompt {RequestId}", requestId);
                await _view.ErrorAsync(requestId, ex.Message);
            }
            finally
            {
                if (!failed)
                {
                    await _view.DoneAsync(requestId, reply, cancelled);
                }

                _active.TryRemove(requestId, out _);
                _running.TryRemove(requestId, out _);
                requestCancellation.Dispose();
            }
        }, CancellationToken.None); // o pedido termina e avisa a TUI antes de sair

        _running[requestId] = task;
    }

    private void CancelAll()
    {
        foreach (var source in _active.Values)
        {
            source.Cancel();
        }
    }

    private static string? ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    // Melhor esforço: se o git falhar ou demorar, a ponte segue com "—".
    private static string ReadGitBranch()
    {
        try
        {
            var startInfo = new ProcessStartInfo("git", "rev-parse --abbrev-ref HEAD")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return "—";
            }

            var output = process.StandardOutput.ReadToEnd().Trim();

            if (!process.WaitForExit(1000))
            {
                process.Kill(entireProcessTree: true);
                return "—";
            }

            return output.Length == 0 ? "—" : output;
        }
        catch
        {
            return "—";
        }
    }
}
