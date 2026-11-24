// Limites de contexto e suporte a ferramentas por modelo. Hoje o Pi usa o
// campo de texto /api/generate; o catálogo deixa a decisão explícita.
namespace Pi.Infrastructure.Llm;

public sealed record ModelCapabilities(string Model, int ContextWindow, bool SupportsTools)
{
    public static ModelCapabilities For(string model, int contextWindow) =>
        new(model, contextWindow, SupportsToolCalls(model));

    // Tool calling real virá com /api/chat; enquanto isso o agente usa comandos de texto.
    private static bool SupportsToolCalls(string model) =>
        model.Contains("qwen3", StringComparison.OrdinalIgnoreCase) ||
        model.Contains("llama3", StringComparison.OrdinalIgnoreCase);
}
