// Controller do pai: coordena o ciclo de vida dos dois módulos.
// Ele não fala NDJSON — a TUI continua sendo dona do terminal e do backend.
using System.Diagnostics;
using Pi.App.Model;
using Pi.App.View;

namespace Pi.App.Controller;

public sealed class RuntimeController(
    ModuleManifest manifest,
    LauncherView view,
    RuntimeState state)
{
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            manifest.Validate();
            view.Preparing(manifest);

            // Seam para smoke/CI: valida os módulos sem abrir um terminal.
            if (Environment.GetEnvironmentVariable("PI_SKIP_TUI") == "1")
            {
                view.Info("PI_SKIP_TUI=1: módulos validados; TUI não iniciada.");
                return 0;
            }

            if (!Directory.Exists(manifest.NodeModules))
            {
                state.SetPhase(RuntimePhase.Preparing);
                view.Installing();

                var codigo = await ExecutarAsync(
                    "bun", manifest.TuiDirectory, cancellationToken, "install", "--frozen-lockfile");

                if (codigo != 0)
                {
                    view.Error($"bun install falhou (código {codigo}).");
                    return codigo;
                }
            }

            var argumentos = new List<string>();
            var porta = Environment.GetEnvironmentVariable("PI_TUI_INSPECT");

            if (!string.IsNullOrWhiteSpace(porta))
            {
                argumentos.Add($"--inspect={porta.Trim()}");
            }

            argumentos.Add("run");
            argumentos.Add("dev:debug");

            state.SetPhase(RuntimePhase.Starting);
            view.Starting(string.Join(" ", argumentos));

            var iniciacao = new ProcessStartInfo("bun")
            {
                WorkingDirectory = manifest.TuiDirectory,
                UseShellExecute = false
            };

            foreach (var argumento in argumentos)
            {
                iniciacao.ArgumentList.Add(argumento);
            }

            using var tui = Process.Start(iniciacao)
                ?? throw new InvalidOperationException("Não foi possível iniciar o bun.");

            state.SetTuiProcess(tui.Id);
            state.SetPhase(RuntimePhase.Running);
            view.Ready();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            ConsoleCancelEventHandler aoCancelar = (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
            };

            Console.CancelKeyPress += aoCancelar;

            try
            {
                await tui.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                view.Stopping();
                Encerrar(tui);
                await tui.WaitForExitAsync();
            }
            finally
            {
                Console.CancelKeyPress -= aoCancelar;
            }

            return tui.ExitCode;
        }
        catch (Exception ex)
        {
            view.Error(ex.Message);
            return 1;
        }
        finally
        {
            state.SetPhase(RuntimePhase.Idle);
            state.SetTuiProcess(null);
        }
    }

    private static async Task<int> ExecutarAsync(
        string executavel,
        string diretorio,
        CancellationToken cancellationToken,
        params string[] argumentos)
    {
        var iniciacao = new ProcessStartInfo(executavel)
        {
            WorkingDirectory = diretorio,
            UseShellExecute = false
        };

        foreach (var argumento in argumentos)
        {
            iniciacao.ArgumentList.Add(argumento);
        }

        using var processo = Process.Start(iniciacao)
            ?? throw new InvalidOperationException($"Não foi possível iniciar '{executavel}'.");

        await processo.WaitForExitAsync(cancellationToken);
        return processo.ExitCode;
    }

    private static void Encerrar(Process processo)
    {
        if (processo.HasExited) return;

        try
        {
            // Mata a TUI e, com ela, o subprocesso BodyTools.
            processo.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // O processo já tinha saído entre a verificação e o Kill.
        }
    }
}
