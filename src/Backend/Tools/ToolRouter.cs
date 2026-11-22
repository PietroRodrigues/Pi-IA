using Pi.Backend.Chat;

namespace Pi.Backend.Tools;

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

    public Task<string?> TryExecuteAsync(string input, CancellationToken cancellationToken = default) =>
        TryExecuteAsync(input, NullAgentEventSink.Instance, cancellationToken);

    public async Task<string?> TryExecuteAsync(
        string input,
        IAgentEventSink sink,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);
        ArgumentNullException.ThrowIfNull(sink);

        foreach (var tool in _tools)
        {
            if (!tool.CanHandle(input))
            {
                continue;
            }

            await sink.ToolCallAsync(tool.Name, input, cancellationToken);

            try
            {
                var result = await tool.ExecuteAsync(input, cancellationToken);
                await sink.ToolResultAsync(tool.Name, true, result, cancellationToken);
                return result;
            }
            catch
            {
                await sink.ToolResultAsync(tool.Name, false, string.Empty, cancellationToken);
                throw;
            }
        }

        return null;
    }
}
