// Extrai o texto da resposta do Ollama, aceitando JSON único ou NDJSON.
using System.Text.Json;

namespace Pi.Infrastructure.Llm;

public static class OllamaResponseParser
{
    public static string Parse(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new InvalidOperationException("Resposta vazia do Ollama.");
        }

        var sawResponse = false;

        foreach (var line in body.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!line.StartsWith('{'))
            {
                continue;
            }

            using var document = JsonDocument.Parse(line);
            if (!document.RootElement.TryGetProperty("response", out var response))
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

        throw new InvalidOperationException("Resposta do Ollama não contém o campo 'response'.");
    }
}
