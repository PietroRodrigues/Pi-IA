// Loop principal do agente: um turno por vez, cancelamento por correlationId,
// streaming de delta, tool calling e persistência do snapshot da sessão.
using System.Text;
using Pi.Contracts.Models;
using Pi.Contracts.Protocol;
using Pi.Core.Abstractions;

namespace Pi.Core.Engine;

public sealed class AgentOrchestrator(
    ILlmProvider provider,
    ContextBudgetManager budget,
    ToolExecutionCoordinator tools,
    ISessionRepository sessions,
    IEventSink sink,
    AgentSettings settings,
    SessionSnapshot initialSession,
    IEnumerable<IAgentTool>? toolsCatalog = null)
{
    private const int MaxToolIterations = 4;

    private readonly SemaphoreSlim _turnGate = new(1, 1);
    private readonly IReadOnlyList<IAgentTool> _tools = (toolsCatalog ?? []).ToList();
    private CancellationTokenSource? _activeTurn;
    private SessionSnapshot _session = initialSession;

    public string SessionId => _session.Id;

    public SessionSnapshot Snapshot => new(
        _session.Id,
        _session.Model,
        _session.Agent,
        _session.Cwd,
        _session.Branch,
        [.. _session.Messages],
        _session.Context,
        _session.UpdatedAt);

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await PublishSnapshotAsync(cancellationToken);
        await sink.PublishAsync(
            EventTypes.ContextMetrics,
            _session.Context,
            string.Empty,
            cancellationToken);
    }

    public async Task RunTurnAsync(
        string text,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            await PublishErrorAsync(correlationId, ErrorCodes.EmptyPrompt, "Prompt vazio.");
            await PublishFinalAsync(correlationId);
            return;
        }

        await _turnGate.WaitAsync(cancellationToken);

        using var turn = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _activeTurn = turn;

        try
        {
            await RunTurnCoreAsync(text, correlationId, turn.Token);

            _session = _session with
            {
                Context = budget.Measure(_session.Messages, string.Empty),
                UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
            };
            await PublishFinalAsync(correlationId);
            await PublishSnapshotAsync(CancellationToken.None);
            await sessions.SaveAsync(Snapshot, CancellationToken.None);
        }
        catch (OperationCanceledException) when (turn.IsCancellationRequested)
        {
            await PublishFinalAsync(correlationId);
            await PublishSnapshotAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            await PublishErrorAsync(correlationId, ErrorCodes.LlmFailure, ex.Message);
            await PublishFinalAsync(correlationId);
        }
        finally
        {
            _activeTurn = null;
            _turnGate.Release();
        }
    }

    public Task InterruptAsync(string? reason = null, CancellationToken cancellationToken = default)
    {
        var turn = _activeTurn;
        if (turn is not null && !turn.IsCancellationRequested)
        {
            turn.Cancel();
        }

        return Task.CompletedTask;
    }

    public async Task SwitchSessionAsync(
        string sessionId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        await InterruptAsync("session_switch", cancellationToken);
        await _turnGate.WaitAsync(cancellationToken);

        try
        {
            _session = await sessions.GetAsync(sessionId, cancellationToken)
                ?? SessionSnapshot.Create(
                    sessionId,
                    _session.Model,
                    _session.Agent,
                    _session.Cwd,
                    _session.Branch,
                    settings.ContextWindowTokens);

            await PublishSnapshotAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await PublishErrorAsync(correlationId, ErrorCodes.SessionFailure, ex.Message);
        }
        finally
        {
            _turnGate.Release();
        }
    }

    public async Task ClearAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        await InterruptAsync("clear", cancellationToken);
        await _turnGate.WaitAsync(cancellationToken);

        try
        {
            _session.Messages.Clear();
            _session = _session with
            {
                Context = new ContextUsage(0, settings.ContextWindowTokens, 0),
                UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
            };
            await PublishSnapshotAsync(cancellationToken);
            await sessions.SaveAsync(Snapshot, CancellationToken.None);
        }
        finally
        {
            _turnGate.Release();
        }
    }

    private async Task RunTurnCoreAsync(
        string text,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var history = budget.Compact(_session.Messages, text);
        var estimate = budget.Measure(history, text);
        var userText = text.Trim();

        var messages = new List<LlmMessage> { new("system", settings.SystemPrompt) };
        foreach (var message in history)
        {
            messages.Add(new LlmMessage(message.Role, message.Text));
        }

        messages.Add(new LlmMessage("user", userText));

        var working = new List<SessionMessage>(history)
        {
            SessionMessage.Create("user", userText)
        };

        var definitions = _tools.Select(ToDefinition).ToList();
        var response = new StringBuilder();
        var executedTools = 0;
        var iteration = 0;
        var reachedLimit = false;

        await sink.PublishAsync(
            EventTypes.ContextMetrics,
            estimate,
            correlationId,
            cancellationToken);

        while (iteration < MaxToolIterations)
        {
            iteration++;
            cancellationToken.ThrowIfCancellationRequested();

            var calls = new List<LlmToolCall>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            await foreach (var chunk in provider.StreamAsync(messages, definitions, cancellationToken))
            {
                if (chunk.Text.Length > 0)
                {
                    response.Append(chunk.Text);
                    await sink.PublishAsync(
                        EventTypes.StreamDelta,
                        new StreamDelta(chunk.Text),
                        correlationId,
                        cancellationToken);
                }

                if (chunk.PromptEvalCount is not null || chunk.EvalCount is not null)
                {
                    var used = (chunk.PromptEvalCount ?? 0) + (chunk.EvalCount ?? 0);
                    await sink.PublishAsync(
                        EventTypes.ContextMetrics,
                        new ContextUsage(used, estimate.Limit, Percent(used, estimate.Limit)),
                        correlationId,
                        cancellationToken);
                }

                foreach (var call in chunk.ToolCalls)
                {
                    var key = $"{call.Id ?? string.Empty}|{call.Name}|{call.ArgumentsJson}";
                    if (seen.Add(key))
                    {
                        calls.Add(call);
                    }
                }
            }

            if (calls.Count == 0)
            {
                // Fallback compatível com o atalho textual antigo, apenas se o modelo não respondeu.
                if (response.Length == 0 && executedTools == 0)
                {
                    var direct = await tools.TryExecuteAsync(text, correlationId, cancellationToken);
                    if (direct is not null)
                    {
                        response.Append(direct);
                        await sink.PublishAsync(
                            EventTypes.StreamDelta,
                            new StreamDelta(direct),
                            correlationId,
                            cancellationToken);
                    }
                }

                break;
            }

            messages.Add(new LlmMessage("assistant", string.Empty, ToolCalls: calls));

            foreach (var call in calls)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await tools.ExecuteAsync(
                    call.Name,
                    call.ArgumentsJson,
                    correlationId,
                    cancellationToken);

                var resultText = result.DisplayText;
                working.Add(SessionMessage.Create("tool", $"{call.Name}: {resultText}"));
                messages.Add(new LlmMessage("tool", resultText, call.Id));
                executedTools++;
            }

            if (iteration == MaxToolIterations)
            {
                reachedLimit = true;
            }
        }

        if (reachedLimit)
        {
            await PublishErrorAsync(
                correlationId,
                ErrorCodes.Internal,
                $"Limite de {MaxToolIterations} iterações de ferramentas atingido.");
        }

        if (response.Length == 0)
        {
            const string fallback = "Sem resposta do modelo.";
            response.Append(fallback);
            await sink.PublishAsync(
                EventTypes.StreamDelta,
                new StreamDelta(fallback),
                correlationId,
                cancellationToken);
        }

        working.Add(SessionMessage.Create("assistant", response.ToString()));
        _session.Messages.Clear();
        _session.Messages.AddRange(working);
    }

    private static LlmToolDefinition ToDefinition(IAgentTool tool) =>
        new(
            tool.Name,
            string.IsNullOrWhiteSpace(tool.Description) ? tool.Name : tool.Description,
            string.IsNullOrWhiteSpace(tool.ParametersSchema)
                ? "{\"type\":\"object\",\"properties\":{}}"
                : tool.ParametersSchema);

    private async Task PublishSnapshotAsync(CancellationToken cancellationToken) =>
        await sink.PublishAsync(
            EventTypes.SessionSnapshot,
            new SessionSnapshotEvent(Snapshot),
            string.Empty,
            cancellationToken);

    private async Task PublishFinalAsync(string correlationId) =>
        await sink.PublishAsync(
            EventTypes.StreamDelta,
            new StreamDelta(string.Empty, Final: true),
            correlationId,
            CancellationToken.None);

    private async Task PublishErrorAsync(string correlationId, string code, string message) =>
        await sink.PublishAsync(
            EventTypes.ErrorEvent,
            new ErrorInfo(code, message),
            correlationId,
            CancellationToken.None);

    private static double Percent(int used, int limit) =>
        limit <= 0 ? 0 : Math.Min(100, used * 100.0 / limit);
}
