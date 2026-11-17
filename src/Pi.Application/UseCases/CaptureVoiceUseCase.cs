using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pi.Application.Abstractions.Voice;
using Pi.Application.Models;
using Pi.Application.Options;

namespace Pi.Application.UseCases;

/// <summary>
/// Captura áudio, transcreve e devolve uma mensagem de voz válida.
/// </summary>
public sealed class CaptureVoiceUseCase
{
    private readonly IAudioCapture _audioCapture;
    private readonly ISpeechToText _speechToText;
    private readonly AudioOptions _options;
    private readonly ILogger<CaptureVoiceUseCase> _logger;

    public CaptureVoiceUseCase(
        IAudioCapture audioCapture,
        ISpeechToText speechToText,
        IOptions<AudioOptions> options,
        ILogger<CaptureVoiceUseCase> logger)
    {
        ArgumentNullException.ThrowIfNull(audioCapture);
        ArgumentNullException.ThrowIfNull(speechToText);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _audioCapture = audioCapture;
        _speechToText = speechToText;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AssistantMessage?> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        string? audioPath = null;

        try
        {
            audioPath = await _audioCapture.CaptureAsync(_options.CaptureDurationMs, cancellationToken);

            var text = (await _speechToText.TranscribeAsync(audioPath, cancellationToken)).Trim();

            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            // Ruído comum do Whisper em áudio sem fala.
            if (text.StartsWith('[') && text.EndsWith(']'))
            {
                return null;
            }

            return AssistantMessage.ForVoice(text);
        }
        finally
        {
            if (audioPath is not null)
            {
                TryDelete(audioPath);
            }
        }
    }

    private void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível remover o arquivo temporário {Path}", path);
        }
    }
}
