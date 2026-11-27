// Extrai o texto da resposta do Ollama, aceitando JSON único ou NDJSON.
using System.Text.Json;
using Pi.Contracts.Constants;

namespace Pi.Infrastructure.Llm;

public static class OllamaResponseParser
{
    public static string Parse(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new InvalidOperationException(PiConstants.Messages.EmptyOllamaResponse);
        }

        var sawResponse = false;

        foreach (var line in body.Split(PiConstants.Protocol.LineFeed,
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!line.StartsWith(PiConstants.Json.ObjectStart))
            {
                continue;
            }

            using var document = JsonDocument.Parse(line);
            if (!document.RootElement.TryGetProperty(PiConstants.Json.Response, out var response))
            {
                continue;
            }

            sawResponse = true;
            var text = response.GetString() ?? string.Empty;
            if (text.Length > 0)
            {
                return text;
            }
        }

        if (sawResponse)
        {
            return string.Empty;
        }

        throw new InvalidOperationException(string.Format(
            PiConstants.Messages.OllamaResponseMissingFieldTemplate,
            PiConstants.Json.Response));
    }
}
