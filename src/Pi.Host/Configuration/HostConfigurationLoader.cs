// Faz o binding das seções, aplica variáveis de ambiente e resolve workspace/log.
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pi.Contracts.Constants;
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
        var ollama = configuration.GetSection(PiConstants.Configuration.OllamaSection).Get<OllamaSettings>()
                     ?? new OllamaSettings();
        var agent = configuration.GetSection(PiConstants.Configuration.AgentSection).Get<AgentRuntimeSettings>()
                    ?? new AgentRuntimeSettings();
        var tools = configuration.GetSection(PiConstants.Configuration.ToolsSection).Get<ToolSettings>()
                    ?? new ToolSettings();
        var sessions = configuration.GetSection(PiConstants.Configuration.SessionsSection).Get<SessionSettings>()
                       ?? new SessionSettings();

        ApplyEnvironment(ollama, agent, tools, sessions);

        var workspace = Path.GetFullPath(
            Environment.GetEnvironmentVariable(PiConstants.Env.PiWorkspace) ?? Directory.GetCurrentDirectory());
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
            ParseLevel(Environment.GetEnvironmentVariable(PiConstants.Env.PiLogLevel)),
            ResolveLogPath());
    }

    private static void ApplyEnvironment(
        OllamaSettings ollama,
        AgentRuntimeSettings agent,
        ToolSettings tools,
        SessionSettings sessions)
    {
        ollama.BaseUrl = Environment.GetEnvironmentVariable(PiConstants.Env.OllamaBaseUrl) ?? ollama.BaseUrl;
        ollama.Model = Environment.GetEnvironmentVariable(PiConstants.Env.PiModel) ?? ollama.Model;
        ollama.ExecutablePath =
            Environment.GetEnvironmentVariable(PiConstants.Env.OllamaExecutable) ?? ollama.ExecutablePath;
        ollama.AutoStart = ParseBool(
            Environment.GetEnvironmentVariable(PiConstants.Env.OllamaAutoStart),
            ollama.AutoStart);
        ollama.StopOnExit = ParseBool(
            Environment.GetEnvironmentVariable(PiConstants.Env.OllamaStopOnExit),
            ollama.StopOnExit);

        if (int.TryParse(
                Environment.GetEnvironmentVariable(PiConstants.Env.PiContextWindowTokens),
                out var context))
        {
            agent.ContextWindowTokens = context;
        }

        sessions.Directory =
            Environment.GetEnvironmentVariable(PiConstants.Env.PiSessionDir) ?? sessions.Directory;
        tools.AllowShell = ParseBool(
            Environment.GetEnvironmentVariable(PiConstants.Env.PiAllowShell),
            tools.AllowShell);
    }

    private static bool ParseBool(string? value, bool fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        if (value is PiConstants.Protocol.TrueValue ||
            value.Equals(PiConstants.Protocol.TrueWord, StringComparison.OrdinalIgnoreCase)) return true;
        if (value is PiConstants.Protocol.FalseValue ||
            value.Equals(PiConstants.Protocol.FalseWord, StringComparison.OrdinalIgnoreCase)) return false;
        return fallback;
    }

    private static LogLevel? ParseLevel(string? value) =>
        Enum.TryParse<LogLevel>(value, ignoreCase: true, out var level) ? level : null;

    private static string ResolveLogPath()
    {
        var configured = Environment.GetEnvironmentVariable(PiConstants.Env.PiHostLog) ??
                         Path.Combine(
                             RepoRoot.Find(),
                             PiConstants.Paths.ArtifactsFolderName,
                             PiConstants.Paths.DebugFolderName,
                             PiConstants.Paths.HostLogFileName);

        var full = Path.GetFullPath(configured);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        return full;
    }
}
