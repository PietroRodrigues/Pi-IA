using Microsoft.Extensions.DependencyInjection;
using Pi;
using Pi.Backend.Chat;
using Pi.Frontend;
using Xunit;

namespace Pi.Tests;

public sealed class ConsoleChatViewTests
{
    [Fact]
    public void ShowWelcome_MostraModeloEAgente()
    {
        using var saida = new StringWriter();
        var view = new ConsoleChatView(saida, new StringReader(string.Empty));

        view.ShowWelcome("qwen3.8-2b-local", "geral");

        Assert.Contains("Modelo: qwen3.8-2b-local · Agente: geral", saida.ToString());
    }

    [Fact]
    public void ShowReply_EscreveRespostaLiteralSemInterpretarMarkup()
    {
        using var saida = new StringWriter();
        var view = new ConsoleChatView(saida, new StringReader(string.Empty));

        view.ShowReply("use [colchetes]", "geral");

        Assert.Contains("use [colchetes]", saida.ToString());
    }

    [Fact]
    public void ShowError_MostraMensagemSimples()
    {
        using var saida = new StringWriter();
        var view = new ConsoleChatView(saida, new StringReader(string.Empty));

        view.ShowError("modelo indisponível");

        Assert.Contains("erro: modelo indisponível", saida.ToString());
    }

    [Fact]
    public async Task ReadInputAsync_LeLinhaEDevolveNullNoFim()
    {
        using var saida = new StringWriter();
        using var entrada = new StringReader("olá, tudo bem?\n");
        var view = new ConsoleChatView(saida, entrada);

        Assert.Equal("olá, tudo bem?", await view.ReadInputAsync());
        Assert.Null(await view.ReadInputAsync());
        Assert.Contains("você › ", saida.ToString());
    }

    [Fact]
    public async Task BeginThinking_EscreveStatusESaiSemEfeito()
    {
        using var saida = new StringWriter();
        var view = new ConsoleChatView(saida, new StringReader(string.Empty));

        await using (view.BeginThinking("geral", "qwen3.8-2b-local"))
        {
            Assert.Contains("pensando · geral · qwen3.8-2b-local", saida.ToString());
        }
    }

    [Fact]
    public void AddFrontend_RegistraSempreAViewTextual()
    {
        var servicos = new ServiceCollection();
        servicos.AddFrontend();
        using var provedor = servicos.BuildServiceProvider();

        Assert.IsType<ConsoleChatView>(provedor.GetRequiredService<IChatView>());
    }
}
