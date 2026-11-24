// Contrato opcional: a resposta chega aos poucos. Sem ele, vem inteira de uma vez.
namespace Pi.Model.Contracts;

// Extensão para modelos que conseguem transmitir a resposta aos poucos.
// Quem não implementa isso entrega o texto inteiro de uma vez.
public interface IStreamingLLMClient : ILLMClient
{
    IAsyncEnumerable<LLMStreamChunk> StreamAsync(
        string prompt,
        CancellationToken cancellationToken = default);
}
