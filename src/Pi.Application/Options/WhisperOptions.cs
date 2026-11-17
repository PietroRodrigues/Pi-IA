using System.ComponentModel.DataAnnotations;

namespace Pi.Application.Options;

public sealed class WhisperOptions
{
    public const string SectionName = "Whisper";

    [Required(AllowEmptyStrings = false)]
    public string ModelPath { get; set; } = Path.Combine("VoiceModels", "ggml-base.bin");

    [Required(AllowEmptyStrings = false)]
    public string Language { get; set; } = "pt";

    [Range(0, 128)]
    public int Threads { get; set; }
}
