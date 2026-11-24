// Inicia a TUI Bun, monitora a saída e garante encerramento limpo (exit 0 no Ctrl+C).
using System.Diagnostics;

namespace Pi.Host.Process;

public static class RepoRoot
{
    public static string Find()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var directory = new DirectoryInfo(start);

            while (directory is not null)
            {
                var marker = Path.Combine(directory.FullName, "src", "Pi.Tui", "package.json");
                if (File.Exists(Path.Combine(directory.FullName, "global.json")) && File.Exists(marker))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException("Não foi possível localizar a raiz do Pi-IA (global.json + src/Pi.Tui).");
    }
}

public sealed class TuiProcessSupervisor
{
    private readonly string _root;
    private readonly string _tuiDirectory;

    public TuiProcessSupervisor(string? root = null)
    {
        _root = root ?? RepoRoot.Find();
        _tuiDirectory = Path.Combine(_root, "src", "Pi.Tui");
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var packageJson = Path.Combine(_tuiDirectory, "package.json");
        if (!File.Exists(packageJson))
        {
            await Console.Error.WriteLineAsync($"[Pi.Host] ERRO: {packageJson} não encontrado.");
            return 1;
        }

        if (Environment.GetEnvironmentVariable("PI_SKIP_TUI") == "1")
        {
            await Console.Error.WriteLineAsync("[Pi.Host] PI_SKIP_TUI=1: ambiente validado; TUI não iniciada.");
            return 0;
        }

        if (!Directory.Exists(Path.Combine(_tuiDirectory, "node_modules")))
        {
            var installCode = await ExecuteAsync("bun", _tuiDirectory, cancellationToken, "install", "--frozen-lockfile");
            if (installCode != 0)
            {
                await Console.Error.WriteLineAsync($"[Pi.Host] bun install falhou (código {installCode}).");
                return installCode;
            }
        }

        var arguments = new List<string>();
        var inspect = Environment.GetEnvironmentVariable("PI_TUI_INSPECT");

        if (!string.IsNullOrWhiteSpace(inspect))
        {
            arguments.Add($"--inspect={inspect.Trim()}");
        }

        arguments.Add("run");
        arguments.Add("src/index.ts");

        var startInfo = new ProcessStartInfo("bun")
        {
            WorkingDirectory = _tuiDirectory,
            UseShellExecute = false
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var tui = System.Diagnostics.Process.Start(startInfo)
            ?? throw new InvalidOperationException("Não foi possível iniciar o bun.");

        await Console.Error.WriteLineAsync($"[Pi.Host] TUI iniciada (pid {tui.Id}); Ctrl+C encerra os processos.");

        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        ConsoleCancelEventHandler onCancel = (_, args) =>
        {
            args.Cancel = true;
            stop.Cancel();
        };

        Console.CancelKeyPress += onCancel;

        try
        {
            await tui.WaitForExitAsync(stop.Token);
        }
        catch (OperationCanceledException)
        {
            await Console.Error.WriteLineAsync("[Pi.Host] encerrando a árvore de processos...");
            KillTree(tui);
            await tui.WaitForExitAsync();
        }
        finally
        {
            Console.CancelKeyPress -= onCancel;
        }

        // Ctrl+C é uma saída normal: nunca devolve código 1 ao depurador.
        return 0;
    }

    private static async Task<int> ExecuteAsync(
        string executable,
        string directory,
        CancellationToken cancellationToken,
        params string[] arguments)
    {
        var startInfo = new ProcessStartInfo(executable)
        {
            WorkingDirectory = directory,
            UseShellExecute = false
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = System.Diagnostics.Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Não foi possível iniciar '{executable}'.");

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode;
    }

    private static void KillTree(System.Diagnostics.Process process)
    {
        if (process.HasExited)
        {
            return;
        }

        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // O processo saiu entre a verificação e o Kill.
        }
    }
}
