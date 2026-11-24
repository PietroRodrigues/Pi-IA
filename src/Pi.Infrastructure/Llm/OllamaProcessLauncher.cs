// Localiza o executável do Ollama e dispara "serve" sem janela e sem herdar o stdout.
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Pi.Infrastructure.Llm;

public interface IOllamaProcessLauncher
{
    OllamaLaunchResult Start(string? configuredPath);
}

public sealed record OllamaLaunchResult(bool Started, Process? Process, string? Error)
{
    public static OllamaLaunchResult Ok(Process process) => new(true, process, null);

    public static OllamaLaunchResult Fail(string error) => new(false, null, error);
}

public sealed class OllamaProcessLauncher(ILogger<OllamaProcessLauncher> logger) : IOllamaProcessLauncher
{
    public OllamaLaunchResult Start(string? configuredPath)
    {
        var attempts = new List<string>();

        foreach (var candidate in Candidates(configuredPath))
        {
            var looksLikePath = candidate.Contains(Path.DirectorySeparatorChar) ||
                                candidate.Contains(Path.AltDirectorySeparatorChar) ||
                                (OperatingSystem.IsWindows() &&
                                 candidate.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));

            if (looksLikePath && !File.Exists(candidate))
            {
                attempts.Add($"{candidate} (arquivo não encontrado)");
                continue;
            }

            try
            {
                var startInfo = new ProcessStartInfo(candidate)
                {
                    Arguments = "serve",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    // Nunca herda o stdout: ele pertence ao NDJSON lido pela TUI.
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                var process = Process.Start(startInfo);
                if (process is null)
                {
                    attempts.Add($"{candidate} (nenhum processo criado)");
                    continue;
                }

                process.OutputDataReceived += (_, _) => { };
                process.ErrorDataReceived += (_, _) => { };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                logger.LogInformation("Ollama iniciado em segundo plano via {Executable}", candidate);
                return OllamaLaunchResult.Ok(process);
            }
            catch (Exception ex) when (ex is Win32Exception or IOException or InvalidOperationException
                                          or UnauthorizedAccessException)
            {
                attempts.Add($"{candidate} ({ex.Message})");
            }
        }

        return OllamaLaunchResult.Fail(
            "Não foi possível iniciar o Ollama. Instale em https://ollama.com/download " +
            "ou configure OLLAMA_EXECUTABLE. Tentativas: " + string.Join("; ", attempts));
    }

    private static IEnumerable<string> Candidates(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            yield return configuredPath.Trim();
            yield break;
        }

        yield return "ollama";

        if (OperatingSystem.IsWindows())
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            yield return Path.Combine(local, "Programs", "Ollama", "ollama.exe");
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ollama", "ollama.exe");
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
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ollama", "bin", "ollama");
        }
    }
}
