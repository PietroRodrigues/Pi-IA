using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Pi.Backend.Bootstrap;

/// <summary>
/// Localiza e inicia o processo do servidor Ollama em segundo plano.
/// </summary>
public interface IOllamaProcessLauncher
{
    OllamaLaunchResult Start(string? configuredPath);
}

/// <summary>
/// Resultado de uma tentativa de inicialização. <see cref="Error"/> é preenchido
/// apenas quando não foi possível iniciar o processo.
/// </summary>
public sealed record OllamaLaunchResult(bool Started, Process? Process, string? Error)
{
    public static OllamaLaunchResult Ok(Process? process = null) => new(true, process, null);

    public static OllamaLaunchResult Fail(string error) => new(false, null, error);
}

/// <summary>
/// Procura o executável no PATH e nos caminhos de instalação conhecidos,
/// então dispara <c>ollama serve</c> sem abrir uma janela de console.
/// </summary>
public sealed class OllamaProcessLauncher : IOllamaProcessLauncher
{
    private readonly ILogger<OllamaProcessLauncher> _logger;

    public OllamaProcessLauncher(ILogger<OllamaProcessLauncher> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    public OllamaLaunchResult Start(string? configuredPath)
    {
        var tentativas = new List<string>();

        foreach (var candidato in Candidatos(configuredPath))
        {
            var eCaminho = candidato.Contains(Path.DirectorySeparatorChar) ||
                           candidato.Contains(Path.AltDirectorySeparatorChar) ||
                           (OperatingSystem.IsWindows() &&
                            candidato.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));

            if (eCaminho && !File.Exists(candidato))
            {
                tentativas.Add($"{candidato} (arquivo não encontrado)");
                continue;
            }

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = candidato,
                    Arguments = "serve",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    // Evita que o filho herde o stdout: no modo ponte ele é
                    // exclusivo do protocolo NDJSON lido pela TUI.
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                var processo = Process.Start(startInfo);
                if (processo is null)
                {
                    tentativas.Add($"{candidato} (nenhum processo criado)");
                    continue;
                }

                processo.OutputDataReceived += (_, _) => { };
                processo.ErrorDataReceived += (_, _) => { };
                processo.BeginOutputReadLine();
                processo.BeginErrorReadLine();

                _logger.LogInformation(
                    "Servidor Ollama iniciado em segundo plano usando {Executable}.",
                    candidato);

                return OllamaLaunchResult.Ok(processo);
            }
            catch (Exception ex) when (ex is Win32Exception or IOException
                                           or InvalidOperationException or UnauthorizedAccessException)
            {
                tentativas.Add($"{candidato} ({ex.Message})");
                _logger.LogDebug(ex, "Falha ao iniciar Ollama usando {Executable}", candidato);
            }
        }

        return OllamaLaunchResult.Fail(
            "Não foi possível iniciar o servidor Ollama. Instale em https://ollama.com/download " +
            "ou configure Ollama:ExecutablePath. Tentativas: " + string.Join("; ", tentativas));
    }

    private static IEnumerable<string> Candidatos(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            yield return configuredPath.Trim();
            yield break;
        }

        // "ollama" é resolvido pelo PATH pelo próprio CreateProcess.
        yield return "ollama";

        if (OperatingSystem.IsWindows())
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            yield return Path.Combine(local, "Programs", "Ollama", "ollama.exe");

            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            yield return Path.Combine(programFiles, "Ollama", "ollama.exe");
        }
        else if (OperatingSystem.IsMacOS())
        {
            yield return "/Applications/Ollama.app/Contents/Resources/ollama";
            yield return "/usr/local/bin/ollama";
            yield return "/opt/homebrew/bin/ollama";
        }
        else
        {
            yield return "/usr/local/bin/ollama";
            yield return "/usr/bin/ollama";

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            yield return Path.Combine(home, ".ollama", "bin", "ollama");
        }
    }
}
