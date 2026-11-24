// Leitura estruturada: caminho válido, traversal bloqueado e arquivo ausente.
using System.Text.Json;
using Pi.Core.Policies;
using Pi.Infrastructure.Options;
using Pi.Infrastructure.Tools;
using Xunit;

namespace Pi.Infrastructure.Tests;

public class ReadFileToolTests
{
    [Fact]
    public async Task ExecuteResultAsync_ArquivoDentroDoWorkspace_RetornaLinhasNumeradas()
    {
        var root = Directory.CreateTempSubdirectory("pi-read-").FullName;

        try
        {
            await File.WriteAllTextAsync(Path.Combine(root, "a.txt"), "linha 1\nlinha 2\n");
            var sut = CreateSut(root);

            var result = await sut.ExecuteResultAsync(JsonSerializer.Serialize(new
            {
                path = "a.txt",
                offset = 0,
                limit = 10
            }));

            Assert.True(result.Ok, result.Error);
            Assert.Contains("linha 1", result.Output, StringComparison.Ordinal);
            Assert.Contains("linha 2", result.Output, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteResultAsync_PathTraversal_RetornaErroDoSandbox()
    {
        var parent = Directory.CreateTempSubdirectory("pi-read-outside-").FullName;
        var root = Path.Combine(parent, "workspace");
        Directory.CreateDirectory(root);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(parent, "fora.txt"), "segredo");
            var sut = CreateSut(root);

            var result = await sut.ExecuteResultAsync(
                JsonSerializer.Serialize(new { path = "../fora.txt" }));

            Assert.False(result.Ok);
            Assert.Contains("fora do workspace", result.Error, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("segredo", result.Output, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteResultAsync_ArquivoInexistente_RetornaErroClaro()
    {
        var root = Directory.CreateTempSubdirectory("pi-read-").FullName;

        try
        {
            var sut = CreateSut(root);

            var result = await sut.ExecuteResultAsync(
                JsonSerializer.Serialize(new { path = "inexistente.txt" }));

            Assert.False(result.Ok);
            Assert.Contains("não encontrado", result.Error, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteResultAsync_Diretorio_RetornaErroClaro()
    {
        var root = Directory.CreateTempSubdirectory("pi-read-").FullName;

        try
        {
            Directory.CreateDirectory(Path.Combine(root, "pasta"));
            var sut = CreateSut(root);

            var result = await sut.ExecuteResultAsync(
                JsonSerializer.Serialize(new { path = "pasta" }));

            Assert.False(result.Ok);
            Assert.Contains("diretório", result.Error, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static ReadFileTool CreateSut(string root) =>
        new(new WorkspaceSandboxPolicy(root), new ToolSettings { MaxReadBytes = 4096 });
}
