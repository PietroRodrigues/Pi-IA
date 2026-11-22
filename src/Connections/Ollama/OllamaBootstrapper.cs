// Antes de abrir a ponte, verifica se o Ollama está no ar e, se preciso, o inicia.
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pi.Options;

namespace Pi.Connections.Ollama;

// Garante que o servidor local do Ollama esteja disponível antes de o chat
// começar a ler mensagens. Falhas são informadas, mas não impedem a subida.
public sealed class OllamaBootstrapper : IHostedService
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;
    private readonly IOllamaProcessLauncher _launcher;
    private readonly ILogger<OllamaBootstrapper> _logger;
    private Process? _ownedProcess;

    public OllamaBootstrapper(
        IHttpClientFactory httpClientFactory,
        IOptions<OllamaOptions> options,
        IOllamaProcessLauncher launcher,
        ILogger<OllamaBootstrapper> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(launcher);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _launcher = launcher;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(nameof(OllamaBootstrapper));
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.AutoStart || !IsLocal(_options.BaseUrl))
        {
            _logger.LogDebug(
                "Auto-start do Ollama ignorado (AutoStart={AutoStart}, BaseUrl={BaseUrl}).",
                _options.AutoStart,
                _options.BaseUrl);

            return;
        }

        if (await IsReadyAsync(cancellationToken))
        {
            _logger.LogInformation("Ollama já está ativo em {BaseUrl}.", _options.BaseUrl);
            return;
        }

        var resultado = _launcher.Start(_options.ExecutablePath);
        if (!resultado.Started)
        {
            _logger.LogError("{Message}", resultado.Error);
            return;
        }

        _ownedProcess = resultado.Process;
        _logger.LogInformation("Aguardando Ollama responder em {BaseUrl}...", _options.BaseUrl);

        using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limite.CancelAfter(TimeSpan.FromSeconds(_options.StartupTimeoutSeconds));

        while (!limite.IsCancellationRequested)
        {
            if (await IsReadyAsync(limite.Token))
            {
                _logger.LogInformation("Ollama pronto em {BaseUrl}.", _options.BaseUrl);
                return;
            }

            if (_ownedProcess is { HasExited: true } exited)
            {
                _logger.LogError(
                    "Ollama encerrou durante a inicialização (código {ExitCode}).",
                    exited.ExitCode);

                return;
            }

            try
            {
                await Task.Delay(PollInterval, limite.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        if (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                "Ollama não respondeu em {BaseUrl} dentro de {TimeoutSeconds}s. " +
                "A aplicação continua, mas mensagens não poderão ser respondidas.",
                _options.BaseUrl,
                _options.StartupTimeoutSeconds);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_options.StopOnExit && _ownedProcess is { HasExited: false } process)
        {
            try
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(milliseconds: 5000);
                _logger.LogInformation("Servidor Ollama iniciado pelo Pi foi encerrado.");
            }
            catch (Exception ex) when (ex is InvalidOperationException or Win32Exception
                                           or NotSupportedException)
            {
                _logger.LogWarning(ex, "Não foi possível encerrar o servidor Ollama.");
            }
        }

        return Task.CompletedTask;
    }

    private async Task<bool> IsReadyAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var probe = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            probe.CancelAfter(ProbeTimeout);

            using var response = await _httpClient.GetAsync(
                "/api/tags",
                HttpCompletionOption.ResponseHeadersRead, // começa a ler antes do corpo terminar
                probe.Token);

            // Qualquer resposta HTTP significa que a porta está aberta.
            return response is not null;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            _logger.LogDebug(ex, "Ollama ainda não responde em {BaseUrl}", _options.BaseUrl);
            return false;
        }
    }

    // Só damos start automático em localhost; servidor remoto é responsabilidade de quem o configurou.
    private static bool IsLocal(string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Host is "localhost" or "127.0.0.1" or "::1" or "[::1]";
    }
}
