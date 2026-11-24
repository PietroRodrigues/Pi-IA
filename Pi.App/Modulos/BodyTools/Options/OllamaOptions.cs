// Configuração do servidor local e do modelo, da seção "Ollama".
using System.ComponentModel.DataAnnotations;

namespace Pi.Options;

public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    [Required]
    [Url]
    public string BaseUrl { get; set; } = "http://127.0.0.1:11434";

    [Required(AllowEmptyStrings = false)]
    public string Model { get; set; } = "qwen3.8-2b-local";

    [Range(1, 3600)]
    public int TimeoutSeconds { get; set; } = 60;

    // Inicia ollama serve automaticamente quando o servidor local não responde.
    public bool AutoStart { get; set; } = true;

    // Tempo máximo esperando o servidor responder depois de iniciado.
    [Range(1, 300)]
    public int StartupTimeoutSeconds { get; set; } = 30;

    // Caminho explícito do executável. Vazio = descoberta automática.
    public string? ExecutablePath { get; set; }

    // Encerra o processo iniciado pelo Pi quando a aplicação sai.
    public bool StopOnExit { get; set; }
}
