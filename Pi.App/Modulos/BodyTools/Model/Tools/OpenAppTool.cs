// Ferramenta reservada para "abra ...". Hoje só avisa que ainda não foi implementada.
using Microsoft.Extensions.Logging;

using Pi.Model.Contracts;

namespace Pi.Model.Tools;

// Placeholder para a futura abertura de aplicativos.
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
