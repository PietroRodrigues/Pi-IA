using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;

namespace Pi.App.LLM
{
    public class OllamaClient
    { 
        private readonly HttpClient _httpClient;

        public OllamaClient()
        {
            _httpClient = new HttpClient();

            _httpClient.BaseAddress = new Uri("http://localhost:11434");
        }

        public async Task<string> SendMessageAsync(string message)
        {
            var requestBody = new
            {
                model = "qwen2.5:3b",
                prompt = message,
                stream = false
            };

            var json =
                JsonSerializer.Serialize(requestBody);

            var content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"
                );

            var response =
                await _httpClient.PostAsync(
                    "/api/generate",
                    content
                );

            var responseText =
                await response.Content.ReadAsStringAsync();

            Console.WriteLine("\nDEBUG:");
            Console.WriteLine(responseText);

            if (!response.IsSuccessStatusCode)
            {
                return $"Erro: {response.StatusCode}";
            }

            // pega apenas o primeiro JSON válido
            var firstJson =
                responseText
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .First();

            using var document =
                JsonDocument.Parse(firstJson);

            return document
                .RootElement
                .GetProperty("response")
                .GetString()
                ?? "Sem resposta";
        }
    }
}