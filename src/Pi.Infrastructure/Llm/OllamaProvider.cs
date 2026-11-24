// Cliente HTTP resiliente do Ollama: resposta única, streaming NDJSON e tool calling,
// com timeout até o primeiro byte e cancelamento colaborativo no corpo.
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Pi.Core.Abstractions;
using Pi.Infrastructure.Options;

namespace Pi.Infrastructure.Llm;

public sealed class OllamaProvider(
    HttpClient httpClient,
    OllamaSettings settings,
    ILogger<OllamaProvider> logger) : ILlmProvider
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public string ModelName => settings.Model;

    public async Task<string> SendAsync(string prompt, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        using var timeout = CreateTimeout(cancellationToken);
        using var response = await PostAsync(prompt, stream: false, timeout.Token);

        var body = await response.Content.ReadAsStringAsync(timeout.Token);
        EnsureSuccess(response, body);

        return OllamaResponseParser.Parse(body);
    }

    public async IAsyncEnumerable<LlmChunk> StreamAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        using var initial = CreateTimeout(cancellationToken);
        using var request = BuildRequest(prompt, stream: true);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                initial.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("O Ollama não respondeu dentro do tempo limite.");
        }
        catch (HttpRequestException ex)
        {
            throw ExplainConnectionFailure(ex);
        }

        using (response)
        {
            var body = string.Empty;
            if (!response.IsSuccessStatusCode)
            {
                body = await response.Content.ReadAsStringAsync(cancellationToken);
                EnsureSuccess(response, body);
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith('{'))
                {
                    continue;
                }

                using var document = JsonDocument.Parse(line);
                var root = document.RootElement;

                var text = root.TryGetProperty("response", out var textProperty)
                    ? textProperty.GetString() ?? string.Empty
                    : string.Empty;

                var done = root.TryGetProperty("done", out var doneProperty) &&
                           doneProperty.ValueKind == JsonValueKind.True;

                yield return new LlmChunk(
                    text,
                    done,
                    ReadInt(root, "prompt_eval_count"),
                    ReadInt(root, "eval_count"));
            }
        }
    }

    public async IAsyncEnumerable<LlmResponseChunk> StreamAsync(
        IReadOnlyList<LlmMessage> messages,
        IReadOnlyList<LlmToolDefinition> tools,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentNullException.ThrowIfNull(tools);

        using var initial = CreateTimeout(cancellationToken);
        using var request = BuildChatRequest(messages, tools);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                initial.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("O Ollama não respondeu dentro do tempo limite.");
        }
        catch (HttpRequestException ex)
        {
            throw ExplainConnectionFailure(ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                EnsureSuccess(response, body);
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);
            var seenToolCalls = new HashSet<string>(StringComparer.Ordinal);

            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith('{'))
                {
                    continue;
                }

                using var document = JsonDocument.Parse(line);
                var root = document.RootElement;
                var message = root.TryGetProperty("message", out var messageElement)
                    ? messageElement
                    : default;

                var done = root.TryGetProperty("done", out var doneProperty) &&
                           doneProperty.ValueKind == JsonValueKind.True;

                yield return new LlmResponseChunk(
                    ReadText(message),
                    done,
                    ReadToolCalls(message, seenToolCalls),
                    ReadInt(root, "prompt_eval_count"),
                    ReadInt(root, "eval_count"));
            }
        }
    }

    private CancellationTokenSource CreateTimeout(CancellationToken cancellationToken)
    {
        var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        source.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, settings.TimeoutSeconds)));
        return source;
    }

    private async Task<HttpResponseMessage> PostAsync(string prompt, bool stream, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(prompt, stream);

        try
        {
            return await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("O Ollama não respondeu dentro do tempo limite.");
        }
        catch (HttpRequestException ex)
        {
            throw ExplainConnectionFailure(ex);
        }
    }

    private HttpRequestMessage BuildRequest(string prompt, bool stream) =>
        new(HttpMethod.Post, "/api/generate")
        {
            Content = JsonContent.Create(new
            {
                model = settings.Model,
                prompt,
                stream,
                think = false
            }, options: Json)
        };

    private HttpRequestMessage BuildChatRequest(
        IReadOnlyList<LlmMessage> messages,
        IReadOnlyList<LlmToolDefinition> tools) =>
        new(HttpMethod.Post, "/api/chat")
        {
            Content = JsonContent.Create(new
            {
                model = settings.Model,
                stream = true,
                think = false,
                messages = messages.Select(BuildChatMessage).ToArray(),
                tools = tools.Select(BuildToolDefinition).ToArray()
            }, options: Json)
        };

    private static Dictionary<string, object?> BuildChatMessage(LlmMessage message)
    {
        var body = new Dictionary<string, object?>
        {
            ["role"] = message.Role,
            ["content"] = message.Content
        };

        if (message.ToolCallId is { Length: > 0 })
        {
            body["tool_call_id"] = message.ToolCallId;
        }

        if (message.ToolCalls is { Count: > 0 })
        {
            body["tool_calls"] = message.ToolCalls.Select(call =>
            {
                var function = new Dictionary<string, object?>
                {
                    ["name"] = call.Name,
                    ["arguments"] = ParseElement(call.ArgumentsJson)
                };

                var item = new Dictionary<string, object?> { ["function"] = function };
                if (call.Id is { Length: > 0 })
                {
                    item["id"] = call.Id;
                }

                return item;
            }).ToArray();
        }

        return body;
    }

    private static object BuildToolDefinition(LlmToolDefinition tool) => new
    {
        type = "function",
        function = new
        {
            name = tool.Name,
            description = tool.Description,
            parameters = ParseElement(tool.ParametersSchema)
        }
    };

    private static JsonElement ParseElement(string? json)
    {
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                return document.RootElement.Clone();
            }
            catch (JsonException)
            {
                // Schema inválido não deve derrubar o turno.
            }
        }

        using var empty = JsonDocument.Parse("{}");
        return empty.RootElement.Clone();
    }

    private static string ReadText(JsonElement message) =>
        message.ValueKind == JsonValueKind.Object &&
        message.TryGetProperty("content", out var content) &&
        content.ValueKind == JsonValueKind.String
            ? content.GetString() ?? string.Empty
            : string.Empty;

    private static IReadOnlyList<LlmToolCall> ReadToolCalls(
        JsonElement message,
        HashSet<string> seen)
    {
        if (message.ValueKind != JsonValueKind.Object ||
            !message.TryGetProperty("tool_calls", out var array) ||
            array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var calls = new List<LlmToolCall>();

        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object ||
                !item.TryGetProperty("function", out var function))
            {
                continue;
            }

            var name = function.TryGetProperty("name", out var nameElement)
                ? nameElement.GetString() ?? string.Empty
                : string.Empty;
            if (name.Length == 0)
            {
                continue;
            }

            var arguments = function.TryGetProperty("arguments", out var argumentsElement)
                ? argumentsElement.ValueKind == JsonValueKind.String
                    ? argumentsElement.GetString() ?? "{}"
                    : argumentsElement.GetRawText()
                : "{}";

            var id = item.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
            var key = $"{id ?? string.Empty}|{name}|{arguments}";

            if (seen.Add(key))
            {
                calls.Add(new LlmToolCall(name, arguments, id));
            }
        }

        return calls;
    }

    private void EnsureSuccess(HttpResponseMessage response, string body)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        logger.LogError("Ollama retornou {Status}: {Body}", (int)response.StatusCode, body);

        var hint = response.StatusCode == HttpStatusCode.NotFound &&
                   body.Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? " Modelo ausente: configure PI_MODEL com um modelo já instalado."
            : string.Empty;

        throw new InvalidOperationException($"Ollama retornou {(int)response.StatusCode}: {body}.{hint}");
    }

    private InvalidOperationException ExplainConnectionFailure(HttpRequestException ex)
    {
        logger.LogError(ex, "Não foi possível conectar ao Ollama em {BaseUrl}", settings.BaseUrl);
        return new InvalidOperationException(
            $"Não foi possível conectar ao Ollama em {settings.BaseUrl}. " +
            "Instale em https://ollama.com/download ou configure OLLAMA_BASE_URL.", ex);
    }

    private static int? ReadInt(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.TryGetInt32(out var number)
            ? number
            : null;
}
