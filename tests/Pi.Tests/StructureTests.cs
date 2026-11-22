using Xunit;

namespace Pi.Tests;

/// <summary>
/// Três regras apenas, fáceis de ler: o backend não conhece o frontend, o
/// frontend conhece só o contrato IChatView, e as entidades da conversa não
/// dependem de nada além da BCL.
/// </summary>
public sealed class StructureTests
{
    [Fact]
    public void Backend_NaoDeveReferenciarFrontend()
    {
        var violacoes = LinhasDe("src", "Backend")
            .Where(linha => linha.Contains("Pi.Frontend", StringComparison.Ordinal));

        Assert.True(
            !violacoes.Any(),
            "Backend não pode conhecer o Frontend. Violações:\n" + string.Join("\n", violacoes));
    }

    [Fact]
    public void Frontend_SoDeveReferenciarContratoDoChat()
    {
        var violacoes = LinhasDe("src", "Frontend")
            .Where(linha => linha.Contains("Pi.Backend.", StringComparison.Ordinal) &&
                            !linha.Contains("Pi.Backend.Chat", StringComparison.Ordinal));

        Assert.True(
            !violacoes.Any(),
            "Frontend só pode usar Pi.Backend.Chat.IChatView. Violações:\n" +
            string.Join("\n", violacoes));
    }

    [Fact]
    public void EntidadesDaConversa_PermanecemIsoladas()
    {
        string[] arquivos =
        [
            CaminhoDe("src", "Backend", "Chat", "ChatMessage.cs"),
            CaminhoDe("src", "Backend", "Chat", "Conversation.cs")
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
