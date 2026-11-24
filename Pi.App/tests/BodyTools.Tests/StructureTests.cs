// Guardrails da arquitetura MVC modular: Model isolado, View contida,
// Controller como única entrada e entidades da conversa sem dependências.
using Xunit;

namespace Pi.BodyTools.Tests;

// Regras curtas, sem reflection: os módulos ficam sob Pi.App/Modulos/ e as camadas
// MVC do BodyTools não se cruzam. A ponte NDJSON continua sendo o único
// contrato entre BodyTools e interfaceTui.
public sealed class StructureTests
{
    [Fact]
    public void Modulos_DevemEstarSobPiApp()
    {
        string[] modulos =
        [
            CaminhoDe("Pi.App"),
            CaminhoDe("Pi.App", "Modulos", "BodyTools"),
            CaminhoDe("Pi.App", "Modulos", "interfaceTui"),
            CaminhoDe("Pi.App", "tests", "BodyTools.Tests")
        ];

        Assert.All(modulos, modulo =>
            Assert.True(Directory.Exists(modulo), $"Módulo ausente: {modulo}"));
    }

    [Fact]
    public void Pai_DeveSerOrquestradorComMvcProprio()
    {
        var pai = CaminhoDe("Pi.App");

        string[] camadas = ["Model", "View", "Controller"];
        Assert.All(camadas, camada =>
            Assert.True(Directory.Exists(Path.Combine(pai, camada)), $"Camada ausente no pai: {camada}"));

        string[] duplicacoes =
        [
            "Pi.Model.Chat",
            "BridgeClient",
            "BridgeView",
            "IAgentEventSink",
            "Console.In"
        ];

        // Só as fontes do pai: Modulos/ e tests/ têm as próprias regras.
        var violacoes = ArquivosDoPai()
            .SelectMany(arquivo => File.ReadAllLines(arquivo))
            .Where(linha => duplicacoes.Any(nome => linha.Contains(nome, StringComparison.Ordinal)));

        Assert.True(
            !violacoes.Any(),
            "O Pi.App só orquestra o ciclo de vida; não pode duplicar chat/ponte. Violações:\n" +
            string.Join("\n", violacoes));
    }

    [Fact]
    public void Model_NaoDeveReferenciarControllerNemView()
    {
        var violacoes = LinhasDe("Pi.App", "Modulos", "BodyTools", "Model")
            .Where(linha => linha.Contains("Pi.Controller", StringComparison.Ordinal) ||
                            linha.Contains("Pi.View", StringComparison.Ordinal));

        Assert.True(
            !violacoes.Any(),
            "Model não pode conhecer Controller nem View. Violações:\n" +
            string.Join("\n", violacoes));
    }

    [Fact]
    public void View_SoDeveReferenciarModelChat()
    {
        var violacoes = LinhasDe("Pi.App", "Modulos", "BodyTools", "View")
            .Where(linha =>
                (linha.Contains("Pi.Model.", StringComparison.Ordinal) &&
                 !linha.Contains("Pi.Model.Chat", StringComparison.Ordinal)) ||
                linha.Contains("Pi.Controller", StringComparison.Ordinal));

        Assert.True(
            !violacoes.Any(),
            "A View da ponte só pode usar Pi.Model.Chat (eventos do agente). Violações:\n" +
            string.Join("\n", violacoes));
    }

    [Fact]
    public void Controller_DeveSerOUnicoLeitorDeStdin()
    {
        var violacoes = ArquivosDe("Pi.App", "Modulos", "BodyTools")
            .Where(arquivo => !arquivo.Replace('\\', '/').Contains("/Controller/", StringComparison.Ordinal))
            .SelectMany(arquivo => File.ReadAllLines(arquivo)
                .Where(linha => linha.Contains("Console.In", StringComparison.Ordinal) ||
                                linha.Contains("_input.ReadLineAsync", StringComparison.Ordinal))
                .Select(linha => $"{Path.GetFileName(arquivo)} -> {linha.Trim()}"));

        Assert.True(
            !violacoes.Any(),
            "Somente o Controller pode ler o stdin da ponte. Violações:\n" +
            string.Join("\n", violacoes));
    }

    [Fact]
    public void TiposDaCliTextual_NaoDevemVoltar()
    {
        string[] proibidos =
        [
            "Pi.Frontend",
            "Pi.CLI",
            "ConsoleChatView",
            "IChatView",
            "NullChatView",
            "ChatSession",
            "ChatWorker",
            "ProcessMessage",
            "StdioBridgeWorker",
            "BridgeAgentEventSink",
            "AddBridge"
        ];

        var violacoes = LinhasDe("Pi.App", "Modulos", "BodyTools")
            .Where(linha => proibidos.Any(nome => linha.Contains(nome, StringComparison.Ordinal)));

        Assert.True(
            !violacoes.Any(),
            "Tipos removidos não devem reaparecer no BodyTools. Violações:\n" +
            string.Join("\n", violacoes));
    }

    [Fact]
    public void EntidadesDaConversa_PermanecemIsoladas()
    {
        string[] arquivos =
        [
            CaminhoDe("Pi.App", "Modulos", "BodyTools", "Model", "Chat", "ChatMessage.cs"),
            CaminhoDe("Pi.App", "Modulos", "BodyTools", "Model", "Chat", "Conversation.cs")
        ];

        var violacoes = arquivos
            .SelectMany(arquivo => File.ReadAllLines(arquivo)
                .Where(linha => linha.StartsWith("using ", StringComparison.Ordinal))
                .Select(linha => $"{Path.GetFileName(arquivo)} -> {linha.Trim()}"));

        Assert.True(
            !violacoes.Any(),
            "ChatMessage e Conversation não devem ter dependências. Violações:\n" +
            string.Join("\n", violacoes));
    }

    private static IEnumerable<string> ArquivosDoPai()
    {
        var pai = CaminhoDe("Pi.App");

        string[] camadas = ["Model", "View", "Controller"];

        return camadas
            .SelectMany(camada => Directory.GetFiles(Path.Combine(pai, camada), "*.cs", SearchOption.AllDirectories))
            .Append(Path.Combine(pai, "Program.cs"));
    }

    private static IEnumerable<string> LinhasDe(params string[] partes) =>
        ArquivosDe(partes).SelectMany(arquivo => File.ReadAllLines(arquivo));

    private static IEnumerable<string> ArquivosDe(params string[] partes) =>
        Directory.GetFiles(CaminhoDe(partes), "*.cs", SearchOption.AllDirectories);

    private static string CaminhoDe(params string[] partes)
    {
        var diretorio = new DirectoryInfo(AppContext.BaseDirectory);

        while (diretorio is not null)
        {
            // Exige o projeto-fonte como marcador: artifacts/bin/Pi.App também existe.
            var marcador = Path.Combine(diretorio.FullName, "Pi.App", "Pi.App.csproj");
            var candidato = Path.Combine(diretorio.FullName, Path.Combine(partes));
            if (File.Exists(marcador) && (File.Exists(candidato) || Directory.Exists(candidato)))
            {
                return candidato;
            }

            diretorio = diretorio.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Não foi possível localizar {Path.Combine(partes)} a partir de {AppContext.BaseDirectory}.");
    }
}
