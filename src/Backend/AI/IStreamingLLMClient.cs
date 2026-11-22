namespace Pi.Backend.AI;

/// <summary>
/// Extensão opcional para modelos que conseguem transmitir a resposta aos poucos.
/// O cliente textual antigo continua usando apenas <see cref="ILLMClient"/>.
/// </summary>
public interface IStreamingLLMClient : ILLMClient
{
    IAsyncEnumerable<OllamaStreamChunk> StreamAsync(
        string prompt,
        CancellationToken cancellationToken = default);
}
