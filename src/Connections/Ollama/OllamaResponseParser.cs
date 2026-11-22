// Tira o texto da resposta do Ollama, tanto no formato único quanto em NDJSON.
using System.Text.Json;

namespace Pi.Connections.Ollama;

// Extrai o campo "response" do payload do Ollama, aceitando JSON único ou NDJSON.
public static class OllamaResponseParser
{
    public static string Parse(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new InvalidOperationException("Resposta vazia do Ollama.");
        }

        foreach (var line in body.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!line.StartsWith('{'))
            {
                continue;
            }

            using var document = JsonDocument.Parse(line);
            if (document.RootElement.TryGetProperty("response", out var response))
            {
                return response.GetString() ?? string.Empty;
            }
        }

        throw new InvalidOperationException("Resposta do Ollama não contém o campo 'response'.");
    }
}
