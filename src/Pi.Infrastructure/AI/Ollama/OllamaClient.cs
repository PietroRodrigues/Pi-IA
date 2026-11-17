using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pi.Application.Abstractions.AI;
using Pi.Application.Options;

namespace Pi.Infrastructure.AI.Ollama;

/// <summary>
/// Cliente HTTP para a API /api/generate do Ollama.
/// </summary>
public sealed class OllamaClient : ILLMClient
{
    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;
    private readonly ILogger<OllamaClient> _logger;

    public OllamaClient(
        HttpClient httpClient,
        IOptions<OllamaOptions> options,
        ILogger<OllamaClient> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public async Task<string> SendAsync(string prompt, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        var payload = new
        {
            model = _options.Model,
            prompt,
            stream = false
        };

        using var response = await _httpClient.PostAsJsonAsync("/api/generate", payload, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Ollama retornou {StatusCode}: {Body}",
                (int)response.StatusCode,
                body);

            throw new InvalidOperationException($"Ollama retornou {(int)response.StatusCode}: {body}");
        }

        return OllamaResponseParser.Parse(body);
    }
}
