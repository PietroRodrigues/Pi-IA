// Listagem do workspace: profundidade, glob, sandbox, ignore e limite de itens.
using System.Text.Json;
using Pi.Core.Policies;
using Pi.Infrastructure.Options;
using Pi.Infrastructure.Tools;
using Xunit;

namespace Pi.Infrastructure.Tests;

public class ListDirectoryToolTests
{
    [Fact]
    public async Task ExecuteResultAsync_RaizComDepthZero_ListaArquivosEPastasSemDescer()
    {
        var root = Directory.CreateTempSubdirectory("pi-list-").FullName;

        try
        {
            Directory.CreateDirectory(Path.Combine(root, "sub"));
            await File.WriteAllTextAsync(Path.Combine(root, "a.txt"), "a");
            await File.WriteAllTextAsync(Path.Combine(root, "sub", "b.txt"), "b");

            var result = await CreateSut(root).ExecuteResultAsync(Json(new { path = ".", depth = 0 }));

            Assert.True(result.Ok, result.Error);
            Assert.Contains("a.txt", result.Output, StringComparison.Ordinal);
            Assert.Contains("sub", result.Output, StringComparison.Ordinal);
            Assert.DoesNotContain("b.txt", result.Output, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteResultAsync_Subpasta_ListaSomenteConteudoDaPasta()
    {
        var root = Directory.CreateTempSubdirectory("pi-list-").FullName;

        try
        {
            Directory.CreateDirectory(Path.Combine(root, "sub"));
            await File.WriteAllTextAsync(Path.Combine(root, "a.txt"), "a");
            await File.WriteAllTextAsync(Path.Combine(root, "sub", "b.txt"), "b");

            var result = await CreateSut(root).ExecuteResultAsync(Json(new { path = "sub", depth = 0 }));

            Assert.True(result.Ok, result.Error);
            Assert.Contains("b.txt", result.Output, StringComparison.Ordinal);
            Assert.DoesNotContain("a.txt", result.Output, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteResultAsync_DepthZeroEUm_ControlaQuantidadeDeNiveis()
    {
        var root = Directory.CreateTempSubdirectory("pi-list-").FullName;

        try
        {
            Directory.CreateDirectory(Path.Combine(root, "sub", "deep"));
            await File.WriteAllTextAsync(Path.Combine(root, "sub", "b.txt"), "b");
            await File.WriteAllTextAsync(Path.Combine(root, "sub", "deep", "c.txt"), "c");
            var sut = CreateSut(root);

            var shallow = await sut.ExecuteResultAsync(Json(new { depth = 0 }));
            var deeper = await sut.ExecuteResultAsync(Json(new { depth = 1 }));

            Assert.True(shallow.Ok, shallow.Error);
            Assert.True(deeper.Ok, deeper.Error);
            Assert.DoesNotContain("b.txt", shallow.Output, StringComparison.Ordinal);
            Assert.Contains("b.txt", deeper.Output, StringComparison.Ordinal);
            Assert.DoesNotContain("c.txt", deeper.Output, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteResultAsync_DepthAcimaDoLimite_ClampaSemDescerALinhaProfunda()
    {
        var root = Directory.CreateTempSubdirectory("pi-list-").FullName;

        try
        {
            Directory.CreateDirectory(Path.Combine(root, "d1", "d2", "d3", "d4"));
            await File.WriteAllTextAsync(Path.Combine(root, "d1", "d2", "d3", "x.txt"), "x");
            await File.WriteAllTextAsync(Path.Combine(root, "d1", "d2", "d3", "d4", "y.txt"), "y");

            var result = await CreateSut(root).ExecuteResultAsync(Json(new { depth = 99 }));

            Assert.True(result.Ok, result.Error);
            Assert.Contains("x.txt", result.Output, StringComparison.Ordinal);
            Assert.DoesNotContain("y.txt", result.Output, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteResultAsync_PatternCs_AplicaGlobTambemEmSubpastas()
    {
        var root = Directory.CreateTempSubdirectory("pi-list-").FullName;

        try
        {
            Directory.CreateDirectory(Path.Combine(root, "sub"));
            await File.WriteAllTextAsync(Path.Combine(root, "a.cs"), "a");
            await File.WriteAllTextAsync(Path.Combine(root, "b.txt"), "b");
            await File.WriteAllTextAsync(Path.Combine(root, "sub", "c.cs"), "c");
            await File.WriteAllTextAsync(Path.Combine(root, "sub", "d.txt"), "d");

            var result = await CreateSut(root).ExecuteResultAsync(Json(
                new { depth = 1, pattern = "*.cs" }));

            Assert.True(result.Ok, result.Error);
            Assert.Contains("a.cs", result.Output, StringComparison.Ordinal);
            Assert.Contains("c.cs", result.Output, StringComparison.Ordinal);
            Assert.DoesNotContain("b.txt", result.Output, StringComparison.Ordinal);
            Assert.DoesNotContain("d.txt", result.Output, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteResultAsync_PathTraversal_RetornaErroDoSandbox()
    {
        var parent = Directory.CreateTempSubdirectory("pi-list-outside-").FullName;
        var root = Path.Combine(parent, "workspace");
        Directory.CreateDirectory(root);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(parent, "fora.txt"), "segredo");

            var result = await CreateSut(root).ExecuteResultAsync(Json(new { path = "../fora" }));

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
    public async Task ExecuteResultAsync_GitIgnoreEDiretoriosPesados_SaoIgnorados()
    {
        var root = Directory.CreateTempSubdirectory("pi-list-").FullName;

        try
        {
            Directory.CreateDirectory(Path.Combine(root, "node_modules"));
            Directory.CreateDirectory(Path.Combine(root, "secret"));
            await File.WriteAllTextAsync(
                Path.Combine(root, ".gitignore"),
                "ignored.txt\nsecret/\n");
            await File.WriteAllTextAsync(Path.Combine(root, "ignored.txt"), "x");
            await File.WriteAllTextAsync(Path.Combine(root, "node_modules", "tool.js"), "x");
            await File.WriteAllTextAsync(Path.Combine(root, "secret", "pass.txt"), "x");
            await File.WriteAllTextAsync(Path.Combine(root, "ok.txt"), "x");

            var result = await CreateSut(root).ExecuteResultAsync(Json(new { depth = 1 }));

            Assert.True(result.Ok, result.Error);
            Assert.Contains("ok.txt", result.Output, StringComparison.Ordinal);
            Assert.DoesNotContain("ignored.txt", result.Output, StringComparison.Ordinal);
            Assert.DoesNotContain("node_modules", result.Output, StringComparison.Ordinal);
            Assert.DoesNotContain("pass.txt", result.Output, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteResultAsync_MuitosItens_ListaTruncadaNoLimite()
    {
        var root = Directory.CreateTempSubdirectory("pi-list-").FullName;

        try
        {
            foreach (var index in Enumerable.Range(1, 5))
            {
                await File.WriteAllTextAsync(Path.Combine(root, $"f{index}.txt"), "x");
            }

            var result = await CreateSut(root, maxResults: 3)
                .ExecuteResultAsync(Json(new { depth = 0 }));

            Assert.True(result.Ok, result.Error);
            Assert.Contains("limite de 3 itens", result.Output, StringComparison.Ordinal);
            Assert.Equal(4, result.Output.Split(Environment.NewLine).Length);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static ListDirectoryTool CreateSut(string root, int maxResults = 50) =>
        new(new WorkspaceSandboxPolicy(root), new ToolSettings { MaxResults = maxResults });

    private static string Json(object arguments) => JsonSerializer.Serialize(arguments);
}
