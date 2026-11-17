namespace Pi.Application.Abstractions.Voice;

/// <summary>
/// Porta para captura de áudio do microfone.
/// </summary>
public interface IAudioCapture
{
    /// <summary>
    /// Grava áudio e devolve o caminho do arquivo temporário.
    /// </summary>
    Task<string> CaptureAsync(int durationMs, CancellationToken cancellationToken = default);
}
