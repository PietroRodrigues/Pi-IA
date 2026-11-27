// Lê o branch atual com timeout e fallback seguro para fora de um repo Git.
using System.Diagnostics;
using Pi.Contracts.Constants;

namespace Pi.Host.Process;

public static class GitBranchReader
{
    public static string Read(string workspace)
    {
        try
        {
            var startInfo = new ProcessStartInfo(
                PiConstants.ProcessNames.Git,
                PiConstants.Commands.GitCurrentBranch)
            {
                WorkingDirectory = workspace,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process is null)
            {
                return PiConstants.Protocol.UnknownBranch;
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            if (!process.WaitForExit(PiConstants.Timeouts.GitWaitMilliseconds))
            {
                process.Kill(entireProcessTree: true);
                return PiConstants.Protocol.UnknownBranch;
            }

            return output.Length == 0 ? PiConstants.Protocol.UnknownBranch : output;
        }
        catch
        {
            return PiConstants.Protocol.UnknownBranch;
        }
    }
}
