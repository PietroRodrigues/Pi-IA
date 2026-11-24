// Coordenador de ferramentas: publica request/result, respeita negação e timeout.
using System.Text.Json;
using Pi.Contracts.Protocol;
using Pi.Core.Abstractions;
using Pi.Core.Engine;
using Pi.Core.Policies;
using Xunit;

namespace Pi.Core.Tests;

public class ToolExecutionCoordinatorTests
{
    [Fact]
    public async Task TryExecuteAsync_QuandoNaoHáFerramenta_RetornaNull()
    {
        var sink = new RecordingSink();
        var sut = new ToolExecutionCoordinator([], new AllowPolicy(), sink);

        var result = await sut.TryExecuteAsync("texto qualquer", "r1");

        Assert.Null(result);
        Assert.Empty(sink.Items);
    }

    [Fact]
    public async Task TryExecuteAsync_PublicaRequestEResultado()
    {
        var tool = new FakeTool("search", ToolRisk.ReadOnly, "resultado");
        var sink = new RecordingSink();
        var sut = new ToolExecutionCoordinator([tool], new AllowPolicy(), sink);

        var result = await sut.TryExecuteAsync("procure algo", "r1");

        Assert.Equal("resultado", result);
        Assert.Equal([EventTypes.ToolRequest, EventTypes.ToolResult], sink.Items.Select(item => item.Type));
        Assert.All(sink.Items, item => Assert.Equal("r1", item.CorrelationId));
    }

    [Fact]
    public async Task TryExecuteAsync_QuandoNegada_NaoExecutaFerramenta()
    {
        var tool = new FakeTool("edit", ToolRisk.Write, "não deveria executar");
        var sink = new RecordingSink();
        var sut = new ToolExecutionCoordinator([tool], new DenyPolicy(), sink);

        var result = await sut.TryExecuteAsync("edite arquivo.txt", "r1");

        Assert.Contains("negada", result, StringComparison.OrdinalIgnoreCase);
        Assert.False(tool.Executed);
        Assert.Equal(EventTypes.ToolResult, sink.Items.Last().Type);

        using var document = JsonDocument.Parse(sink.Items.Last().Json);
        Assert.False(document.RootElement.GetProperty("ok").GetBoolean());
    }

    [Fact]
    public async Task ExecuteAsync_CaminhoForaDoSandbox_NaoExecutaFerramenta()
    {
        var parent = Directory.CreateTempSubdirectory("pi-coord-outside-").FullName;
        var root = Path.Combine(parent, "workspace");
        Directory.CreateDirectory(root);

        try
        {
            var tool = new FakeTool("read_file", ToolRisk.ReadOnly, "não deveria executar");
            var sink = new RecordingSink();
            var sandbox = new WorkspaceSandboxPolicy(root);
            var sut = new ToolExecutionCoordinator([tool], new AllowPolicy(), sink, sandbox: sandbox);

            var result = await sut.ExecuteAsync(
                "read_file",
                """{"path":"../fora.txt"}""",
                "r1");

            Assert.False(result.Ok);
            Assert.Contains("fora do workspace", result.Error, StringComparison.OrdinalIgnoreCase);
            Assert.False(tool.Executed);
            Assert.Equal(EventTypes.ToolResult, sink.Items.Last().Type);

            using var document = JsonDocument.Parse(sink.Items.Last().Json);
            Assert.False(document.RootElement.GetProperty("ok").GetBoolean());
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }

    [Fact]
    public void Decide_ResolveAprovacaoPendente()
    {
        var sink = new RecordingSink();
        var sut = new ToolExecutionCoordinator([], new AllowPolicy(), sink);

        // Não lança quando não há pendência — a TUI pode responder após um timeout.
        sut.Decide("inexistente", true);
    }

    private sealed class FakeTool(string name, ToolRisk risk, string answer) : IAgentTool
    {
        public string Name { get; } = name;

        public ToolRisk Risk { get; } = risk;

        public bool Executed { get; private set; }

        public bool CanHandle(string input) => true;

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

    private sealed class DenyPolicy : IPermissionPolicy
    {
        public PermissionDecision Decide(IAgentTool tool, string input) => PermissionDecision.Deny;
    }

    private sealed class RecordingSink : IEventSink
    {
        public List<(string Type, string CorrelationId, string Json)> Items { get; } = [];

        public Task PublishAsync<T>(
            string type,
            T payload,
            string correlationId = "",
            CancellationToken cancellationToken = default)
        {
            Items.Add((type, correlationId, JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web))));
            return Task.CompletedTask;
        }
    }
}
