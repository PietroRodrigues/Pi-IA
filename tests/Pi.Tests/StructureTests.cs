// Guardrails da arquitetura: Core isolado, ponte contida, sem CLI antiga
// e entidades da conversa sem dependências.
using Xunit;

namespace Pi.Tests;

// Quatro regras curtas: o Core não conhece adapters, a ponte NDJSON usa só
// Pi.Core.Chat, a CLI textual não volta a aparecer, e as entidades da conversa
// não dependem de nada além da BCL.
public sealed class StructureTests
{
    [Fact]
    public void Core_NaoDeveReferenciarBridgeNemConexoes()
    {
        var violacoes = LinhasDe("src", "Core")
            .Where(linha => linha.Contains("Pi.Bridge", StringComparison.Ordinal) ||
                            linha.Contains("Pi.Connections", StringComparison.Ordinal));

        Assert.True(
            !violacoes.Any(),
            "Core não pode conhecer Bridge nem Connections/Ollama. Violações:\n" +
            string.Join("\n", violacoes));
    }

    [Fact]
    public void Bridge_SoDeveReferenciarCoreChat()
    {
        var violacoes = LinhasDe("src", "Bridge")
            .Where(linha => linha.Contains("Pi.Core.", StringComparison.Ordinal) &&
                            !linha.Contains("Pi.Core.Chat", StringComparison.Ordinal));

        Assert.True(
            !violacoes.Any(),
            "A ponte Bridge só pode usar Pi.Core.Chat (AgentService e eventos). Violações:\n" +
            string.Join("\n", violacoes));
    }

    [Fact]
    public void TiposDaCliTextual_NaoDevemVoltar()
    {
        string[] proibidos =
        [
            "Pi.Frontend",
            "ConsoleChatView",
            "IChatView",
            "NullChatView",
            "ChatSession",
            "ChatWorker",
            "ProcessMessage"
        ];

        var violacoes = LinhasDe("src")
            .Where(linha => proibidos.Any(nome => linha.Contains(nome, StringComparison.Ordinal)));

        Assert.True(
            !violacoes.Any(),
            "A CLI textual foi removida; estes tipos não devem reaparecer em src/. Violações:\n" +
            string.Join("\n", violacoes));
    }

    [Fact]
    public void EntidadesDaConversa_PermanecemIsoladas()
    {
        string[] arquivos =
        [
            CaminhoDe("src", "Core", "Chat", "ChatMessage.cs"),
            CaminhoDe("src", "Core", "Chat", "Conversation.cs")
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

    private static IEnumerable<string> LinhasDe(params string[] partes) =>
        Directory.GetFiles(CaminhoDe(partes), "*.cs", SearchOption.AllDirectories)
            .SelectMany(arquivo => File.ReadAllLines(arquivo));

    private static string CaminhoDe(params string[] partes)
    {
        var diretorio = new DirectoryInfo(AppContext.BaseDirectory);

        while (diretorio is not null)
        {
            var candidato = Path.Combine(diretorio.FullName, Path.Combine(partes));
            if (File.Exists(candidato) || Directory.Exists(candidato))
            {
                return candidato;
            }

            diretorio = diretorio.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Não foi possível localizar {Path.Combine(partes)} a partir de {AppContext.BaseDirectory}.");
    }
}
