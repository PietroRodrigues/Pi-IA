using Pi.Application.Abstractions.Tools;

namespace Pi.Application.Tools;

/// <summary>
/// Seleciona a primeira ferramenta capaz de tratar a entrada.
/// </summary>
public sealed class ToolRouter
{
    private readonly IReadOnlyList<ITool> _tools;

    public ToolRouter(IEnumerable<ITool> tools)
    {
        ArgumentNullException.ThrowIfNull(tools);
        _tools = tools.ToList();
    }

    public async Task<string?> TryExecuteAsync(string input, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);

        foreach (var tool in _tools)
        {
            if (!tool.CanHandle(input))
            {
                continue;
            }

            return await tool.ExecuteAsync(input, cancellationToken);
        }

        return null;
    }
}
