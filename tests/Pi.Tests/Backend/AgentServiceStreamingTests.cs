using System.Runtime.CompilerServices;
using NSubstitute;
using Pi.Backend.AI;
using Pi.Backend.Chat;
using Pi.Backend.Tools;
using Pi.Options;
using Xunit;

namespace Pi.Tests;

public class AgentServiceStreamingTests
{
    [Fact]
    public async Task AskStreamAsync_TransmiteTokensEContagemDeContexto()
    {
        var client = new FakeStreamingClient();
        var tool = Substitute.For<ITool>();
        tool.CanHandle(Arg.Any<string>()).Returns(false);
        var conversation = new Conversation();
        var sink = new RecordingSink();
        var sut = CreateSut(client, tool, conversation);

        var reply = await sut.AskStreamAsync("olá", sink);

        Assert.Equal("Olá mundo", reply);
        Assert.Equal(["Olá", " mundo"], sink.Tokens);
        Assert.Contains(sink.ContextStats, item => item.Limit == 32_768);
        Assert.Equal(2, conversation.Messages.Count);
    }

    [Fact]
    public async Task AskStreamAsync_QuandoFerramentaResolve_EmiteEventosSemChamarModelo()
    {
        var client = new FakeStreamingClient();
        var tool = Substitute.For<ITool>();
        tool.Name.Returns(ToolNames.SearchFiles);
        tool.CanHandle("procure relatório").Returns(true);
        tool.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("arquivo.txt");
        var conversation = new Conversation();
        var sink = new RecordingSink();
        var sut = CreateSut(client, tool, conversation);

        var reply = await sut.AskStreamAsync("procure relatório", sink);

        Assert.Equal("arquivo.txt", reply);
        Assert.Equal([ToolNames.SearchFiles], sink.ToolCalls.Select(item => item.Name));
        Assert.Single(sink.ToolResults);
        Assert.True(sink.ToolResults[0].Ok);
        Assert.Equal(["arquivo.txt"], sink.Tokens);
        Assert.Empty(conversation.Messages);
        Assert.Equal(string.Empty, client.LastPrompt);
    }

    private static AgentService CreateSut(
        ILLMClient client,
        ITool tool,
        Conversation conversation) =>
        new(
            client,
            new ToolRouter([tool]),
            conversation,
            Microsoft.Extensions.Options.Options.Create(new AssistantOptions()));

    private sealed class FakeStreamingClient : IStreamingLLMClient
    {
        public string ModelName => "fake";

        public string LastPrompt { get; private set; } = string.Empty;

        public Task<string> SendAsync(string prompt, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("O caminho de streaming não deve usar SendAsync.");

        public async IAsyncEnumerable<OllamaStreamChunk> StreamAsync(
            string prompt,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            LastPrompt = prompt;
            await Task.Yield();

            yield return new OllamaStreamChunk("Olá", false);
            yield return new OllamaStreamChunk(" mundo", false);
            yield return new OllamaStreamChunk(string.Empty, true, 10, 3);
        }
    }

    private sealed class RecordingSink : IAgentEventSink
    {
        public List<string> Tokens { get; } = [];

        public List<(string Name, string Input)> ToolCalls { get; } = [];

        public List<(string Name, bool Ok, string Text)> ToolResults { get; } = [];

        public List<(int Used, int Limit)> ContextStats { get; } = [];

        public Task ThoughtAsync(string text, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ToolCallAsync(string name, string input, CancellationToken cancellationToken = default)
        {
            ToolCalls.Add((name, input));
            return Task.CompletedTask;
        }

        public Task ToolResultAsync(string name, bool ok, string text, CancellationToken cancellationToken = default)
        {
            ToolResults.Add((name, ok, text));
            return Task.CompletedTask;
        }

        public Task TokenAsync(string text, CancellationToken cancellationToken = default)
        {
            Tokens.Add(text);
            return Task.CompletedTask;
        }

        public Task ContextStatsAsync(int used, int limit, CancellationToken cancellationToken = default)
        {
            ContextStats.Add((used, limit));
            return Task.CompletedTask;
        }
    }
}
