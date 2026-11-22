// Contrato mínimo para falar com um modelo. Quem implementa decide o provedor.
namespace Pi.Core.Contracts;

// Porta para um modelo de linguagem.
public interface ILLMClient
{
    // Nome do modelo em uso, para exibição na interface.
    string ModelName { get; }

    Task<string> SendAsync(string prompt, CancellationToken cancellationToken = default);
}
