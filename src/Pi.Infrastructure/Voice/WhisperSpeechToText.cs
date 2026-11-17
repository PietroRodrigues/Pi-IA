using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pi.Application.Abstractions.Voice;
using Pi.Application.Options;
using Whisper.net;

namespace Pi.Infrastructure.Voice;

/// <summary>
/// Transcreve áudio usando Whisper.net.
/// </summary>
public sealed class WhisperSpeechToText : ISpeechToText
{
    private readonly WhisperOptions _options;
    private readonly ILogger<WhisperSpeechToText> _logger;

    public WhisperSpeechToText(IOptions<WhisperOptions> options, ILogger<WhisperSpeechToText> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> TranscribeAsync(string audioPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(audioPath);

        if (!File.Exists(audioPath))
        {
            throw new FileNotFoundException("Arquivo de áudio não encontrado.", audioPath);
        }

        var threads = _options.Threads > 0 ? _options.Threads : Environment.ProcessorCount;

        using var factory = WhisperFactory.FromPath(_options.ModelPath);
        using var audioStream = File.OpenRead(audioPath);
        using var processor = factory
            .CreateBuilder()
            .WithLanguage(_options.Language)
            .WithThreads(threads)
            .Build();

        var transcript = new StringBuilder();

        await foreach (var segment in processor.ProcessAsync(audioStream, cancellationToken))
        {
            transcript.Append(segment.Text);
        }

        _logger.LogDebug("Transcrição concluída: {Length} caracteres", transcript.Length);

        return transcript.ToString();
    }
}
