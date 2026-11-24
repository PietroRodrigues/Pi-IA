// Busca rápida em arquivos de texto, respeitando .gitignore e limites de tempo/resultado.
using Microsoft.Extensions.Logging;
using Pi.Core.Abstractions;
using Pi.Core.Policies;
using Pi.Infrastructure.Options;

namespace Pi.Infrastructure.Tools;

public sealed class SearchCodeTool(
    WorkspaceSandboxPolicy sandbox,
    ToolSettings settings,
    ILogger<SearchCodeTool> logger) : IAgentTool
{
    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", "node_modules", "artifacts", "bin", "obj", "dist", ".vs", "coverage"
    };

    public string Name => "search_code";

    public ToolRisk Risk => ToolRisk.ReadOnly;

    public bool CanHandle(string input) =>
        input.StartsWith("procure ", StringComparison.OrdinalIgnoreCase) ||
        input.StartsWith("busque ", StringComparison.OrdinalIgnoreCase);

    public async Task<string> ExecuteAsync(string input, CancellationToken cancellationToken = default)
    {
        var term = ToolInput.Argument(input);
        if (term.Length < 2)
        {
            return "Informe um termo com pelo menos 2 caracteres após 'procure'.";
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, settings.SearchTimeoutSeconds)));

        try
        {
            var matches = await Task.Run(() => Search(term, timeout.Token), timeout.Token);
            return matches.Count == 0
                ? "Nenhum arquivo encontrado."
                : string.Join(Environment.NewLine, matches);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return "A busca excedeu o tempo limite.";
        }
    }

    private List<string> Search(string term, CancellationToken cancellationToken)
    {
        var found = new List<string>();
        var ignorePatterns = ReadIgnorePatterns();
        var roots = new List<string> { sandbox.Root };
        roots.AddRange(settings.AllowedFolders.Where(sandbox.IsAllowed));

        foreach (var root in roots.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            Walk(root, root, term, ignorePatterns, found, cancellationToken);
            if (found.Count >= settings.MaxResults)
            {
                break;
            }
        }

        return found;
    }

    private void Walk(
        string root,
        string folder,
        string term,
        IReadOnlyList<string> ignorePatterns,
        List<string> found,
        CancellationToken cancellationToken)
    {
        if (found.Count >= settings.MaxResults)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (IsIgnored(relative, ignorePatterns) || new FileInfo(file).Length > 1_000_000)
            {
                continue;
            }

            try
            {
                foreach (var line in File.ReadLines(file))
                {
                    if (line.Contains(term, StringComparison.OrdinalIgnoreCase))
                    {
                        found.Add($"{relative}: {line.Trim()}");
                        break;
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                logger.LogDebug(ex, "Não foi possível ler {File}", file);
            }

            if (found.Count >= settings.MaxResults)
            {
                return;
            }
        }

        foreach (var directory in Directory.EnumerateDirectories(folder, "*", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileName(directory);
            var relative = Path.GetRelativePath(root, directory).Replace('\\', '/');

            if (IgnoredDirectories.Contains(name) || IsIgnored(relative, ignorePatterns))
            {
                continue;
            }

            Walk(root, directory, term, ignorePatterns, found, cancellationToken);
        }
    }

    private IReadOnlyList<string> ReadIgnorePatterns()
    {
        var gitignore = Path.Combine(sandbox.Root, ".gitignore");
        if (!File.Exists(gitignore))
        {
            return [];
        }

        return File.ReadLines(gitignore)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .Select(line => line.TrimStart('/'))
            .ToList();
    }

    private static bool IsIgnored(string relativePath, IReadOnlyList<string> patterns) =>
        patterns.Any(pattern =>
            relativePath.Contains(pattern, StringComparison.OrdinalIgnoreCase) ||
            relativePath.EndsWith(pattern, StringComparison.OrdinalIgnoreCase));
}
