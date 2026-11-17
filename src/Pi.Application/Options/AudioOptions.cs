using System.ComponentModel.DataAnnotations;

namespace Pi.Application.Options;

public sealed class AudioOptions
{
    public const string SectionName = "Audio";

    [Range(8000, 48000)]
    public int SampleRate { get; set; } = 16000;

    [Range(1, 2)]
    public int Channels { get; set; } = 1;

    [Range(100, 60000)]
    public int CaptureDurationMs { get; set; } = 5000;

    [Required(AllowEmptyStrings = false)]
    public string TempFilePath { get; set; } = "recording.wav";
}
