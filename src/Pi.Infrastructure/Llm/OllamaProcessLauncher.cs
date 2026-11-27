// Localiza o executável do Ollama e dispara "serve" sem janela e sem herdar o stdout.
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Pi.Contracts.Constants;

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
                                 candidate.EndsWith(PiConstants.ProcessNames.WindowsExecutableSuffix,
                                     StringComparison.OrdinalIgnoreCase));

            if (looksLikePath && !File.Exists(candidate))
            {
                attempts.Add(string.Format(PiConstants.Messages.MissingFileAttemptTemplate, candidate));
                continue;
            }

            try
            {
                var startInfo = new ProcessStartInfo(candidate)
                {
                    Arguments = PiConstants.Commands.OllamaServe,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    // Nunca herda o stdout: ele pertence ao NDJSON lido pela TUI.
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                var process = Process.Start(startInfo);
                if (process is null)
                {
                    attempts.Add(string.Format(PiConstants.Messages.NoProcessCreatedAttemptTemplate, candidate));
                    continue;
                }

                process.OutputDataReceived += (_, _) => { };
                process.ErrorDataReceived += (_, _) => { };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                logger.LogInformation(PiConstants.Messages.LogOllamaStarted, candidate);
                return OllamaLaunchResult.Ok(process);
            }
            catch (Exception ex) when (ex is Win32Exception or IOException or InvalidOperationException
                                          or UnauthorizedAccessException)
            {
                attempts.Add(string.Format(PiConstants.Messages.FailedAttemptTemplate, candidate, ex.Message));
            }
        }

        return OllamaLaunchResult.Fail(string.Format(
            PiConstants.Messages.OllamaLaunchFailureTemplate,
            PiConstants.Endpoints.OllamaDownload,
            PiConstants.Env.OllamaExecutable,
            string.Join(PiConstants.Protocol.AttemptsSeparator, attempts)));
    }

    private static IEnumerable<string> Candidates(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            yield return configuredPath.Trim();
            yield break;
        }

        yield return PiConstants.ProcessNames.Ollama;

        if (OperatingSystem.IsWindows())
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            yield return Path.Combine(
                local,
                PiConstants.Paths.ProgramsFolderName,
                PiConstants.Paths.OllamaFolderName,
                PiConstants.ProcessNames.OllamaWindowsExecutable);
            yield return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                PiConstants.Paths.OllamaFolderName,
                PiConstants.ProcessNames.OllamaWindowsExecutable);
        }
        else if (OperatingSystem.IsMacOS())
        {
            yield return PiConstants.Paths.OllamaMacApplicationPath;
            yield return PiConstants.Paths.OllamaUsrLocalBinPath;
            yield return PiConstants.Paths.OllamaHomeBrewBinPath;
        }
        else
        {
            yield return PiConstants.Paths.OllamaUsrLocalBinPath;
            yield return PiConstants.Paths.OllamaUsrBinPath;
            yield return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                PiConstants.Paths.OllamaHomeFolderName,
                PiConstants.Paths.BinFolderName,
                PiConstants.ProcessNames.Ollama);
        }
    }
}
