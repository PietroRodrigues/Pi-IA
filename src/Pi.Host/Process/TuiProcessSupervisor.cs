// Inicia a TUI Bun, monitora a saída e garante encerramento limpo (exit 0 no Ctrl+C).
using System.Diagnostics;
using Pi.Contracts.Constants;

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
                var marker = Path.Combine(
                    directory.FullName,
                    PiConstants.Paths.SourceFolderName,
                    PiConstants.Paths.TuiFolderName,
                    PiConstants.Paths.PackageJsonFileName);
                if (File.Exists(Path.Combine(directory.FullName, PiConstants.Paths.GlobalJsonFileName)) &&
                    File.Exists(marker))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException(string.Format(
            PiConstants.Messages.RepoRootNotFoundTemplate,
            PiConstants.Paths.GlobalJsonFileName,
            PiConstants.Paths.TuiRelativeDisplay));
    }
}

public sealed class TuiProcessSupervisor
{
    private readonly string _root;
    private readonly string _tuiDirectory;

    public TuiProcessSupervisor(string? root = null)
    {
        _root = root ?? RepoRoot.Find();
        _tuiDirectory = Path.Combine(
            _root,
            PiConstants.Paths.SourceFolderName,
            PiConstants.Paths.TuiFolderName);
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var packageJson = Path.Combine(_tuiDirectory, PiConstants.Paths.PackageJsonFileName);
        if (!File.Exists(packageJson))
        {
            await Console.Error.WriteLineAsync(string.Format(
                PiConstants.Messages.TuiPackageNotFoundTemplate,
                PiConstants.Protocol.HostLogPrefix,
                packageJson));
            return 1;
        }

        if (Environment.GetEnvironmentVariable(PiConstants.Env.PiSkipTui) == PiConstants.Protocol.TrueValue)
        {
            await Console.Error.WriteLineAsync(string.Format(
                PiConstants.Messages.TuiSkippedTemplate,
                PiConstants.Protocol.HostLogPrefix,
                PiConstants.Env.PiSkipTui,
                PiConstants.Protocol.TrueValue));
            return 0;
        }

        if (!Directory.Exists(Path.Combine(_tuiDirectory, PiConstants.Paths.NodeModulesFolderName)))
        {
            var installCode = await ExecuteAsync(
                PiConstants.ProcessNames.Bun,
                _tuiDirectory,
                cancellationToken,
                PiConstants.Commands.BunInstall,
                PiConstants.Commands.BunFrozenLockfile);
            if (installCode != 0)
            {
                await Console.Error.WriteLineAsync(string.Format(
                    PiConstants.Messages.TuiInstallFailedTemplate,
                    PiConstants.Protocol.HostLogPrefix,
                    PiConstants.ProcessNames.Bun,
                    PiConstants.Commands.BunInstall,
                    installCode));
                return installCode;
            }
        }

        var arguments = new List<string>();
        var inspect = Environment.GetEnvironmentVariable(PiConstants.Env.PiTuiInspect);

        if (!string.IsNullOrWhiteSpace(inspect))
        {
            arguments.Add($"{PiConstants.Protocol.InspectorFlagPrefix}{inspect.Trim()}");
        }

        arguments.Add(PiConstants.Commands.BunRun);
        arguments.Add(PiConstants.Paths.TuiEntryPoint);

        var startInfo = new ProcessStartInfo(PiConstants.ProcessNames.Bun)
        {
            WorkingDirectory = _tuiDirectory,
            UseShellExecute = false
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var tui = System.Diagnostics.Process.Start(startInfo)
            ?? throw new InvalidOperationException(string.Format(
                PiConstants.Messages.CannotStartBunTemplate,
                PiConstants.ProcessNames.Bun));

        await Console.Error.WriteLineAsync(string.Format(
            PiConstants.Messages.TuiStartedTemplate,
            PiConstants.Protocol.HostLogPrefix,
            tui.Id));

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
            await Console.Error.WriteLineAsync(string.Format(
                PiConstants.Messages.TuiStoppingTemplate,
                PiConstants.Protocol.HostLogPrefix));
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
            ?? throw new InvalidOperationException(string.Format(
                PiConstants.Messages.CannotStartExecutableTemplate,
                executable));

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
