using System.ComponentModel.DataAnnotations;

namespace Pi.Application.Options;

public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    [Required]
    [Url]
    public string BaseUrl { get; set; } = "http://localhost:11434";

    [Required(AllowEmptyStrings = false)]
    public string Model { get; set; } = "qwen2.5:3b";

    [Range(1, 3600)]
    public int TimeoutSeconds { get; set; } = 60;
}
