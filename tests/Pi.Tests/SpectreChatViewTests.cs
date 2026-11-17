using System.Text.RegularExpressions;
using Pi.UI;
using Spectre.Console;
using Xunit;

namespace Pi.Tests;

/// <summary>
/// Renderiza em StringWriter com ANSI desligado e normaliza eventuais escapes
/// residuais, afetando o conteúdo sem depender de terminal nem de códigos de cor.
/// A ausência de ANSI na saída redirecionada real é verificada no smoke manual.
/// </summary>
public sealed class SpectreChatViewTests
{
    [Fact]
    public void ShowWelcome_MostraModeloEAgente()
    {
        using var saida = new StringWriter();
        new SpectreChatView(CriarConsole(saida)).ShowWelcome("qwen2.5:3b", "geral");

        var texto = SemAnsi(saida.ToString());
        Assert.Contains("qwen2.5:3b", texto);
        Assert.Contains("geral", texto);
    }

    [Fact]
    public void ShowReply_MostraRespostaEscapandoMarkup()
    {
        using var saida = new StringWriter();

        new SpectreChatView(CriarConsole(saida)).ShowReply("use [colchetes]", "geral");

        var texto = SemAnsi(saida.ToString());
        Assert.Contains("use [colchetes]", texto);
        Assert.Contains("Pi", texto);
    }

    [Fact]
    public async Task ReadInputAsync_LeLinhaEDevolveNullNoFim()
    {
        using var saida = new StringWriter();
        using var entrada = new StringReader("olá\n");
        var view = new SpectreChatView(CriarConsole(saida), entrada);

        Assert.Equal("olá", await view.ReadInputAsync());
        Assert.Null(await view.ReadInputAsync());
    }

    private static IAnsiConsole CriarConsole(StringWriter saida) =>
        AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(saida)
        });

    private static string SemAnsi(string texto) =>
        Regex.Replace(texto, @"\x1B\[[0-9;?]*[ -/]*[@-~]", string.Empty);
}
