// Descobre processos locais do Ollama para que a saída do Pi não deixe o
// servidor órfão — inclusive quando ele já estava aberto antes do app iniciar.
using System.Diagnostics;
using Pi.Contracts.Constants;

namespace Pi.Infrastructure.Llm;

public interface IOllamaProcessDiscovery
{
    IReadOnlyCollection<Process> FindLocal();
}

public sealed class OllamaProcessDiscovery : IOllamaProcessDiscovery
{
    public IReadOnlyCollection<Process> FindLocal() =>
        Process.GetProcesses()
            .Where(process => IsOllamaName(process.ProcessName))
            .ToArray();

    private static bool IsOllamaName(string name) =>
        name.StartsWith(PiConstants.ProcessNames.Ollama, StringComparison.OrdinalIgnoreCase);
}
