using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pi.Abstracts.Tools;
using Pi.Options;
using Pi.Tools;

namespace Pi.Tools;

/// <summary>
/// Busca arquivos por nome nas pastas explicitamente permitidas.
/// </summary>
public sealed class SearchFilesTool : ITool
{
    private readonly ToolOptions _options;
    private readonly ILogger<SearchFilesTool> _logger;

    public SearchFilesTool(IOptions<ToolOptions> options, ILogger<SearchFilesTool> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
    }

    public string Name => ToolNames.SearchFiles;

    public bool CanHandle(string input)
        => input.StartsWith("procure", StringComparison.OrdinalIgnoreCase);

    public async Task<string> ExecuteAsync(string input, CancellationToken cancellationToken = default)
    {
        var term = input.Replace("procure", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();

        if (term.Length < 2)
        {
            return "Informe um termo com pelo menos 2 caracteres após 'procure'.";
        }

        if (_options.AllowedFolders.Length == 0)
        {
            return "Nenhuma pasta permitida configurada. Ajuste Tools:AllowedFolders no appsettings.json.";
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.SearchTimeoutSeconds));

        List<string> found;

        try
        {
            found = await Task.Run(() => Search(term, timeout.Token), timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return "A busca excedeu o tempo limite.";
        }

        return found.Count == 0
            ? "Nenhum arquivo encontrado."
            : string.Join(Environment.NewLine, found);
    }

    private List<string> Search(string term, CancellationToken cancellationToken)
    {
        var found = new List<string>();

        foreach (var folder in _options.AllowedFolders)
        {
            if (!Directory.Exists(folder))
            {
                _logger.LogWarning("Pasta permitida não existe: {Folder}", folder);
                continue;
            }

            try
            {
                foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!Path.GetFileName(file).Contains(term, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    found.Add(file);

                    if (found.Count >= _options.MaxResults)
                    {
                        return found;
                    }
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or DirectoryNotFoundException)
            {
                _logger.LogWarning(ex, "Falha ao varrer a pasta {Folder}", folder);
            }
        }

        return found;
    }
}
