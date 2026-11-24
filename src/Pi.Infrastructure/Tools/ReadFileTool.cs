// Leitura paginada por linha com limite de bytes e validação de sandbox.
using System.Text;
using System.Text.Json;
using Pi.Core.Abstractions;
using Pi.Core.Policies;
using Pi.Infrastructure.Options;

namespace Pi.Infrastructure.Tools;

public sealed class ReadFileTool(
    WorkspaceSandboxPolicy sandbox,
    ToolSettings settings) : IAgentTool
{
    private const string DescriptionText =
        "Lê um arquivo de texto dentro do workspace, com paginação opcional por linha e limite de bytes.";

    private const string SchemaText = """
        {
          "type": "object",
          "properties": {
            "path": { "type": "string", "description": "Caminho relativo à raiz do workspace." },
            "offset": { "type": "integer", "minimum": 0, "default": 0 },
            "limit": { "type": "integer", "minimum": 1, "maximum": 500, "default": 200 },
            "maxBytes": { "type": "integer", "minimum": 256 }
          },
          "required": ["path"]
        }
        """;

    public string Name => "read_file";

    public ToolRisk Risk => ToolRisk.ReadOnly;

    public string Description => DescriptionText;

    public ToolRisk RiskLevel => ToolRisk.ReadOnly;

    public string ParametersSchema => SchemaText;

    public bool CanHandle(string input) =>
        input.StartsWith("leia ", StringComparison.OrdinalIgnoreCase) ||
        input.StartsWith("read ", StringComparison.OrdinalIgnoreCase);

    // Caminho legado: mantém compatibilidade com prompts do tipo "leia arquivo.txt 0 20".
    public async Task<string> ExecuteAsync(string input, CancellationToken cancellationToken = default)
    {
        if (input.TrimStart().StartsWith('{'))
        {
            var result = await ExecuteResultAsync(input, cancellationToken);
            return result.DisplayText;
        }

        var tokens = ToolInput.Tokens(ToolInput.Argument(input));
        if (tokens.Count == 0)
        {
            return "Informe o caminho do arquivo após 'leia'.";
        }

        var arguments = JsonSerializer.Serialize(new
        {
            path = tokens[0],
            offset = tokens.Count > 1 && int.TryParse(tokens[1], out var parsedOffset)
                ? Math.Max(0, parsedOffset)
                : 0,
            limit = tokens.Count > 2 && int.TryParse(tokens[2], out var parsedLimit)
                ? Math.Clamp(parsedLimit, 1, 500)
                : 200
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
            return Task.FromResult(ToolExecutionResult.Failure(
                "Argumentos inválidos. Esperado JSON com o campo 'path'."));
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("path", out var pathElement) ||
                pathElement.ValueKind != JsonValueKind.String)
            {
                return Task.FromResult(ToolExecutionResult.Failure(
                    "O campo 'path' é obrigatório e deve ser um texto."));
            }

            var requestedPath = pathElement.GetString() ?? string.Empty;
            if (requestedPath.Length == 0)
            {
                return Task.FromResult(ToolExecutionResult.Failure("O campo 'path' não pode ser vazio."));
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
                return Task.FromResult(ToolExecutionResult.Failure(
                    $"O caminho é um diretório, não um arquivo: {requestedPath}"));
            }

            if (!File.Exists(path))
            {
                return Task.FromResult(ToolExecutionResult.Failure(
                    $"Arquivo não encontrado: {requestedPath}"));
            }

            var offset = ReadInt(root, "offset", 0);
            var limit = ReadInt(root, "limit", 200);
            var maxBytes = ReadInt(root, "maxBytes", settings.MaxReadBytes);

            offset = Math.Max(0, offset);
            limit = Math.Clamp(limit, 1, 500);
            maxBytes = Math.Clamp(maxBytes, 256, Math.Max(256, settings.MaxReadBytes));

            var output = new StringBuilder();
            var bytes = 0;
            var lineNumber = offset + 1;

            foreach (var line in File.ReadLines(path).Skip(offset).Take(limit))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var rendered = $"{lineNumber,5}: {line}{Environment.NewLine}";
                var size = Encoding.UTF8.GetByteCount(rendered);
                if (bytes + size > maxBytes)
                {
                    output.AppendLine($"... limite de {maxBytes} bytes atingido.");
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
