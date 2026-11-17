using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NAudio.Wave;
using Pi.Application.Abstractions.Voice;
using Pi.Application.Options;

namespace Pi.Infrastructure.Voice;

/// <summary>
/// Captura áudio do microfone com NAudio e grava em um arquivo temporário único.
/// </summary>
public sealed class NaudioAudioCapture : IAudioCapture
{
    private readonly AudioOptions _options;
    private readonly ILogger<NaudioAudioCapture> _logger;

    public NaudioAudioCapture(IOptions<AudioOptions> options, ILogger<NaudioAudioCapture> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> CaptureAsync(int durationMs, CancellationToken cancellationToken = default)
    {
        if (durationMs <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMs));
        }

        var outputPath = BuildTempPath();

        using var waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(_options.SampleRate, _options.Channels)
        };

        WaveFileWriter? writer = null;
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        EventHandler<WaveInEventArgs> dataHandler = (_, args) =>
            writer?.Write(args.Buffer, 0, args.BytesRecorded);

        EventHandler<StoppedEventArgs> stopHandler = (_, _) =>
        {
            writer?.Dispose();
            writer = null;
            stopped.TrySetResult();
        };

        waveIn.DataAvailable += dataHandler;
        waveIn.RecordingStopped += stopHandler;

        var succeeded = false;

        try
        {
            writer = new WaveFileWriter(outputPath, waveIn.WaveFormat);

            _logger.LogDebug("Gravando áudio por {Duration} ms em {Path}", durationMs, outputPath);
            waveIn.StartRecording();

            try
            {
                await Task.Delay(durationMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                waveIn.StopRecording();
                await WaitForStopAsync(stopped);
                throw;
            }

            waveIn.StopRecording();
            await WaitForStopAsync(stopped);

            succeeded = true;
            return outputPath;
        }
        finally
        {
            waveIn.DataAvailable -= dataHandler;
            waveIn.RecordingStopped -= stopHandler;
            writer?.Dispose();

            if (!succeeded)
            {
                TryDelete(outputPath);
            }
        }
    }

    private string BuildTempPath()
    {
        var directory = Path.GetDirectoryName(_options.TempFilePath);

        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = AppContext.BaseDirectory;
        }

        Directory.CreateDirectory(directory);

        var name = Path.GetFileNameWithoutExtension(_options.TempFilePath);
        var extension = Path.GetExtension(_options.TempFilePath);

        return Path.Combine(directory, $"{name}_{Guid.NewGuid():N}{extension}");
    }

    private async Task WaitForStopAsync(TaskCompletionSource stopped)
    {
        try
        {
            await stopped.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("O dispositivo de áudio não sinalizou o fim da gravação.");
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
            _logger.LogWarning(ex, "Não foi possível remover {Path}", path);
        }
    }
}
