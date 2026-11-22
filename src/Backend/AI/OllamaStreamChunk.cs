namespace Pi.Backend.AI;

/// <summary>
/// Um pedaço da resposta do Ollama, com as contagens de contexto quando disponíveis.
/// </summary>
/// <param name="Text">Texto gerado neste pedaço.</param>
/// <param name="Done">Indica o último pacote da resposta.</param>
/// <param name="PromptEvalCount">Tokens do prompt, informados no pacote final.</param>
/// <param name="EvalCount">Tokens gerados até o pacote final.</param>
public sealed record OllamaStreamChunk(
    string Text,
    bool Done,
    int? PromptEvalCount = null,
    int? EvalCount = null);
