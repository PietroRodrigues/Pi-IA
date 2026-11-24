// Garante que o servidor local do Ollama esteja disponível antes do primeiro turno.
// Falhas são informadas, mas não impedem a subida da aplicação.
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Pi.Infrastructure.Options;

namespace Pi.Infrastructure.Llm;

public sealed class OllamaBootstrapper(
    HttpClient httpClient,
    OllamaSettings settings,
    IOllamaProcessLauncher launcher,
    ILogger<OllamaBootstrapper> logger)
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

    private Process? _ownedProcess;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!settings.AutoStart || !IsLocal(settings.BaseUrl))
        {
            return;
        }

        if (await IsReadyAsync(cancellationToken))
        {
            logger.LogInformation("Ollama já ativo em {BaseUrl}", settings.BaseUrl);
            return;
        }

        var result = launcher.Start(settings.ExecutablePath);
        if (!result.Started)
        {
            logger.LogError("{Message}", result.Error);
            return;
        }

        _ownedProcess = result.Process;
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limit.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, settings.StartupTimeoutSeconds)));

        while (!limit.IsCancellationRequested)
        {
            if (await IsReadyAsync(limit.Token))
            {
                logger.LogInformation("Ollama pronto em {BaseUrl}", settings.BaseUrl);
                return;
            }

            if (_ownedProcess is { HasExited: true } exited)
            {
                logger.LogError("Ollama encerrou durante a inicialização (código {ExitCode})", exited.ExitCode);
                return;
            }

            try
            {
                await Task.Delay(PollInterval, limit.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        if (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Ollama não respondeu em {BaseUrl} dentro de {Timeout}s", settings.BaseUrl, settings.StartupTimeoutSeconds);
        }
    }

    public void StopOnExit()
    {
        if (!settings.StopOnExit || _ownedProcess is not { HasExited: false } process)
        {
            return;
        }

        try
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit(5000);
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException)
        {
            logger.LogWarning(ex, "Não foi possível encerrar o Ollama iniciado pelo Pi");
        }
    }

    private async Task<bool> IsReadyAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var probe = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            probe.CancelAfter(ProbeTimeout);

            using var response = await httpClient.GetAsync(
                "/api/tags",
                HttpCompletionOption.ResponseHeadersRead,
                probe.Token);

            return response is not null;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            return false;
        }
    }

    private static bool IsLocal(string baseUrl) =>
        Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) &&
        uri.Host is "localhost" or "127.0.0.1" or "::1" or "[::1]";
}
