// Faz o binding das seções, aplica variáveis de ambiente e resolve workspace/log.
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pi.Contracts.Models;
using Pi.Core.Engine;
using Pi.Core.Policies;
using Pi.Host.Process;
using Pi.Infrastructure.Options;

namespace Pi.Host.Configuration;

public static class HostConfigurationLoader
{
    public static HostConfiguration Load(IConfiguration configuration)
    {
        var ollama = configuration.GetSection("Ollama").Get<OllamaSettings>() ?? new OllamaSettings();
        var agent = configuration.GetSection("Agent").Get<AgentRuntimeSettings>() ?? new AgentRuntimeSettings();
        var tools = configuration.GetSection("Tools").Get<ToolSettings>() ?? new ToolSettings();
        var sessions = configuration.GetSection("Sessions").Get<SessionSettings>() ?? new SessionSettings();

        ApplyEnvironment(ollama, agent, tools, sessions);

        var workspace = Path.GetFullPath(
            Environment.GetEnvironmentVariable("PI_WORKSPACE") ?? Directory.GetCurrentDirectory());
        var sandbox = new WorkspaceSandboxPolicy(workspace, tools.AllowedFolders, tools.AllowShell);

        var initialSession = SessionSnapshot.Create(
            agent.SessionId,
            ollama.Model,
            agent.AgentName,
            workspace,
            GitBranchReader.Read(workspace),
            agent.ContextWindowTokens);

        return new HostConfiguration(
            ollama,
            agent,
            tools,
            sessions,
            workspace,
            sandbox,
            new AgentSettings(
                agent.SystemPrompt,
                agent.ContextWindowTokens,
                agent.MaxHistoryMessages),
            initialSession,
            ParseLevel(Environment.GetEnvironmentVariable("PI_LOG_LEVEL")),
            ResolveLogPath());
    }

    private static void ApplyEnvironment(
        OllamaSettings ollama,
        AgentRuntimeSettings agent,
        ToolSettings tools,
        SessionSettings sessions)
    {
        ollama.BaseUrl = Environment.GetEnvironmentVariable("OLLAMA_BASE_URL") ?? ollama.BaseUrl;
        ollama.Model = Environment.GetEnvironmentVariable("PI_MODEL") ?? ollama.Model;
        ollama.ExecutablePath = Environment.GetEnvironmentVariable("OLLAMA_EXECUTABLE") ?? ollama.ExecutablePath;

        if (int.TryParse(Environment.GetEnvironmentVariable("PI_CONTEXT_WINDOW_TOKENS"), out var context))
        {
            agent.ContextWindowTokens = context;
        }

        sessions.Directory = Environment.GetEnvironmentVariable("PI_SESSION_DIR") ?? sessions.Directory;
        tools.AllowShell = Environment.GetEnvironmentVariable("PI_ALLOW_SHELL") is { } shell &&
                           (shell == "1" || shell.Equals("true", StringComparison.OrdinalIgnoreCase));
    }

    private static LogLevel? ParseLevel(string? value) =>
        Enum.TryParse<LogLevel>(value, ignoreCase: true, out var level) ? level : null;

    private static string ResolveLogPath()
    {
        var configured = Environment.GetEnvironmentVariable("PI_HOST_LOG") ??
                         Path.Combine(RepoRoot.Find(), "artifacts", "debug", "pi-host.log");

        var full = Path.GetFullPath(configured);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        return full;
    }
}
