// Modelos mínimos para tool calling. O domínio não conhece o formato do Ollama.
namespace Pi.Core.Abstractions;

public sealed record LlmToolDefinition(string Name, string Description, string ParametersSchema);

public sealed record LlmToolCall(string Name, string ArgumentsJson, string? Id = null);

public sealed record LlmMessage(
    string Role,
    string Content,
    string? ToolCallId = null,
    IReadOnlyList<LlmToolCall>? ToolCalls = null);

public sealed record LlmResponseChunk(
    string Text,
    bool Done,
    IReadOnlyList<LlmToolCall> ToolCalls,
    int? PromptEvalCount = null,
    int? EvalCount = null);
