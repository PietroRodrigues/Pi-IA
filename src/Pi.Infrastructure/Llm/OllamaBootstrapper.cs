// Garante que o servidor local do Ollama esteja disponível antes do primeiro turno.
// Falhas são informadas, mas não impedem a subida da aplicação.
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Pi.Contracts.Constants;
using Pi.Infrastructure.Options;

namespace Pi.Infrastructure.Llm;

public sealed class OllamaBootstrapper(
    HttpClient httpClient,
    OllamaSettings settings,
    IOllamaProcessLauncher launcher,
    IOllamaProcessDiscovery discovery,
    ILogger<OllamaBootstrapper> logger)
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(PiConstants.Timeouts.ProbeSeconds);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(PiConstants.Timeouts.PollMilliseconds);

    private Process? _ownedProcess;
    private int _stopped;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!settings.AutoStart || !IsLocal(settings.BaseUrl))
        {
            return;
        }

        if (await IsReadyAsync(cancellationToken))
        {
            logger.LogInformation(PiConstants.Messages.LogOllamaAlreadyActive, settings.BaseUrl);
            return;
        }

        // Uma instância local pode estar apenas demorando a subir: nunca
        // abrimos uma segunda enquanto essa ainda está viva.
        if (HasLocalProcess())
        {
            logger.LogInformation(PiConstants.Messages.LogOllamaLocalDetected, settings.BaseUrl);
            await WaitUntilReadyAsync(cancellationToken, null);
            return;
        }

        var result = launcher.Start(settings.ExecutablePath);
        if (!result.Started)
        {
            logger.LogError(PiConstants.Messages.LogGenericMessage, result.Error);
            return;
        }

        _ownedProcess = result.Process;
        await WaitUntilReadyAsync(cancellationToken, _ownedProcess);
    }

    private bool HasLocalProcess()
    {
        try
        {
            return discovery.FindLocal().Count > 0;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, PiConstants.Messages.LogOllamaProcessCheckFailure);
            return false;
        }
    }

    private async Task WaitUntilReadyAsync(CancellationToken cancellationToken, Process? owned)
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limit.CancelAfter(TimeSpan.FromSeconds(
            Math.Max(PiConstants.Timeouts.MinimumSeconds, settings.StartupTimeoutSeconds)));

        while (!limit.IsCancellationRequested)
        {
            if (await IsReadyAsync(limit.Token))
            {
                logger.LogInformation(PiConstants.Messages.LogOllamaReady, settings.BaseUrl);
                return;
            }

            if (owned is { HasExited: true } exited)
            {
                logger.LogError(PiConstants.Messages.LogOllamaExitedDuringStartup, exited.ExitCode);
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
            logger.LogWarning(
                PiConstants.Messages.LogOllamaStartupTimeout,
                settings.BaseUrl,
                settings.StartupTimeoutSeconds);
        }
    }

    // Opt-in: encerra apenas o processo que o Pi iniciou. Um Ollama já aberto
    // pelo usuário nunca é encerrado — ele é reutilizado na próxima execução.
    public void StopOnExit()
    {
        if (!settings.StopOnExit || !IsLocal(settings.BaseUrl) ||
            Interlocked.Exchange(ref _stopped, 1) != 0)
        {
            return;
        }

        var process = _ownedProcess;
        if (process is null) return;

        try
        {
            if (process.HasExited) return;
            process.Kill(entireProcessTree: true);
            process.WaitForExit(PiConstants.Timeouts.KillWaitMilliseconds);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, PiConstants.Messages.LogOllamaStopFailure);
        }
    }

    private async Task<bool> IsReadyAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var probe = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            probe.CancelAfter(ProbeTimeout);

            using var response = await httpClient.GetAsync(
                PiConstants.Endpoints.Tags,
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
        PiConstants.Endpoints.LocalHosts.Contains(uri.Host, StringComparer.Ordinal);
}
