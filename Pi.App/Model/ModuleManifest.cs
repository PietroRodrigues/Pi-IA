// Model do projeto pai: apenas descobre e valida os módulos sob Pi.App/Modulos/.
// Não conhece Controller, View, NDJSON nem a regra de negócio do chat.
namespace Pi.App.Model;

public sealed class ModuleManifest
{
    private ModuleManifest(string root, string solution, string backendDll, string tuiDirectory)
    {
        Root = root;
        Solution = solution;
        BackendDll = backendDll;
        TuiDirectory = tuiDirectory;
    }

    public string Root { get; }
    public string Solution { get; }
    public string BackendDll { get; }
    public string TuiDirectory { get; }
    public string PackageJson => Path.Combine(TuiDirectory, "package.json");
    public string NodeModules => Path.Combine(TuiDirectory, "node_modules");

    public static ModuleManifest Discover()
    {
        // A saída fica em <raiz>/artifacts/bin/Pi.App/<config>; subir até encontrar
        // a configuração compartilhada evita depender do cwd de quem iniciou o pai.
        var diretorio = new DirectoryInfo(AppContext.BaseDirectory);

        while (diretorio is not null)
        {
            var solucao = Path.Combine(diretorio.FullName, "Pi.App", "Pi.App.slnx");
            var global = Path.Combine(diretorio.FullName, "global.json");

            if (File.Exists(solucao) && File.Exists(global))
            {
                return new ModuleManifest(
                    diretorio.FullName,
                    solucao,
                    Path.Combine(diretorio.FullName, "artifacts", "bin", "BodyTools", "debug", "BodyTools.dll"),
                    Path.Combine(diretorio.FullName, "Pi.App", "Modulos", "interfaceTui"));
            }

            diretorio = diretorio.Parent;
        }

        throw new DirectoryNotFoundException(
            "Não foi possível localizar a raiz do Pi-IA (global.json + Pi.App/Pi.App.slnx).");
    }

    public void Validate()
    {
        string[] necessarios = [Solution, BackendDll, PackageJson];
        var faltando = necessarios.Where(caminho => !File.Exists(caminho)).ToArray();

        if (faltando.Length == 0) return;

        throw new InvalidOperationException(
            "Módulos incompletos. Rode 'dotnet build Pi.App/Pi.App.slnx' e 'bun install' em Pi.App/Modulos/interfaceTui.\n" +
            string.Join("\n", faltando.Select(caminho => $"  - {caminho}")));
    }
}
