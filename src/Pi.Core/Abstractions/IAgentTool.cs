// Ferramenta do agente com metadados de risco usados pela política de permissão.
// Propriedades adicionais têm implementação padrão para não quebrar ferramentas antigas.
using Pi.Contracts.Constants;

namespace Pi.Core.Abstractions;

public enum ToolRisk
{
    ReadOnly,
    Write,
    Shell
}

public interface IAgentTool
{
    string Name { get; }

    ToolRisk Risk { get; }

    string Description => Name;

    ToolRisk RiskLevel => Risk;

    string ParametersSchema => PiConstants.Json.EmptyToolSchema;

    bool CanHandle(string input);

    Task<string> ExecuteAsync(string input, CancellationToken cancellationToken = default);

    async Task<ToolExecutionResult> ExecuteResultAsync(
        string arguments,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return ToolExecutionResult.Success(await ExecuteAsync(arguments, cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ToolExecutionResult.Failure(ex.Message);
        }
    }
}
