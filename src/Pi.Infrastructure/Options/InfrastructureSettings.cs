// Configurações tipadas dos adaptadores. O Host faz o binding e injeta estas instâncias.
namespace Pi.Infrastructure.Options;

public sealed class OllamaSettings
{
    public string BaseUrl { get; set; } = "http://127.0.0.1:11434";
    public string Model { get; set; } = "qwen3.8-2b-local";
    public int TimeoutSeconds { get; set; } = 60;
    public bool AutoStart { get; set; } = true;
    public int StartupTimeoutSeconds { get; set; } = 30;
    public string? ExecutablePath { get; set; }
    public bool StopOnExit { get; set; }
}

public sealed class AgentRuntimeSettings
{
    public string SystemPrompt { get; set; } =
        "Você é Pi, um assistente local. Fale português do Brasil, seja claro e não invente informações.";

    public int ContextWindowTokens { get; set; } = 32768;
    public int MaxHistoryMessages { get; set; } = 20;
    public string AgentName { get; set; } = "geral";
    public string SessionId { get; set; } = "default";
}

public sealed class ToolSettings
{
    public string[] AllowedFolders { get; set; } = [];
    public int MaxResults { get; set; } = 20;
    public int SearchTimeoutSeconds { get; set; } = 10;
    public int MaxReadBytes { get; set; } = 65536;
    public int MaxOutputCharacters { get; set; } = 8000;
    public int ShellTimeoutSeconds { get; set; } = 15;
    public bool AllowShell { get; set; }
}

public sealed class SessionSettings
{
    public string Directory { get; set; } = ".pi-ia/sessions";
}
