// Configurações tipadas dos adaptadores. O Host faz o binding e injeta estas instâncias.
using Pi.Contracts.Constants;

namespace Pi.Infrastructure.Options;

public sealed class OllamaSettings
{
    public string BaseUrl { get; set; } = PiConstants.Defaults.BaseUrl;
    public string Model { get; set; } = PiConstants.Defaults.Model;
    public int TimeoutSeconds { get; set; } = PiConstants.Defaults.TimeoutSeconds;
    public bool AutoStart { get; set; } = PiConstants.Defaults.AutoStart;
    public int StartupTimeoutSeconds { get; set; } = PiConstants.Defaults.StartupTimeoutSeconds;
    public string? ExecutablePath { get; set; }
    public bool StopOnExit { get; set; } = PiConstants.Defaults.StopOnExit;
}

public sealed class AgentRuntimeSettings
{
    public string SystemPrompt { get; set; } = PiConstants.Defaults.SystemPrompt;

    public int ContextWindowTokens { get; set; } = PiConstants.Defaults.ContextWindowTokens;
    public int MaxHistoryMessages { get; set; } = PiConstants.Defaults.MaxHistoryMessages;
    public string AgentName { get; set; } = PiConstants.Defaults.AgentName;
    public string SessionId { get; set; } = PiConstants.Defaults.SessionId;
}

public sealed class ToolSettings
{
    public string[] AllowedFolders { get; set; } = [];
    public int MaxResults { get; set; } = PiConstants.Defaults.MaxResults;
    public int SearchTimeoutSeconds { get; set; } = PiConstants.Defaults.SearchTimeoutSeconds;
    public int MaxReadBytes { get; set; } = PiConstants.Defaults.MaxReadBytes;
    public int MaxOutputCharacters { get; set; } = PiConstants.Defaults.MaxOutputCharacters;
    public int ShellTimeoutSeconds { get; set; } = PiConstants.Defaults.ShellTimeoutSeconds;
    public bool AllowShell { get; set; } = PiConstants.Defaults.AllowShell;
}

public sealed class SessionSettings
{
    public string Directory { get; set; } = PiConstants.Defaults.SessionDirectory;
}
