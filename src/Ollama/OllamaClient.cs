using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pi.Abstracts.AI;
using Pi.Options;

namespace Pi.Ollama;

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

    public string ModelName => _options.Model;

    public async Task<string> SendAsync(string prompt, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        var payload = new
        {
            model = _options.Model,
            prompt,
            stream = false
        };

        string body;

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("/api/generate", payload, cancellationToken);
            body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Ollama retornou {StatusCode}: {Body}",
                    (int)response.StatusCode,
                    body);

                var dica = response.StatusCode == HttpStatusCode.NotFound &&
                           body.Contains("not found", StringComparison.OrdinalIgnoreCase)
                    ? " Modelo ausente: rode scripts/setup-ollama.ps1 antes de conversar."
                    : string.Empty;

                throw new InvalidOperationException(
                    $"Ollama retornou {(int)response.StatusCode}: {body}.{dica}");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Não foi possível conectar ao Ollama em {BaseUrl}", _options.BaseUrl);

            throw new InvalidOperationException(
                $"Não foi possível conectar ao Ollama em {_options.BaseUrl}. " +
                "Instale em https://ollama.com/download ou configure Ollama:ExecutablePath.",
                ex);
        }

        return OllamaResponseParser.Parse(body);
    }
}
