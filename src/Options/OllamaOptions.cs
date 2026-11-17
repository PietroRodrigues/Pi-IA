using System.ComponentModel.DataAnnotations;

namespace Pi.Options;

public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    [Required]
    [Url]
    public string BaseUrl { get; set; } = "http://127.0.0.1:11434";

    [Required(AllowEmptyStrings = false)]
    public string Model { get; set; } = "qwen2.5:3b";

    [Range(1, 3600)]
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Inicia <c>ollama serve</c> automaticamente quando o servidor local não responde.
    /// </summary>
    public bool AutoStart { get; set; } = true;

    /// <summary>
    /// Tempo máximo esperando o servidor responder depois de iniciado.
    /// </summary>
    [Range(1, 300)]
    public int StartupTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Caminho explícito do executável. Vazio = descoberta automática.
    /// </summary>
    public string? ExecutablePath { get; set; }

    /// <summary>
    /// Encerra o processo iniciado pelo Pi quando a aplicação sai.
    /// </summary>
    public bool StopOnExit { get; set; }
}
