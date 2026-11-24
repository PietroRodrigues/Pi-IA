// Guardrails da arquitetura alvo: árvore, fronteiras e contrato v1.
using Xunit;

namespace Pi.Bridge.Tests;

public class StructureTests
{
    [Fact]
    public void ArvoreAlvo_Existe()
    {
        var root = FindRoot();

        string[] paths =
        [
            "Pi.sln",
            "docs/protocol-v1.md",
            ".env.example",
            "src/Pi.Contracts/Protocol/Envelope.cs",
            "src/Pi.Core/Engine/AgentOrchestrator.cs",
            "src/Pi.Infrastructure/Llm/OllamaProvider.cs",
            "src/Pi.Host/Bridge/CommandDispatcher.cs",
            "src/Pi.Tui/src/index.ts",
            "src/Pi.Tui/src/protocol/schema.ts",
            "tests/Pi.Core.Tests/Pi.Core.Tests.csproj",
            "tests/Pi.Infrastructure.Tests/Pi.Infrastructure.Tests.csproj",
            "tests/Pi.Bridge.Tests/Pi.Bridge.Tests.csproj"
        ];

        Assert.All(paths, path => Assert.True(File.Exists(Path.Combine(root, path)), $"Arquivo ausente: {path}"));
        Assert.False(Directory.Exists(Path.Combine(root, "Pi.App")), "A pasta legada Pi.App/ ainda existe.");
    }

    [Fact]
    public void Core_NaoReferenciaCamadasExternas()
    {
        var root = FindRoot();
        var core = Path.Combine(root, "src", "Pi.Core");

        foreach (var file in Directory.EnumerateFiles(core, "*.cs", SearchOption.AllDirectories))
        {
            var code = File.ReadAllText(file);
            Assert.DoesNotContain("Pi.Infrastructure", code, StringComparison.Ordinal);
            Assert.DoesNotContain("Pi.Host", code, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void CodigoCSharp_NaoContemReferenciasLegadas()
    {
        var root = FindRoot();

        foreach (var directory in new[] { Path.Combine(root, "src"), Path.Combine(root, "tests") })
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(file) == nameof(StructureTests) + ".cs")
                {
                    continue;
                }

                var code = File.ReadAllText(file);
                Assert.DoesNotContain("Pi.App", code, StringComparison.Ordinal);
                Assert.DoesNotContain("BodyTools", code, StringComparison.Ordinal);
            }
        }
    }

    private static string FindRoot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(start);

            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Pi.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException("Raiz do repositório (Pi.sln) não encontrada.");
    }
}
