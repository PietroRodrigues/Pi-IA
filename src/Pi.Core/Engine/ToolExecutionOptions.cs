// Opções de execução de ferramentas usadas pelo Host via DI.
using Pi.Contracts.Constants;

namespace Pi.Core.Engine;

public sealed record ToolExecutionOptions(
    int DecisionTimeoutSeconds,
    int ExecutionTimeoutSeconds)
{
    public static ToolExecutionOptions Default { get; } = new(
        PiConstants.Timeouts.DecisionSeconds,
        PiConstants.Timeouts.ExecutionSeconds);
}
