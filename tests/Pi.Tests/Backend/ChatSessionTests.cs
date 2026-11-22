using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Pi.Backend.AI;
using Pi.Backend.Chat;
using Pi.Backend.Tools;
using Pi.Options;
using Xunit;

namespace Pi.Tests;

/// <summary>
/// Testa a lógica da conversa sem terminal: a view é um contrato falso.
/// </summary>
public sealed class ChatSessionTests
{
    [Fact]
    public async Task RunAsync_ProcessaMensagemEEncerraNoEof()
    {
        var llm = CriarLlm("resposta");
        var view = CriarView("olá", null);
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        var sessao = CriarSessao(view, llm, lifetime);

        await sessao.RunAsync(CancellationToken.None);

        view.Received(1).ShowWelcome("modelo-teste", "geral");
        view.Received(1).BeginThinking("geral", "modelo-teste");
        view.Received(1).ShowReply("resposta", "geral");
        lifetime.Received(1).StopApplication();
    }

    [Fact]
    public async Task RunAsync_ComandoSair_EncerraSemChamarOLlm()
    {
        var llm = CriarLlm("resposta");
        var view = CriarView("SAIR", null);
        var lifetime = Substitute.For<IHostApplicationLifetime>();

        await CriarSessao(view, llm, lifetime).RunAsync(CancellationToken.None);

        await llm.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        view.DidNotReceive().ShowReply(Arg.Any<string>(), Arg.Any<string>());
        lifetime.Received(1).StopApplication();
    }

    [Fact]
    public async Task RunAsync_QuandoLlmFalha_MostraErroENaoDerrubaOChat()
    {
        var llm = CriarLlm("resposta");
        llm.SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<string>(_ => throw new InvalidOperationException("serviço indisponível"));

        var view = CriarView("olá", "sair", null);
        var sessao = CriarSessao(view, llm, Substitute.For<IHostApplicationLifetime>());

        await sessao.RunAsync(CancellationToken.None);

        view.Received(1).ShowError("serviço indisponível");
        view.DidNotReceive().ShowReply(Arg.Any<string>(), Arg.Any<string>());
    }

    private static ChatSession CriarSessao(
        IChatView view,
        ILLMClient llm,
        IHostApplicationLifetime lifetime)
    {
        var tool = Substitute.For<ITool>();
        tool.CanHandle(Arg.Any<string>()).Returns(false);

        var agente = new AgentService(
            llm,
            new ToolRouter([tool]),
            new Conversation(),
            Microsoft.Extensions.Options.Options.Create(new AssistantOptions()));

        return new ChatSession(
            view,
            new ProcessMessage(agente),
            llm,
            Microsoft.Extensions.Options.Options.Create(new AssistantOptions()),
            lifetime,
            NullLogger<ChatSession>.Instance);
    }

    private static ILLMClient CriarLlm(string resposta)
    {
        var llm = Substitute.For<ILLMClient>();
        llm.ModelName.Returns("modelo-teste");
        llm.SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(resposta));
        return llm;
    }

    private static IChatView CriarView(params string?[] entradas)
    {
        var fila = new Queue<string?>(entradas);
        var view = Substitute.For<IChatView>();
        view.ReadInputAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(fila.Count > 0 ? fila.Dequeue() : null));
        view.BeginThinking(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Substitute.For<IAsyncDisposable>());
        return view;
    }
}
