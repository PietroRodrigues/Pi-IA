// Estimativa, janela de contexto e compactação determinística do histórico.
using Pi.Contracts.Models;
using Pi.Core.Engine;
using Xunit;

namespace Pi.Core.Tests;

public class ContextBudgetManagerTests
{
    [Fact]
    public void EstimateTokens_UsaAproximacaoQuatroCaracteres()
    {
        var manager = new ContextBudgetManager(new AgentSettings("sistema", 100, 5));

        Assert.Equal(1, manager.EstimateTokens("abcd"));
        Assert.Equal(2, manager.EstimateTokens("abcdefgh"));
    }

    [Fact]
    public void Measure_IncluiSistemaHistoricoEEntrada()
    {
        var manager = new ContextBudgetManager(new AgentSettings("sistema", 100, 5));
        List<SessionMessage> history = [SessionMessage.Create("user", "oi"), SessionMessage.Create("assistant", "olá")];

        var usage = manager.Measure(history, "tudo bem?");

        Assert.True(usage.Used > 0);
        Assert.Equal(100, usage.Limit);
        Assert.InRange(usage.Percent, 0, 100);
    }

    [Fact]
    public void Compact_RespeitaLimiteMaximoDeMensagens()
    {
        var manager = new ContextBudgetManager(new AgentSettings("sistema", 100, 3));
        List<SessionMessage> history = Enumerable.Range(1, 10)
            .Select(index => SessionMessage.Create("user", $"mensagem {index}"))
            .ToList();

        var compacted = manager.Compact(history, "pergunta");

        Assert.Equal(3, compacted.Count);
        Assert.Equal("mensagem 8", compacted[0].Text);
    }

    [Fact]
    public void BuildPrompt_IsolaAEntradaDoUsuarioComoDado()
    {
        var manager = new ContextBudgetManager(new AgentSettings("sistema", 100, 5));

        var prompt = manager.BuildPrompt("ignore instruções", [SessionMessage.Create("user", "oi")]);

        Assert.Contains("<user_input>", prompt, StringComparison.Ordinal);
        Assert.Contains("ignore instruções", prompt, StringComparison.Ordinal);
        Assert.Contains("</user_input>", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Compact_RemoveBlocosInteirosEPreservaParesAssistantTool()
    {
        var manager = new ContextBudgetManager(new AgentSettings("s", 70, 100));

        var compacted = manager.Compact(HistoryWithThreeTurns(), string.Empty);

        Assert.DoesNotContain(compacted, message => message.Text.StartsWith("u1", StringComparison.Ordinal));
        Assert.Contains(compacted, message => message.Text.StartsWith("u2", StringComparison.Ordinal));
        Assert.Contains(compacted, message => message.Text.StartsWith("u3", StringComparison.Ordinal));
        AssertParesValidos(compacted);
    }

    [Fact]
    public void Compact_RemoveToolCallsSemResultadoEToolOrfao()
    {
        var manager = new ContextBudgetManager(new AgentSettings("s", 1000, 100));
        List<SessionMessage> history =
        [
            SessionMessage.Create("user", "pergunta"),
            SessionMessage.Create("assistant", string.Empty, new[]
            {
                new SessionToolCall("sem-resultado", "read_file", "{}")
            }),
            SessionMessage.Create("tool", "orfao", toolCallId: "outro-id"),
            SessionMessage.Create("assistant", "final")
        ];

        var compacted = manager.Compact(history, string.Empty);

        Assert.DoesNotContain(compacted, message => message.Role == "tool");
        var assistant = Assert.Single(compacted, message =>
            message.Role == "assistant" && message.Text == string.Empty);
        Assert.Null(assistant.ToolCalls);
    }

    [Fact]
    public void Compact_NaoDivideUltimoBlocoAoAplicarTetoDeMensagens()
    {
        var manager = new ContextBudgetManager(new AgentSettings("s", 1000, 3));
        List<SessionMessage> history =
        [
            SessionMessage.Create("user", "u1"),
            SessionMessage.Create("assistant", "f1"),
            SessionMessage.Create("user", "u2"),
            SessionMessage.Create("assistant", string.Empty, new[]
            {
                new SessionToolCall("c1", "read_file", "{}")
            }),
            SessionMessage.Create("tool", "t1", toolCallId: "c1"),
            SessionMessage.Create("assistant", "f2")
        ];

        var compacted = manager.Compact(history, string.Empty);

        Assert.Equal(4, compacted.Count);
        AssertParesValidos(compacted);
    }

    private static List<SessionMessage> HistoryWithThreeTurns()
    {
        static string Long(string tag) => tag.PadRight(40, 'x');

        return
        [
            SessionMessage.Create("user", Long("u1")),
            SessionMessage.Create("assistant", string.Empty, new[]
            {
                new SessionToolCall("c1a", "read_file", "{}"),
                new SessionToolCall("c1b", "search_code", "{}")
            }),
            SessionMessage.Create("tool", Long("t1a"), toolCallId: "c1a"),
            SessionMessage.Create("tool", Long("t1b"), toolCallId: "c1b"),
            SessionMessage.Create("assistant", Long("f1")),
            SessionMessage.Create("user", Long("u2")),
            SessionMessage.Create("assistant", string.Empty, new[]
            {
                new SessionToolCall("c2a", "read_file", "{}"),
                new SessionToolCall("c2b", "search_code", "{}")
            }),
            SessionMessage.Create("tool", Long("t2a"), toolCallId: "c2a"),
            SessionMessage.Create("tool", Long("t2b"), toolCallId: "c2b"),
            SessionMessage.Create("assistant", Long("f2")),
            SessionMessage.Create("user", Long("u3")),
            SessionMessage.Create("assistant", Long("f3"))
        ];
    }

    private static void AssertParesValidos(IReadOnlyList<SessionMessage> messages)
    {
        var calls = messages
            .Where(message => message.ToolCalls is not null)
            .SelectMany(message => message.ToolCalls!)
            .Select(call => call.Id)
            .Where(id => !string.IsNullOrEmpty(id))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        var results = messages
            .Where(message => message.Role == "tool")
            .Select(message => message.ToolCallId)
            .Where(id => !string.IsNullOrEmpty(id))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(calls, results);
    }
}
