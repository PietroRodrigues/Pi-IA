namespace Pi.Application.Abstractions.AI;

/// <summary>
/// Porta para um modelo de linguagem.
/// </summary>
public interface ILLMClient
{
    Task<string> SendAsync(string prompt, CancellationToken cancellationToken = default);
}
