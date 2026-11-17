using NSubstitute;
using Pi.Abstracts.AI;
using Pi.Options;
using Pi.Workers;
using Xunit;

namespace Pi.Tests;

public sealed class ChatPresenterTests
{
    [Fact]
    public void Welcome_MostraModeloEAgente()
    {
        var texto = Criar().Welcome();

        Assert.Contains("Olá! Sou a Pi ✨", texto);
        Assert.Contains("Modelo: qwen2.5:3b · Agente: geral", texto);
        Assert.Contains("Digite sua mensagem", texto);
    }

    [Fact]
    public void Thinking_MostraModeloEAgente()
    {
        var texto = Criar().Thinking();

        Assert.Contains("💭 Pi está pensando...", texto);
        Assert.Contains("geral", texto);
        Assert.Contains("qwen2.5:3b", texto);
    }

    [Fact]
    public void Reply_MantemOPrefixoPi()
    {
        Assert.Equal("\nPi: olá", Criar().Reply("olá"));
    }

    private static ChatPresenter Criar()
    {
        var llm = Substitute.For<ILLMClient>();
        llm.ModelName.Returns("qwen2.5:3b");

        return new ChatPresenter(
            llm,
            Microsoft.Extensions.Options.Options.Create(new AssistantOptions { AgentName = "geral" }));
    }
}
