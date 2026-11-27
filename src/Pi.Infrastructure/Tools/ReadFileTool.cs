// Leitura paginada por linha com limite de bytes e validação de sandbox.
using System.Text;
using System.Text.Json;
using Pi.Contracts.Constants;
using Pi.Core.Abstractions;
using Pi.Core.Policies;
using Pi.Infrastructure.Options;

namespace Pi.Infrastructure.Tools;

public sealed class ReadFileTool(
    WorkspaceSandboxPolicy sandbox,
    ToolSettings settings) : IAgentTool
{
    public string Name => PiConstants.Tools.ReadFileName;

    public ToolRisk Risk => ToolRisk.ReadOnly;

    public string Description => PiConstants.Json.ReadFileDescription;

    public ToolRisk RiskLevel => ToolRisk.ReadOnly;

    public string ParametersSchema => PiConstants.Json.ReadFileSchema;

    public bool CanHandle(string input) =>
        PiConstants.Tools.ReadPrefixes.Any(prefix =>
            input.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    // Caminho legado: mantém compatibilidade com prompts do tipo "leia arquivo.txt 0 20".
    public async Task<string> ExecuteAsync(string input, CancellationToken cancellationToken = default)
    {
        if (input.TrimStart().StartsWith(PiConstants.Json.ObjectStart))
        {
            var result = await ExecuteResultAsync(input, cancellationToken);
            return result.DisplayText;
        }

        var tokens = ToolInput.Tokens(ToolInput.Argument(input));
        if (tokens.Count == 0)
        {
            return PiConstants.Messages.MissingFilePath;
        }

        var arguments = JsonSerializer.Serialize(new
        {
            path = tokens[0],
            offset = tokens.Count > 1 && int.TryParse(tokens[1], out var parsedOffset)
                ? Math.Max(0, parsedOffset)
                : 0,
            limit = tokens.Count > 2 && int.TryParse(tokens[2], out var parsedLimit)
                ? Math.Clamp(
                    parsedLimit,
                    PiConstants.Limits.ReadFileMinLines,
                    PiConstants.Limits.ReadFileMaxLines)
                : PiConstants.Limits.ReadFileDefaultLines
        });

        var legacyResult = await ExecuteResultAsync(arguments, cancellationToken);
        return legacyResult.DisplayText;
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
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty(PiConstants.Json.Path, out var pathElement) ||
                pathElement.ValueKind != JsonValueKind.String)
            {
                return Task.FromResult(ToolExecutionResult.Failure(string.Format(
                    PiConstants.Messages.MissingFieldTemplate,
                    PiConstants.Json.Path)));
            }

            var requestedPath = pathElement.GetString() ?? string.Empty;
            if (requestedPath.Length == 0)
            {
                return Task.FromResult(ToolExecutionResult.Failure(string.Format(
                    PiConstants.Messages.EmptyFieldTemplate,
                    PiConstants.Json.Path)));
            }

            string path;
            try
            {
                path = sandbox.ResolvePath(requestedPath);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Task.FromResult(ToolExecutionResult.Failure(ex.Message));
            }

            if (Directory.Exists(path))
            {
                return Task.FromResult(ToolExecutionResult.Failure(string.Format(
                    PiConstants.Messages.DirectoryInsteadOfFileTemplate,
                    requestedPath)));
            }

            if (!File.Exists(path))
            {
                return Task.FromResult(ToolExecutionResult.Failure(string.Format(
                    PiConstants.Messages.FileNotFoundTemplate,
                    requestedPath)));
            }

            var offset = ReadInt(root, PiConstants.Json.Offset, 0);
            var limit = ReadInt(root, PiConstants.Json.Limit, PiConstants.Limits.ReadFileDefaultLines);
            var maxBytes = ReadInt(root, PiConstants.Json.MaxBytes, settings.MaxReadBytes);

            offset = Math.Max(0, offset);
            limit = Math.Clamp(
                limit,
                PiConstants.Limits.ReadFileMinLines,
                PiConstants.Limits.ReadFileMaxLines);
            maxBytes = Math.Clamp(
                maxBytes,
                PiConstants.Limits.ReadFileMinBytes,
                Math.Max(PiConstants.Limits.ReadFileMinBytes, settings.MaxReadBytes));

            var output = new StringBuilder();
            var bytes = 0;
            var lineNumber = offset + 1;

            foreach (var line in File.ReadLines(path).Skip(offset).Take(limit))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var rendered = string.Format(
                    PiConstants.Messages.ReadFileLineTemplate,
                    lineNumber,
                    line,
                    Environment.NewLine);
                var size = Encoding.UTF8.GetByteCount(rendered);
                if (bytes + size > maxBytes)
                {
                    output.AppendLine(string.Format(PiConstants.Messages.ReadByteLimitTemplate, maxBytes));
                    break;
                }

                output.Append(rendered);
                bytes += size;
                lineNumber++;
            }

            return Task.FromResult(ToolExecutionResult.Success(output.ToString()));
        }
    }

    private static int ReadInt(JsonElement root, string name, int fallback) =>
        root.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.Number &&
        value.TryGetInt32(out var number)
            ? number
            : fallback;
}
