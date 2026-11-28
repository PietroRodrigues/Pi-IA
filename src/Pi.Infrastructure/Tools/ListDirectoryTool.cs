// Listagem limitada do workspace: profundidade, glob simples e respeito ao sandbox.
using System.Text.Json;
using Pi.Contracts.Constants;
using Pi.Core.Abstractions;
using Pi.Core.Policies;
using Pi.Infrastructure.Options;

namespace Pi.Infrastructure.Tools;

public sealed class ListDirectoryTool(
    WorkspaceSandboxPolicy sandbox,
    ToolSettings settings) : IAgentTool
{
    private static readonly HashSet<string> IgnoredDirectories =
        new(PiConstants.Tools.IgnoredDirectories, StringComparer.OrdinalIgnoreCase);

    public string Name => PiConstants.Tools.ListDirectoryName;

    public ToolRisk Risk => ToolRisk.ReadOnly;

    public string Description => PiConstants.Json.ListDirectoryDescription;

    public string ParametersSchema => PiConstants.Json.ListDirectorySchema;

    public bool CanHandle(string input) =>
        PiConstants.Tools.ListPrefixes.Any(prefix =>
            input.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    public async Task<string> ExecuteAsync(string input, CancellationToken cancellationToken = default)
    {
        if (input.TrimStart().StartsWith(PiConstants.Json.ObjectStart))
        {
            return (await ExecuteResultAsync(input, cancellationToken)).DisplayText;
        }

        var tokens = ToolInput.Tokens(ToolInput.Argument(input));
        var arguments = JsonSerializer.Serialize(new
        {
            path = tokens.Count > 0 ? tokens[0] : string.Empty,
            depth = tokens.Count > 1 && int.TryParse(tokens[1], out var parsedDepth)
                ? parsedDepth
                : PiConstants.Limits.DefaultListDepth,
            pattern = tokens.Count > 2 ? tokens[2] : string.Empty
        });

        return (await ExecuteResultAsync(arguments, cancellationToken)).DisplayText;
    }

    public Task<ToolExecutionResult> ExecuteResultAsync(
        string arguments,
        CancellationToken cancellationToken = default)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(arguments);
        }
        catch (JsonException)
        {
            return Task.FromResult(ToolExecutionResult.Failure(string.Format(
                PiConstants.Messages.InvalidJsonArgumentsTemplate,
                PiConstants.Json.Path)));
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return Task.FromResult(ToolExecutionResult.Failure(string.Format(
                    PiConstants.Messages.MissingFieldTemplate,
                    PiConstants.Json.Path)));
            }

            var requestedPath = ReadString(root, PiConstants.Json.Path);
            var pattern = ReadString(root, PiConstants.Json.Pattern) ?? string.Empty;
            var depth = Math.Clamp(
                ReadInt(root, PiConstants.Json.Depth, PiConstants.Limits.DefaultListDepth),
                0,
                PiConstants.Limits.MaxListDepth);

            string resolved;
            try
            {
                resolved = sandbox.ResolvePath(string.IsNullOrEmpty(requestedPath)
                    ? sandbox.Root
                    : requestedPath);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Task.FromResult(ToolExecutionResult.Failure(ex.Message));
            }

            if (!Directory.Exists(resolved))
            {
                return Task.FromResult(ToolExecutionResult.Failure(string.Format(
                    File.Exists(resolved)
                        ? PiConstants.Messages.FileInsteadOfDirectoryTemplate
                        : PiConstants.Messages.DirectoryNotFoundTemplate,
                    requestedPath)));
            }

            var maxEntries = Math.Clamp(
                settings.MaxResults,
                PiConstants.Limits.MinListEntries,
                PiConstants.Limits.MaxListEntries);

            var entries = new List<string>();
            try
            {
                Walk(
                    resolved,
                    resolved,
                    pattern,
                    depth,
                    0,
                    ReadIgnorePatterns(),
                    entries,
                    maxEntries,
                    cancellationToken);
            }
            catch (ArgumentException)
            {
                return Task.FromResult(ToolExecutionResult.Failure(string.Format(
                    PiConstants.Messages.InvalidPatternTemplate,
                    pattern)));
            }

            if (entries.Count == 0)
            {
                return Task.FromResult(ToolExecutionResult.Success(PiConstants.Messages.DirectoryEmpty));
            }

            var output = string.Join(Environment.NewLine, entries);
            if (entries.Count >= maxEntries)
            {
                output += Environment.NewLine + string.Format(
                    PiConstants.Messages.ListTruncatedTemplate,
                    maxEntries);
            }

            return Task.FromResult(ToolExecutionResult.Success(output));
        }
    }

    private void Walk(
        string root,
        string folder,
        string pattern,
        int maxDepth,
        int currentDepth,
        IReadOnlyList<string> ignorePatterns,
        List<string> entries,
        int maxEntries,
        CancellationToken cancellationToken)
    {
        if (currentDepth > maxDepth || entries.Count >= maxEntries)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        HashSet<string>? matches = null;
        if (pattern.Length > 0)
        {
            matches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var match in Directory.EnumerateFileSystemEntries(
                         folder,
                         pattern,
                         SearchOption.TopDirectoryOnly))
            {
                matches.Add(match);
            }
        }

        foreach (var entry in Directory.EnumerateFileSystemEntries(
                     folder,
                     PiConstants.Paths.AllEntriesPattern,
                     SearchOption.TopDirectoryOnly))
        {
            if (entries.Count >= maxEntries)
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var name = Path.GetFileName(entry);
            var relative = Path.GetRelativePath(root, entry)
                .Replace(PiConstants.Paths.WindowsSeparator, PiConstants.Paths.UnixSeparator);

            if (!sandbox.IsAllowed(entry) ||
                IgnoredDirectories.Contains(name) ||
                IsIgnored(relative, ignorePatterns))
            {
                continue;
            }

            var isDirectory = Directory.Exists(entry);
            if (matches is null || matches.Contains(entry))
            {
                var display = isDirectory ? relative + PiConstants.Paths.UnixSeparator : relative;
                var indent = new string(
                    PiConstants.Protocol.Space,
                    currentDepth * PiConstants.Limits.ListIndentSpaces);
                entries.Add(string.Format(PiConstants.Messages.ListEntryTemplate, indent, display));
            }

            // O glob filtra o que é exibido, mas não poda a travessia das pastas.
            if (isDirectory && currentDepth < maxDepth)
            {
                Walk(
                    root,
                    entry,
                    pattern,
                    maxDepth,
                    currentDepth + 1,
                    ignorePatterns,
                    entries,
                    maxEntries,
                    cancellationToken);
            }
        }
    }

    private static string? ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int ReadInt(JsonElement root, string name, int fallback) =>
        root.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.Number &&
        value.TryGetInt32(out var number)
            ? number
            : fallback;

    private IReadOnlyList<string> ReadIgnorePatterns()
    {
        var gitignore = Path.Combine(sandbox.Root, PiConstants.Paths.GitIgnoreFileName);
        if (!File.Exists(gitignore))
        {
            return [];
        }

        return File.ReadLines(gitignore)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith(PiConstants.Env.CommentPrefix))
            .Select(line => line.TrimStart(PiConstants.Paths.UnixSeparator))
            .ToList();
    }

    private static bool IsIgnored(string relativePath, IReadOnlyList<string> patterns) =>
        patterns.Any(pattern =>
            relativePath.Contains(pattern, StringComparison.OrdinalIgnoreCase) ||
            relativePath.EndsWith(pattern, StringComparison.OrdinalIgnoreCase));
}
