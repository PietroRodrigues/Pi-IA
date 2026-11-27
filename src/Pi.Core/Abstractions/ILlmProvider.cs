// Contrato de inferência com streaming e cancelamento. O domínio não conhece HTTP.
using System.Runtime.CompilerServices;
using Pi.Contracts.Constants;

namespace Pi.Core.Abstractions;

public sealed record LlmChunk(
    string Text,
    bool Done,
    int? PromptEvalCount = null,
    int? EvalCount = null);

public interface ILlmProvider
{
    string ModelName { get; }

    Task<string> SendAsync(string prompt, CancellationToken cancellationToken = default);

    IAsyncEnumerable<LlmChunk> StreamAsync(
        string prompt,
        CancellationToken cancellationToken = default);

    // Implementação padrão mantém providers antigos funcionando sem tool calling.
    async IAsyncEnumerable<LlmResponseChunk> StreamAsync(
        IReadOnlyList<LlmMessage> messages,
        IReadOnlyList<LlmToolDefinition> tools,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var prompt = string.Join(
            Environment.NewLine,
            messages.Select(message =>
                string.Format(PiConstants.Prompt.MessageTemplate, message.Role, message.Content)));

        await foreach (var chunk in StreamAsync(prompt, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return new LlmResponseChunk(
                chunk.Text,
                chunk.Done,
                [],
                chunk.PromptEvalCount,
                chunk.EvalCount);
        }
    }
}
