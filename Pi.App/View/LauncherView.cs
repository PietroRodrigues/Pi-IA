// View do pai: texto para o desenvolvedor, sempre no stderr.
// O stdout pertence ao NDJSON da ponte e nunca é tocado aqui.
using Pi.App.Model;

namespace Pi.App.View;

public sealed class LauncherView(RuntimeState state)
{
    public void Preparing(ModuleManifest manifest)
    {
        Escrever($"raiz={manifest.Root}");
        Escrever($"backend={manifest.BackendDll}");
        Escrever($"interface={manifest.TuiDirectory}");
    }

    public void Installing() => Escrever("instalando dependências da TUI (bun install --frozen-lockfile)...");

    public void Starting(string command) => Escrever($"iniciando TUI: bun {command}");

    public void Ready() => Escrever($"TUI pronta (pid {state.TuiProcessId}); Ctrl+C encerra os módulos.");

    public void Stopping() => Escrever("encerrando a árvore de processos...");

    public void Info(string message) => Escrever(message);

    public void Error(string message) => Console.Error.WriteLine($"[Pi.App] ERRO: {message}");

    private static void Escrever(string message) => Console.Error.WriteLine($"[Pi.App] {message}");
}
