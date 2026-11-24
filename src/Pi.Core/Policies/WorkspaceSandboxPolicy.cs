// Restringe ferramentas ao diretório do projeto e bloqueia comandos perigosos.
// Caminhos fora da raiz (inclusive via ..) são negados antes de qualquer I/O.
using System.Text.Json;
using Pi.Core.Abstractions;

namespace Pi.Core.Policies;

public sealed class WorkspaceSandboxPolicy : IPermissionPolicy
{
    private static readonly string[] DangerousCommands =
    [
        "rm -rf", "del /s", "format", "mkfs", "diskpart", "shutdown",
        "taskkill /f", "stop-process -force", "reg delete", "curl ", "wget "
    ];

    private readonly string _root;
    private readonly IReadOnlyList<string> _allowedFolders;
    private readonly bool _allowShell;

    public WorkspaceSandboxPolicy(string workspaceRoot, IEnumerable<string>? allowedFolders = null, bool allowShell = false)
    {
        _root = Normalize(workspaceRoot);
        _allowedFolders = (allowedFolders ?? [])
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Normalize(path, _root))
            .ToList();
        _allowShell = allowShell;
    }

    public string Root => _root;

    public PermissionDecision Decide(IAgentTool tool, string input)
    {
        var risk = tool.RiskLevel;

        if (risk == ToolRisk.Shell && !_allowShell)
        {
            return PermissionDecision.Deny;
        }

        if (risk == ToolRisk.ReadOnly)
        {
            var path = ExtractPath(input);
            return path is not null && !IsAllowed(path)
                ? PermissionDecision.Deny
                : PermissionDecision.Auto;
        }

        if (risk == ToolRisk.Shell && IsDangerous(input))
        {
            return PermissionDecision.Deny;
        }

        if (risk == ToolRisk.Write)
        {
            var path = ExtractPath(input);
            return path is not null && !IsAllowed(path)
                ? PermissionDecision.Deny
                : PermissionDecision.Ask;
        }

        return PermissionDecision.Ask;
    }

    public bool IsAllowed(string path)
    {
        var full = Normalize(path, _root);
        return IsInside(_root, full) || _allowedFolders.Any(folder => IsInside(folder, full));
    }

    public string ResolvePath(string path)
    {
        if (!IsAllowed(path))
        {
            throw new UnauthorizedAccessException($"Caminho fora do workspace: {path}");
        }

        return Normalize(path, _root);
    }

    public static bool IsDangerous(string input)
    {
        var normalized = input.ToLowerInvariant();
        return DangerousCommands.Any(command => normalized.Contains(command, StringComparison.Ordinal));
    }

    private static string Normalize(string path, string? baseDirectory = null)
    {
        var root = Path.GetPathRoot(path);
        var combined = string.IsNullOrEmpty(root)
            ? Path.Combine(baseDirectory ?? Directory.GetCurrentDirectory(), path)
            : path;
        var full = Path.GetFullPath(combined);
        return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static bool IsInside(string root, string candidate)
    {
        var prefix = root.EndsWith(Path.DirectorySeparatorChar) || root.EndsWith(Path.AltDirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;

        return string.Equals(root, candidate, StringComparison.OrdinalIgnoreCase) ||
               candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractPath(string input)
    {
        var trimmed = input.Trim();

        if (trimmed.StartsWith('{'))
        {
            try
            {
                using var document = JsonDocument.Parse(trimmed);
                foreach (var key in new[] { "path", "filePath", "file" })
                {
                    if (document.RootElement.TryGetProperty(key, out var value) &&
                        value.ValueKind == JsonValueKind.String)
                    {
                        return value.GetString();
                    }
                }
            }
            catch (JsonException)
            {
                // Argumentos inválidos são tratados pela ferramenta/coordenador.
            }

            return null;
        }

        var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 1 ? parts[1] : null;
    }
}
