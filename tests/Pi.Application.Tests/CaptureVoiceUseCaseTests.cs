using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Pi.Application.Abstractions.Voice;
using Pi.Application.Options;
using Pi.Application.UseCases;
using Xunit;

namespace Pi.Application.Tests;

public class CaptureVoiceUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsVoiceMessageForValidTranscription()
    {
        var audioPath = Path.GetTempFileName();
        var (sut, _, _) = CreateSut(audioPath, "olá pi");

        var result = await sut.ExecuteAsync();

        Assert.NotNull(result);
        Assert.Equal("olá pi", result.Text);
        Assert.Equal(Pi.Domain.Conversation.MessageSource.Voice, result.Source);
        Assert.False(File.Exists(audioPath));
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsNullForEmptyTranscription()
    {
        var audioPath = Path.GetTempFileName();
        var (sut, _, _) = CreateSut(audioPath, "   ");

        var result = await sut.ExecuteAsync();

        Assert.Null(result);
        Assert.False(File.Exists(audioPath));
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsNullForWhisperNoiseTag()
    {
        var audioPath = Path.GetTempFileName();
        var (sut, _, _) = CreateSut(audioPath, "[Música]");

        var result = await sut.ExecuteAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task ExecuteAsync_UsesConfiguredCaptureDuration()
    {
        var audioPath = Path.GetTempFileName();
        var (sut, capture, _) = CreateSut(audioPath, "teste");

        await sut.ExecuteAsync();

        await capture.Received(1).CaptureAsync(1500, Arg.Any<CancellationToken>());
    }

    private static (
        CaptureVoiceUseCase UseCase,
        IAudioCapture Capture,
        ISpeechToText Speech) CreateSut(string audioPath, string transcription)
    {
        var capture = Substitute.For<IAudioCapture>();
        capture.CaptureAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(audioPath);

        var speech = Substitute.For<ISpeechToText>();
        speech.TranscribeAsync(audioPath, Arg.Any<CancellationToken>()).Returns(transcription);

        var options = Microsoft.Extensions.Options.Options.Create(new AudioOptions { CaptureDurationMs = 1500 });
        var logger = Substitute.For<ILogger<CaptureVoiceUseCase>>();

        return (new CaptureVoiceUseCase(capture, speech, options, logger), capture, speech);
    }
}
