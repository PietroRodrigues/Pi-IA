// Formato neutro dos pedaços de resposta, sem nada específico do Ollama.
namespace Pi.Core.Contracts;

// Pedaço genérico de uma resposta em streaming, independente do provedor.
// Text: Texto gerado neste pedaço.
// Done: Indica o último pacote da resposta.
// PromptEvalCount: Tokens do prompt, quando disponíveis.
// EvalCount: Tokens gerados até o momento, quando disponíveis.
public sealed record LLMStreamChunk(
    string Text,
    bool Done,
    int? PromptEvalCount = null,
    int? EvalCount = null);
