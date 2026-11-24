// Opções de execução de ferramentas usadas pelo Host via DI.
namespace Pi.Core.Engine;

public sealed record ToolExecutionOptions(
    int DecisionTimeoutSeconds,
    int ExecutionTimeoutSeconds)
{
    public static ToolExecutionOptions Default { get; } = new(30, 30);
}
