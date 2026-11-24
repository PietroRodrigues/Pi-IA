// Validação, autorização, timeout e execução isolada de ferramentas.
// A decisão do usuário chega pelo comando tool_decision da TUI.
using System.Collections.Concurrent;
using System.Text.Json;
using Pi.Contracts.Protocol;
using Pi.Core.Abstractions;
using Pi.Core.Policies;

namespace Pi.Core.Engine;

public sealed class ToolExecutionCoordinator(
    IEnumerable<IAgentTool> tools,
    IPermissionPolicy policy,
    IEventSink sink,
    int decisionTimeoutSeconds = 30,
    int executionTimeoutSeconds = 30,
    WorkspaceSandboxPolicy? sandbox = null)
{
    private readonly IReadOnlyList<IAgentTool> _tools = tools.ToList();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _pending = new(StringComparer.Ordinal);

    /// <summary>Sobrecarga usada pelo DI do Host, com timeouts vindos das configurações.</summary>
    public ToolExecutionCoordinator(
        IEnumerable<IAgentTool> tools,
        IPermissionPolicy policy,
        IEventSink sink,
        ToolExecutionOptions options,
        WorkspaceSandboxPolicy? sandbox = null)
        : this(
            tools,
            policy,
            sink,
            options.DecisionTimeoutSeconds,
            options.ExecutionTimeoutSeconds,
            sandbox)
    {
    }

    /// <summary>Caminho legado: interpreta o texto do usuário como comando de ferramenta.</summary>
    public async Task<string?> TryExecuteAsync(
        string input,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var tool = _tools.FirstOrDefault(candidate => candidate.CanHandle(input));
        if (tool is null)
        {
            return null;
        }

        var result = await RunAsync(tool, input, correlationId, cancellationToken);
        return result.DisplayText;
    }

    /// <summary>Caminho de tool calling: executa uma ferramenta por nome com argumentos JSON.</summary>
    public Task<ToolExecutionResult> ExecuteAsync(
        string toolName,
        string argumentsJson,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var tool = _tools.FirstOrDefault(candidate =>
            candidate.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));

        if (tool is null)
        {
            return Task.FromResult(ToolExecutionResult.Failure($"Ferramenta desconhecida: {toolName}"));
        }

        return RunAsync(tool, argumentsJson, correlationId, cancellationToken);
    }

    public void Decide(string toolRequestId, bool approved)
    {
        if (_pending.TryGetValue(toolRequestId, out var source))
        {
            source.TrySetResult(approved);
        }
    }

    private async Task<ToolExecutionResult> RunAsync(
        IAgentTool tool,
        string input,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid().ToString("N");
        var decision = policy.Decide(tool, input);
        var requiresDecision = decision == PermissionDecision.Ask;

        await sink.PublishAsync(
            EventTypes.ToolRequest,
            new ToolRequest(requestId, tool.Name, input, tool.RiskLevel.ToString(), requiresDecision),
            correlationId,
            cancellationToken);

        if (sandbox is not null && TryReadPath(input, out var candidatePath))
        {
            if (!sandbox.IsAllowed(candidatePath))
            {
                return await FailAsync(
                    requestId,
                    tool,
                    $"Caminho fora do workspace: {candidatePath}",
                    correlationId,
                    cancellationToken);
            }
        }

        if (decision == PermissionDecision.Deny)
        {
            return await FailAsync(
                requestId,
                tool,
                $"Ferramenta '{tool.Name}' negada pela política de segurança.",
                correlationId,
                cancellationToken);
        }

        if (requiresDecision && !await WaitForDecisionAsync(requestId, cancellationToken))
        {
            return await FailAsync(
                requestId,
                tool,
                $"Ferramenta '{tool.Name}' não aprovada (negada ou sem resposta).",
                correlationId,
                CancellationToken.None);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, executionTimeoutSeconds)));

        try
        {
            var result = await tool.ExecuteResultAsync(input, timeout.Token);
            await PublishResultAsync(requestId, tool, result, correlationId, cancellationToken);
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return await FailAsync(
                requestId,
                tool,
                "A ferramenta excedeu o tempo limite.",
                correlationId,
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            return await FailAsync(requestId, tool, ex.Message, correlationId, CancellationToken.None);
        }
    }

    private async Task<ToolExecutionResult> FailAsync(
        string requestId,
        IAgentTool tool,
        string message,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var failure = ToolExecutionResult.Failure(message);
        await PublishResultAsync(requestId, tool, failure, correlationId, cancellationToken);
        return failure;
    }

    private Task PublishResultAsync(
        string requestId,
        IAgentTool tool,
        ToolExecutionResult result,
        string correlationId,
        CancellationToken cancellationToken) =>
        sink.PublishAsync(
            EventTypes.ToolResult,
            new ToolResult(requestId, tool.Name, result.Ok, result.DisplayText),
            correlationId,
            cancellationToken);

    private async Task<bool> WaitForDecisionAsync(string requestId, CancellationToken cancellationToken)
    {
        var source = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[requestId] = source;

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(1, decisionTimeoutSeconds)));
        using var registration = timeout.Token.Register(() => source.TrySetResult(false));

        try
        {
            return await source.Task.WaitAsync(cancellationToken);
        }
        finally
        {
            _pending.TryRemove(requestId, out _);
        }
    }

    private static bool TryReadPath(string input, out string path)
    {
        path = string.Empty;
        var trimmed = input.Trim();
        if (!trimmed.StartsWith('{'))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(trimmed);
            foreach (var key in new[] { "path", "filePath", "file" })
            {
                if (document.RootElement.TryGetProperty(key, out var value) &&
                    value.ValueKind == JsonValueKind.String)
                {
                    path = value.GetString() ?? string.Empty;
                    return path.Length > 0;
                }
            }
        }
        catch (JsonException)
        {
            // O próprio tool retorna erro de argumentos inválidos.
        }

        return false;
    }
}
