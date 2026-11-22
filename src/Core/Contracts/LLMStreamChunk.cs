namespace Pi.Core.Contracts;

/// <summary>
/// Pedaço genérico de uma resposta em streaming, independente do provedor.
/// </summary>
/// <param name="Text">Texto gerado neste pedaço.</param>
/// <param name="Done">Indica o último pacote da resposta.</param>
/// <param name="PromptEvalCount">Tokens do prompt, quando disponíveis.</param>
/// <param name="EvalCount">Tokens gerados até o momento, quando disponíveis.</param>
public sealed record LLMStreamChunk(
    string Text,
    bool Done,
    int? PromptEvalCount = null,
    int? EvalCount = null);
