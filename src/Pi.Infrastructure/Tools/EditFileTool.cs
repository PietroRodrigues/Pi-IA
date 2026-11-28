// Edição segura de arquivos: criação, faixa de linhas, backup atômico e diff unificado.
using System.Text.Json;
using Pi.Contracts.Constants;
using Pi.Core.Abstractions;
using Pi.Core.Policies;

namespace Pi.Infrastructure.Tools;

public sealed class EditFileTool(WorkspaceSandboxPolicy sandbox) : IAgentTool
{
    public string Name => PiConstants.Tools.EditFileName;

    public ToolRisk Risk => ToolRisk.Write;

    public string Description => PiConstants.Json.EditFileDescription;

    public string ParametersSchema => PiConstants.Json.EditFileSchema;

    public bool CanHandle(string input) =>
        PiConstants.Tools.EditPrefixes.Any(prefix =>
            input.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    public async Task<string> ExecuteAsync(string input, CancellationToken cancellationToken = default)
    {
        var trimmed = input.Trim();
        var arguments = trimmed.StartsWith(PiConstants.Json.ObjectStart)
            ? trimmed
            : ToolInput.Argument(input);
        return (await ExecuteResultAsync(arguments, cancellationToken)).DisplayText;
    }

    public async Task<ToolExecutionResult> ExecuteResultAsync(
        string arguments,
        CancellationToken cancellationToken = default)
    {
        var trimmed = arguments.Trim();
        var json = trimmed.StartsWith(PiConstants.Json.ObjectStart)
            ? trimmed
            : ToJson(trimmed);
        if (json is null)
        {
            return ToolExecutionResult.Failure(PiConstants.Messages.EditFormat);
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return ToolExecutionResult.Failure(string.Format(
                PiConstants.Messages.InvalidJsonArgumentsTemplate,
                PiConstants.Json.Path));
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty(PiConstants.Json.Path, out var pathElement) ||
                pathElement.ValueKind != JsonValueKind.String)
            {
                return ToolExecutionResult.Failure(string.Format(
                    PiConstants.Messages.MissingFieldTemplate,
                    PiConstants.Json.Path));
            }

            var requestedPath = pathElement.GetString() ?? string.Empty;
            if (requestedPath.Length == 0)
            {
                return ToolExecutionResult.Failure(string.Format(
                    PiConstants.Messages.EmptyFieldTemplate,
                    PiConstants.Json.Path));
            }

            string path;
            try
            {
                path = sandbox.ResolvePath(requestedPath);
            }
            catch (UnauthorizedAccessException ex)
            {
                return ToolExecutionResult.Failure(ex.Message);
            }

            var operation = ReadString(root, PiConstants.Json.Operation)
                ?? PiConstants.Tools.ReplaceTextOperation;
            var overwrite = ReadBool(root, PiConstants.Json.Overwrite, false);

            return operation switch
            {
                PiConstants.Tools.CreateFileOperation => await CreateAsync(
                    root, path, requestedPath, overwrite, cancellationToken),
                PiConstants.Tools.ReplaceLinesOperation => await ReplaceLinesAsync(
                    root, path, requestedPath, cancellationToken),
                PiConstants.Tools.ReplaceTextOperation => await ReplaceTextAsync(
                    root, path, requestedPath, cancellationToken),
                _ => ToolExecutionResult.Failure(string.Format(
                    PiConstants.Messages.UnknownOperationTemplate,
                    operation))
            };
        }
    }

    private async Task<ToolExecutionResult> CreateAsync(
        JsonElement root,
        string path,
        string requestedPath,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        var content = ReadString(root, PiConstants.Json.Content);
        if (content is null)
        {
            return ToolExecutionResult.Failure(PiConstants.Messages.MissingFileContent);
        }

        if (File.Exists(path) && !overwrite)
        {
            return ToolExecutionResult.Failure(string.Format(
                PiConstants.Messages.FileAlreadyExistsTemplate,
                requestedPath));
        }

        var parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(parent))
        {
            if (!sandbox.IsAllowed(parent))
            {
                return ToolExecutionResult.Failure(string.Format(
                    PiConstants.Messages.PathOutsideWorkspaceTemplate,
                    requestedPath));
            }

            Directory.CreateDirectory(parent);
        }

        var original = File.Exists(path)
            ? await File.ReadAllTextAsync(path, cancellationToken)
            : string.Empty;

        return await CommitAsync(
            path,
            original,
            content,
            overwrite,
            string.Format(PiConstants.Messages.FileCreatedTemplate, Path.GetFileName(path)),
            cancellationToken);
    }

    private async Task<ToolExecutionResult> ReplaceLinesAsync(
        JsonElement root,
        string path,
        string requestedPath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return ToolExecutionResult.Failure(string.Format(
                PiConstants.Messages.FileNotFoundTemplate,
                requestedPath));
        }

        var newContent = ReadString(root, PiConstants.Json.NewContent);
        if (newContent is null)
        {
            return ToolExecutionResult.Failure(PiConstants.Messages.MissingNewContent);
        }

        var original = await File.ReadAllTextAsync(path, cancellationToken);
        if (original.Length == 0)
        {
            return ToolExecutionResult.Failure(PiConstants.Messages.EmptyFileForLines);
        }

        var lines = (await File.ReadAllLinesAsync(path, cancellationToken)).ToList();
        var start = ReadInt(root, PiConstants.Json.StartLine, 0);
        var end = ReadInt(root, PiConstants.Json.EndLine, 0);

        if (start < PiConstants.Limits.MinimumLineNumber || end < start)
        {
            return ToolExecutionResult.Failure(string.Format(
                PiConstants.Messages.InvalidLineRangeTemplate,
                start,
                end));
        }

        if (end > lines.Count)
        {
            return ToolExecutionResult.Failure(string.Format(
                PiConstants.Messages.LineOutOfRangeTemplate,
                start,
                end,
                lines.Count));
        }

        var replacement = SplitLines(newContent);
        var updatedLines = lines
            .Take(start - PiConstants.Limits.MinimumLineNumber)
            .Concat(replacement)
            .Concat(lines.Skip(end))
            .ToList();
        var trailing = EndsWithNewLine(original);
        var updated = string.Join(Environment.NewLine, updatedLines) +
                      (trailing && updatedLines.Count > 0 ? Environment.NewLine : string.Empty);

        return await CommitAsync(
            path,
            original,
            updated,
            overwrite: true,
            string.Format(PiConstants.Messages.FileUpdatedTemplate, Path.GetFileName(path)),
            cancellationToken);
    }

    private async Task<ToolExecutionResult> ReplaceTextAsync(
        JsonElement root,
        string path,
        string requestedPath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return ToolExecutionResult.Failure(string.Format(
                PiConstants.Messages.FileNotFoundTemplate,
                requestedPath));
        }

        var oldText = ReadString(root, PiConstants.Json.OldText);
        var newText = ReadString(root, PiConstants.Json.NewText);
        if (oldText is null || newText is null)
        {
            return ToolExecutionResult.Failure(PiConstants.Messages.MissingReplaceText);
        }

        if (oldText.Length == 0)
        {
            return ToolExecutionResult.Failure(PiConstants.Messages.EmptyOldText);
        }

        var original = await File.ReadAllTextAsync(path, cancellationToken);
        var occurrences = CountOccurrences(original, oldText);
        if (occurrences != PiConstants.Limits.ExpectedSingleOccurrence)
        {
            return ToolExecutionResult.Failure(string.Format(
                PiConstants.Messages.OldTextOccurrencesTemplate,
                occurrences,
                PiConstants.Limits.ExpectedSingleOccurrence));
        }

        var updated = original.Replace(oldText, newText, StringComparison.Ordinal);
        return await CommitAsync(
            path,
            original,
            updated,
            overwrite: true,
            string.Format(PiConstants.Messages.FileUpdatedTemplate, Path.GetFileName(path)),
            cancellationToken);
    }

    private async Task<ToolExecutionResult> CommitAsync(
        string path,
        string original,
        string updated,
        bool overwrite,
        string success,
        CancellationToken cancellationToken)
    {
        var token = Guid.NewGuid().ToString(PiConstants.Formats.GuidN);
        var temporary = path + PiConstants.Paths.TemporarySuffix + token;
        var backup = path + PiConstants.Paths.BackupSuffix + token;
        var hadOriginal = File.Exists(path);
        var backupWritten = false;

        try
        {
            if (hadOriginal)
            {
                await File.WriteAllTextAsync(backup, original, cancellationToken);
                backupWritten = true;
            }

            await File.WriteAllTextAsync(temporary, updated, cancellationToken);
            File.Move(temporary, path, overwrite);

            if (backupWritten)
            {
                File.Delete(backup);
            }

            var diff = UnifiedDiff.Build(DisplayPath(path), original, updated);
            return ToolExecutionResult.Success(success + Environment.NewLine + diff);
        }
        catch
        {
            if (backupWritten)
            {
                File.Copy(backup, path, overwrite: true);
            }

            if (File.Exists(backup))
            {
                File.Delete(backup);
            }

            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }

            throw;
        }
    }

    private string DisplayPath(string path) =>
        Path.GetRelativePath(sandbox.Root, path)
            .Replace(PiConstants.Paths.WindowsSeparator, PiConstants.Paths.UnixSeparator);

    private static string? ToJson(string input)
    {
        var pipe = input.IndexOf(PiConstants.Protocol.PipeSeparator, StringComparison.Ordinal);
        if (pipe < 0)
        {
            return null;
        }

        var path = input[..pipe].Trim();
        var rest = input[(pipe + PiConstants.Protocol.PipeSeparator.Length)..].Trim();
        var arrow = rest.IndexOf(PiConstants.Protocol.EditArrow, StringComparison.Ordinal);
        if (arrow < 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(new Dictionary<string, string>
        {
            [PiConstants.Json.Path] = path,
            [PiConstants.Json.Operation] = PiConstants.Tools.ReplaceTextOperation,
            [PiConstants.Json.OldText] = rest[..arrow].Trim(),
            [PiConstants.Json.NewText] = rest[(arrow + PiConstants.Protocol.EditArrow.Length)..].Trim()
        });
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

    private static bool ReadBool(JsonElement root, string name, bool fallback) =>
        root.TryGetProperty(name, out var value) &&
        value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : fallback;

    private static IReadOnlyList<string> SplitLines(string text)
    {
        if (text.Length == 0)
        {
            return [];
        }

        var crlf = string.Concat(PiConstants.Protocol.CarriageReturn, PiConstants.Protocol.LineFeed);
        var lineFeed = PiConstants.Protocol.LineFeed.ToString();
        return text.Replace(crlf, lineFeed)
            .TrimEnd(PiConstants.Protocol.LineFeed)
            .Split(PiConstants.Protocol.LineFeed);
    }

    private static bool EndsWithNewLine(string text) =>
        text.Length > 0 &&
        (text.EndsWith(PiConstants.Protocol.LineFeed) ||
         text.EndsWith(PiConstants.Protocol.CarriageReturn));

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;

        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
