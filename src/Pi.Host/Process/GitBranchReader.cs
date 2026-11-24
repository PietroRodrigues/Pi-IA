// Lê o branch atual com timeout e fallback seguro para fora de um repo Git.
using System.Diagnostics;

namespace Pi.Host.Process;

public static class GitBranchReader
{
    public static string Read(string workspace)
    {
        try
        {
            var startInfo = new ProcessStartInfo("git", "rev-parse --abbrev-ref HEAD")
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
