// Criação de arquivo, patch por faixa de linhas, diff unificado e sandbox.
using System.Text.Json;
using Pi.Core.Policies;
using Pi.Infrastructure.Tools;
using Xunit;

namespace Pi.Infrastructure.Tests;

public class EditFileToolTests
{
    [Fact]
    public async Task ExecuteResultAsync_CreateFile_CriaArquivoComDiffUnificado()
    {
        var root = Directory.CreateTempSubdirectory("pi-edit-").FullName;

        try
        {
            var result = await CreateSut(root).ExecuteResultAsync(Json(new
            {
                path = "novo.txt",
                operation = "create_file",
                content = "linha 1\nlinha 2"
            }));
            var target = Path.Combine(root, "novo.txt");

            Assert.True(result.Ok, result.Error);
            Assert.Equal("linha 1\nlinha 2", await File.ReadAllTextAsync(target));
            Assert.Contains("--- a/novo.txt", result.Output, StringComparison.Ordinal);
            Assert.Contains("+++ b/novo.txt", result.Output, StringComparison.Ordinal);
            Assert.Contains("@@", result.Output, StringComparison.Ordinal);
            Assert.Contains("+linha 1", result.Output, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteResultAsync_CreateFileExistenteSemOverwrite_NaoSobrescreve()
    {
        var root = Directory.CreateTempSubdirectory("pi-edit-").FullName;

        try
        {
            var target = Path.Combine(root, "existe.txt");
            await File.WriteAllTextAsync(target, "original");

            var result = await CreateSut(root).ExecuteResultAsync(Json(new
            {
                path = "existe.txt",
                operation = "create_file",
                content = "novo"
            }));

            Assert.False(result.Ok);
            Assert.Contains("já existente", result.Error, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("original", await File.ReadAllTextAsync(target));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteResultAsync_CreateFileComOverwriteExplicito_Sobrescreve()
    {
        var root = Directory.CreateTempSubdirectory("pi-edit-").FullName;

        try
        {
            var target = Path.Combine(root, "existe.txt");
            await File.WriteAllTextAsync(target, "original");

            var result = await CreateSut(root).ExecuteResultAsync(Json(new
            {
                path = "existe.txt",
                operation = "create_file",
                content = "novo",
                overwrite = true
            }));

            Assert.True(result.Ok, result.Error);
            Assert.Equal("novo", await File.ReadAllTextAsync(target));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteResultAsync_ReplaceLines_SubstituiFaixaEMostraDiff()
    {
        var root = Directory.CreateTempSubdirectory("pi-edit-").FullName;

        try
        {
            var target = Path.Combine(root, "linhas.txt");
            await File.WriteAllLinesAsync(target, ["a", "b", "c", "d"]);

            var result = await CreateSut(root).ExecuteResultAsync(Json(new
            {
                path = "linhas.txt",
                operation = "replace_lines",
                start_line = 2,
                end_line = 3,
                new_content = "X\nY\nZ"
            }));

            Assert.True(result.Ok, result.Error);
            Assert.Equal(["a", "X", "Y", "Z", "d"], await File.ReadAllLinesAsync(target));
            Assert.Contains("@@", result.Output, StringComparison.Ordinal);
            Assert.Contains("-b", result.Output, StringComparison.Ordinal);
            Assert.Contains("-c", result.Output, StringComparison.Ordinal);
            Assert.Contains("+X", result.Output, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteResultAsync_ReplaceLinesComConteudoVazio_RemoveFaixa()
    {
        var root = Directory.CreateTempSubdirectory("pi-edit-").FullName;

        try
        {
            var target = Path.Combine(root, "linhas.txt");
            await File.WriteAllLinesAsync(target, ["a", "b", "c"]);

            var result = await CreateSut(root).ExecuteResultAsync(Json(new
            {
                path = "linhas.txt",
                operation = "replace_lines",
                start_line = 2,
                end_line = 2,
                new_content = ""
            }));

            Assert.True(result.Ok, result.Error);
            Assert.Equal(["a", "c"], await File.ReadAllLinesAsync(target));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(3, 2)]
    [InlineData(2, 9)]
    public async Task ExecuteResultAsync_ReplaceLinesComFaixaInvalida_RetornaErro(int start, int end)
    {
        var root = Directory.CreateTempSubdirectory("pi-edit-").FullName;

        try
        {
            var target = Path.Combine(root, "linhas.txt");
            await File.WriteAllLinesAsync(target, ["a", "b", "c"]);

            var result = await CreateSut(root).ExecuteResultAsync(Json(new
            {
                path = "linhas.txt",
                operation = "replace_lines",
                start_line = start,
                end_line = end,
                new_content = "x"
            }));

            Assert.False(result.Ok);
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
            Assert.Equal(["a", "b", "c"], await File.ReadAllLinesAsync(target));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteResultAsync_ReplaceLinesEmArquivoVazio_RetornaErro()
    {
        var root = Directory.CreateTempSubdirectory("pi-edit-").FullName;

        try
        {
            var target = Path.Combine(root, "vazio.txt");
            await File.WriteAllTextAsync(target, string.Empty);

            var result = await CreateSut(root).ExecuteResultAsync(Json(new
            {
                path = "vazio.txt",
                operation = "replace_lines",
                start_line = 1,
                end_line = 1,
                new_content = "x"
            }));

            Assert.False(result.Ok);
            Assert.Contains("vazio", result.Error, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_FormatoLegado_SubstituiTextoEIncluiDiff()
    {
        var root = Directory.CreateTempSubdirectory("pi-edit-").FullName;

        try
        {
            var target = Path.Combine(root, "a.txt");
            await File.WriteAllTextAsync(target, "oi mundo");

            var output = await CreateSut(root).ExecuteAsync("edite a.txt | oi => ola");

            Assert.Equal("ola mundo", await File.ReadAllTextAsync(target));
            Assert.Contains("--- a/a.txt", output, StringComparison.Ordinal);
            Assert.Contains("-oi mundo", output, StringComparison.Ordinal);
            Assert.Contains("+ola mundo", output, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteResultAsync_PathTraversal_BloqueiaCriacaoForaDoWorkspace()
    {
        var parent = Directory.CreateTempSubdirectory("pi-edit-outside-").FullName;
        var root = Path.Combine(parent, "workspace");
        Directory.CreateDirectory(root);

        try
        {
            var result = await CreateSut(root).ExecuteResultAsync(Json(new
            {
                path = "../fora.txt",
                operation = "create_file",
                content = "segredo"
            }));

            Assert.False(result.Ok);
            Assert.Contains("fora do workspace", result.Error, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(Path.Combine(parent, "fora.txt")));
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }

    private static EditFileTool CreateSut(string root) => new(new WorkspaceSandboxPolicy(root));

    private static string Json(object arguments) => JsonSerializer.Serialize(arguments);
}
