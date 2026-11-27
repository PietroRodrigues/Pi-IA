// Roteamento tipado dos comandos da TUI para o Pi.Core, com tratamento de
// linhas corrompidas e cancelamento sem derrubar o host.
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pi.Contracts.Constants;
using Pi.Contracts.Protocol;
using Pi.Core.Engine;

namespace Pi.Host.Bridge;

public sealed class CommandDispatcher(
    NdjsonReader reader,
    NdjsonWriter writer,
    AgentOrchestrator orchestrator,
    ToolExecutionCoordinator tools,
    IHostApplicationLifetime lifetime,
    ILogger<CommandDispatcher> logger)
{
    private readonly ConcurrentDictionary<string, Task> _running = new(StringComparer.Ordinal);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                string line;

                try
                {
                    var value = await reader.ReadLineAsync(cancellationToken);
                    if (value is null)
                    {
                        break;
                    }

                    line = value;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (InvalidDataException ex)
                {
                    await PublishErrorAsync(string.Empty, ErrorCodes.InvalidMessage, ex.Message);
                    break;
                }
                catch (IOException ex)
                {
                    logger.LogWarning(ex, PiConstants.Messages.LogBridgeReadFailure);
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                await DispatchAsync(line, cancellationToken);
            }
        }
        finally
        {
            await orchestrator.InterruptAsync(PiConstants.Protocol.ShutdownReason, CancellationToken.None);

            var pending = _running.Values.ToArray();
            if (pending.Length > 0)
            {
                await Task.WhenAll(pending).WaitAsync(
                    TimeSpan.FromSeconds(PiConstants.Timeouts.DispatcherShutdownSeconds));
            }

            lifetime.StopApplication();
        }
    }

    private async Task DispatchAsync(string line, CancellationToken cancellationToken)
    {
        ProtocolEnvelope envelope;

        try
        {
            envelope = ProtocolEnvelope.Parse(line);
        }
        catch (JsonException ex)
        {
            await PublishErrorAsync(
                string.Empty,
                ErrorCodes.InvalidMessage,
                string.Format(PiConstants.Messages.InvalidJsonTemplate, ex.Message));
            return;
        }

        var correlationId = string.IsNullOrWhiteSpace(envelope.CorrelationId)
            ? Guid.NewGuid().ToString(PiConstants.Formats.GuidN)
            : envelope.CorrelationId;

        try
        {
            switch (envelope.Type)
            {
                case CommandTypes.PromptSubmit:
                {
                    var command = envelope.Read<PromptSubmit>();
                    // O orquestrador valida o texto e emite error_event + final.
                    var task = orchestrator.RunTurnAsync(command.Text, correlationId, cancellationToken);
                    _running[correlationId] = task;
                    _ = task.ContinueWith(
                        completed => _running.TryRemove(correlationId, out _),
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                    return;
                }

                case CommandTypes.InterruptTurn:
                    await orchestrator.InterruptAsync(envelope.Read<InterruptTurn>().Reason, cancellationToken);
                    return;

                case CommandTypes.ToolDecision:
                {
                    var decision = envelope.Read<ToolDecision>();
                    tools.Decide(decision.ToolRequestId, decision.Approved);
                    return;
                }

                case CommandTypes.SessionSwitch:
                {
                    var command = envelope.Read<SessionSwitch>();
                    await orchestrator.SwitchSessionAsync(command.SessionId, correlationId, cancellationToken);
                    return;
                }

                case CommandTypes.SessionClear:
                    await orchestrator.ClearAsync(correlationId, cancellationToken);
                    return;

                default:
                    await PublishErrorAsync(
                        correlationId,
                        ErrorCodes.UnknownCommand,
                        string.Format(PiConstants.Messages.UnknownCommandTemplate, envelope.Type));
                    return;
            }
        }
        catch (JsonException ex)
        {
            await PublishErrorAsync(
                correlationId,
                ErrorCodes.InvalidMessage,
                string.Format(PiConstants.Messages.InvalidPayloadTemplate, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, PiConstants.Messages.LogDispatchFailure, envelope.Type);
            await PublishErrorAsync(correlationId, ErrorCodes.Internal, ex.Message);
        }
    }

    private Task PublishErrorAsync(string correlationId, string code, string message) =>
        writer.WriteAsync(
            EventTypes.ErrorEvent,
            new ErrorInfo(code, message),
            correlationId,
            CancellationToken.None);
}
