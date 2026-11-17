using Microsoft.Extensions.Options;
using NSubstitute;
using Pi.Application.Abstractions.AI;
using Pi.Application.Abstractions.Tools;
using Pi.Application.Options;
using Pi.Application.Services;
using Pi.Application.Tools;
using Pi.Domain.Conversation;
using Xunit;
using ConversationAggregate = Pi.Domain.Conversation.Conversation;

namespace Pi.Application.Tests;

public class AgentServiceTests
{
    [Fact]
    public async Task AskAsync_WhenToolHandlesInput_DoesNotCallTheLlm()
    {
        var llm = Substitute.For<ILLMClient>();
        var tool = Substitute.For<ITool>();
        tool.CanHandle("procure relatório").Returns(true);
        tool.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("arquivo.txt");

        var conversation = new ConversationAggregate();
        var sut = new AgentService(
            llm,
            new ToolRouter([tool]),
            conversation,
            Microsoft.Extensions.Options.Options.Create(new AssistantOptions()));

        var result = await sut.AskAsync("procure relatório");

        Assert.Equal("arquivo.txt", result);
        await llm.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.Empty(conversation.Messages);
    }

    [Fact]
    public async Task AskAsync_WhenNoToolHandlesInput_CallsLlmAndRecordsHistory()
    {
        var llm = Substitute.For<ILLMClient>();
        llm.SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("resposta");
        var tool = Substitute.For<ITool>();
        tool.CanHandle(Arg.Any<string>()).Returns(false);

        var conversation = new ConversationAggregate();
        var sut = new AgentService(
            llm,
            new ToolRouter([tool]),
            conversation,
            Microsoft.Extensions.Options.Options.Create(new AssistantOptions()));

        var result = await sut.AskAsync("olá");

        Assert.Equal("resposta", result);
        Assert.Equal(2, conversation.Messages.Count);
        Assert.Equal(MessageRole.User, conversation.Messages[0].Role);
        Assert.Equal(MessageRole.Assistant, conversation.Messages[1].Role);
    }

    [Fact]
    public async Task AskAsync_IncludesHistoryInThePrompt()
    {
        var llm = Substitute.For<ILLMClient>();
        llm.SendAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("primeira resposta", "segunda resposta");
        var tool = Substitute.For<ITool>();
        tool.CanHandle(Arg.Any<string>()).Returns(false);

        var conversation = new ConversationAggregate();
        var sut = new AgentService(
            llm,
            new ToolRouter([tool]),
            conversation,
            Microsoft.Extensions.Options.Options.Create(new AssistantOptions { SystemPrompt = "VOCÊ É PI" }));

        await sut.AskAsync("primeira pergunta");
        await sut.AskAsync("segunda pergunta");

        var prompt = (string)llm.ReceivedCalls()
            .Last(call => call.GetMethodInfo().Name == nameof(ILLMClient.SendAsync))
            .GetArguments()[0]!;

        Assert.Contains("VOCÊ É PI", prompt);
        Assert.Contains("[User] primeira pergunta", prompt);
        Assert.Contains("[Assistant] primeira resposta", prompt);
        Assert.Contains("<user_input>", prompt);
    }
}
