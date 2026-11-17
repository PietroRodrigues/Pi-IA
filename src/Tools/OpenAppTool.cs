using Microsoft.Extensions.Logging;
using Pi.Abstracts.Tools;
using Pi.Tools;

namespace Pi.Tools;

/// <summary>
/// Placeholder para a futura abertura de aplicativos.
/// </summary>
public sealed class OpenAppTool : ITool
{
    private readonly ILogger<OpenAppTool> _logger;

    public OpenAppTool(ILogger<OpenAppTool> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public string Name => ToolNames.OpenApp;

    public bool CanHandle(string input)
        => input.StartsWith("abra", StringComparison.OrdinalIgnoreCase);

    public Task<string> ExecuteAsync(string input, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Ferramenta {Tool} foi acionada mas ainda não está implementada", Name);
        return Task.FromResult("A abertura de aplicativos ainda não está implementada.");
    }
}
