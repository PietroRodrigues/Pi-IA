namespace Pi.Core.Contracts;

/// <summary>
/// Porta para um modelo de linguagem.
/// </summary>
public interface ILLMClient
{
    /// <summary>
    /// Nome do modelo em uso, para exibição na interface.
    /// </summary>
    string ModelName { get; }

    Task<string> SendAsync(string prompt, CancellationToken cancellationToken = default);
}
