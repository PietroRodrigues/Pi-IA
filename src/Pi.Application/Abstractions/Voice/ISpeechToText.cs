namespace Pi.Application.Abstractions.Voice;

/// <summary>
/// Porta para transcrição de áudio em texto.
/// </summary>
public interface ISpeechToText
{
    Task<string> TranscribeAsync(string audioPath, CancellationToken cancellationToken = default);
}
