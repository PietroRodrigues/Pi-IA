namespace Pi.Core.Contracts;

/// <summary>
/// Extensão opcional para modelos que conseguem transmitir a resposta aos poucos.
/// O cliente textual antigo continua usando apenas <see cref="ILLMClient"/>.
/// </summary>
public interface IStreamingLLMClient : ILLMClient
{
    IAsyncEnumerable<LLMStreamChunk> StreamAsync(
        string prompt,
        CancellationToken cancellationToken = default);
}
