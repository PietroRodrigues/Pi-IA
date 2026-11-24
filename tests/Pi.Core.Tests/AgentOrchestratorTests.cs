// Orquestração de um turno: streaming, persistência, métricas e cancelamento.
using System.Runtime.CompilerServices;
using System.Text.Json;
using Pi.Contracts.Models;
using Pi.Contracts.Protocol;
using Pi.Core.Abstractions;
using Pi.Core.Engine;
using Xunit;

namespace Pi.Core.Tests;

public class AgentOrchestratorTests
{
    [Fact]
    public async Task RunTurnAsync_TransmiteTokensEMetricasEPersisteHistorico()
    {
        var settings = new AgentSettings("sistema", 1000, 10);
        var provider = new FakeProvider(
            new LlmChunk("Olá", false),
            new LlmChunk(" mundo", true, 12, 3));
        var sink = new RecordingSink();
        var sessions = new MemorySessions();
        var sut = CreateSut(settings, provider, sink, sessions);

        await sut.RunTurnAsync("oi", "r1");

        var deltas = sink.Items
            .Where(item => item.Type == EventTypes.StreamDelta && !Payload(item).GetProperty("final").GetBoolean())
            .Select(item => Payload(item).GetProperty("text").GetString() ?? string.Empty)
            .ToArray();

        Assert.Equal(["Olá", " mundo"], deltas);
        Assert.Contains(sink.Items, item => item.Type == EventTypes.ContextMetrics);
        Assert.Contains(sink.Items, item =>
            item.Type == EventTypes.StreamDelta && Payload(item).GetProperty("final").GetBoolean());

        var saved = await sessions.GetAsync("s1");
        Assert.NotNull(saved);
        Assert.Equal(2, saved!.Messages.Count);
        Assert.Equal("oi", saved.Messages[0].Text);
        Assert.Equal("Olá mundo", saved.Messages[1].Text);
    }

    [Fact]
    public async Task InterruptAsync_CancelaTurnoEEmiteFinal()
    {
        var settings = new AgentSettings("sistema", 1000, 10);
        var sink = new RecordingSink();
        var sut = CreateSut(settings, new BlockingProvider(), sink, new MemorySessions());

        var turn = sut.RunTurnAsync("oi", "r1");

        await WaitUntilAsync(
            () => sink.Items.Any(item => item.Type == EventTypes.StreamDelta),
            TimeSpan.FromSeconds(3));

        await sut.InterruptAsync("teste");
        await turn.WaitAsync(TimeSpan.FromSeconds(3));

        Assert.Contains(sink.Items, item =>
            item.Type == EventTypes.StreamDelta && Payload(item).GetProperty("final").GetBoolean());
    }

    [Fact]
    public async Task RunTurnAsync_ComPromptVazio_EmiteErrorEFinal()
    {
        var settings = new AgentSettings("sistema", 1000, 10);
        var sink = new RecordingSink();
        var sut = CreateSut(settings, new FakeProvider(), sink, new MemorySessions());

        await sut.RunTurnAsync("   ", "r1");

        Assert.Contains(sink.Items, item => item.Type == EventTypes.ErrorEvent);
        Assert.Contains(sink.Items, item =>
            item.Type == EventTypes.StreamDelta && Payload(item).GetProperty("final").GetBoolean());
    }

    [Fact]
    public async Task RunTurnAsync_ComToolCall_ExecutaEAnexaMensagemTool()
    {
        var settings = new AgentSettings("sistema", 1000, 10);
        var provider = new ToolCallingProvider();
        var tool = new StaticTool("read_file", "conteudo do arquivo");
        var sink = new RecordingSink();
        var sessions = new MemorySessions();
        var sut = CreateSut(settings, provider, sink, sessions, [tool]);

        await sut.RunTurnAsync("leia a.txt", "r1");

        Assert.True(tool.Executed);
        Assert.Contains(sink.Items, item => item.Type == EventTypes.ToolRequest);
        Assert.Contains(sink.Items, item => item.Type == EventTypes.ToolResult);

        var saved = await sessions.GetAsync("s1");
        Assert.NotNull(saved);
        Assert.Contains(saved!.Messages, message => message.Role == "tool");
        Assert.Contains(saved.Messages, message =>
            message.Role == "assistant" && message.Text == "Resposta final");
    }

    private static AgentOrchestrator CreateSut(
        AgentSettings settings,
        ILlmProvider provider,
        RecordingSink sink,
        ISessionRepository sessions,
        IEnumerable<IAgentTool>? catalog = null)
    {
        var session = SessionSnapshot.Create("s1", "fake", "geral", "C:/temp", "main", settings.ContextWindowTokens);
        var tools = catalog?.ToList() ?? [];

        return new AgentOrchestrator(
            provider,
            new ContextBudgetManager(settings),
            new ToolExecutionCoordinator(tools, new AllowPolicy(), sink),
            sessions,
            sink,
            settings,
            session,
            tools);
    }

    private static JsonElement Payload(RecordingSink.Item item)
    {
        using var document = JsonDocument.Parse(item.Json);
        return document.RootElement.Clone();
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        using var limit = new CancellationTokenSource(timeout);

        while (!condition())
        {
            await Task.Delay(20, limit.Token);
        }
    }

    private sealed class FakeProvider(params LlmChunk[] chunks) : ILlmProvider
    {
        public string ModelName => "fake";

        public Task<string> SendAsync(string prompt, CancellationToken cancellationToken = default) =>
            Task.FromResult(string.Concat(chunks.Select(chunk => chunk.Text)));

        public async IAsyncEnumerable<LlmChunk> StreamAsync(
            string prompt,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var chunk in chunks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return chunk;
            }

            await Task.CompletedTask;
        }
    }

    private sealed class BlockingProvider : ILlmProvider
    {
        public string ModelName => "blocking";

        public Task<string> SendAsync(string prompt, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public async IAsyncEnumerable<LlmChunk> StreamAsync(
            string prompt,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new LlmChunk("parcial", false);
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
    }

    private sealed class ToolCallingProvider : ILlmProvider
    {
        private bool _toolCallSent;

        public string ModelName => "tool-calling";

        public Task<string> SendAsync(string prompt, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<LlmChunk> StreamAsync(
            string prompt,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public async IAsyncEnumerable<LlmResponseChunk> StreamAsync(
            IReadOnlyList<LlmMessage> messages,
            IReadOnlyList<LlmToolDefinition> tools,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (!_toolCallSent)
            {
                _toolCallSent = true;
                yield return new LlmResponseChunk(
                    string.Empty,
                    false,
                    [new LlmToolCall("read_file", """{"path":"a.txt"}""", "call-1")]);
            }
            else
            {
                yield return new LlmResponseChunk("Resposta final", true, []);
            }

            await Task.CompletedTask;
        }
    }

    private sealed class StaticTool(string name, string answer) : IAgentTool
    {
        public string Name { get; } = name;

        public ToolRisk Risk => ToolRisk.ReadOnly;

        public bool Executed { get; private set; }

        public bool CanHandle(string input) => false;

        public Task<string> ExecuteAsync(string input, CancellationToken cancellationToken = default)
        {
            Executed = true;
            return Task.FromResult(answer);
        }
    }

    private sealed class AllowPolicy : IPermissionPolicy
    {
        public PermissionDecision Decide(IAgentTool tool, string input) => PermissionDecision.Allow;
    }

    private sealed class MemorySessions : ISessionRepository
    {
        private readonly Dictionary<string, SessionSnapshot> _items = new(StringComparer.OrdinalIgnoreCase);

        public Task<SessionSnapshot?> GetAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.TryGetValue(id, out var snapshot) ? Clone(snapshot) : null);

        public Task SaveAsync(SessionSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            _items[snapshot.Id] = Clone(snapshot);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>([.. _items.Keys.OrderBy(key => key)]);

        private static SessionSnapshot Clone(SessionSnapshot snapshot) =>
            new(
                snapshot.Id,
                snapshot.Model,
                snapshot.Agent,
                snapshot.Cwd,
                snapshot.Branch,
                [.. snapshot.Messages],
                snapshot.Context,
                snapshot.UpdatedAt);
    }

    private sealed class RecordingSink : IEventSink
    {
        public List<Item> Items { get; } = [];

        public Task PublishAsync<T>(
            string type,
            T payload,
            string correlationId = "",
            CancellationToken cancellationToken = default)
        {
            Items.Add(new Item(
                type,
                correlationId,
                JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web))));
            return Task.CompletedTask;
        }

        public sealed record Item(string Type, string CorrelationId, string Json);
    }
}
