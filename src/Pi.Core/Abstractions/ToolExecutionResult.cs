// Resultado estruturado de uma ferramenta. Texto de erro é seguro para exibir na TUI.
namespace Pi.Core.Abstractions;

public sealed record ToolExecutionResult(bool Ok, string Output, string? Error = null)
{
    public static ToolExecutionResult Success(string output) => new(true, output, null);

    public static ToolExecutionResult Failure(string error) => new(false, string.Empty, error);

    public string DisplayText => Ok ? Output : Error ?? Output;
}
