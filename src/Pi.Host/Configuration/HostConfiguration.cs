// Configuração já resolvida do host: evita reler env/variáveis espalhadas pelo Program.
using Microsoft.Extensions.Logging;
using Pi.Contracts.Models;
using Pi.Core.Engine;
using Pi.Core.Policies;
using Pi.Host.Logging;
using Pi.Infrastructure.Options;

namespace Pi.Host.Configuration;

public sealed record HostConfiguration(
    OllamaSettings Ollama,
    AgentRuntimeSettings Agent,
    ToolSettings Tools,
    SessionSettings Sessions,
    string Workspace,
    WorkspaceSandboxPolicy Sandbox,
    AgentSettings AgentCore,
    SessionSnapshot InitialSession,
    LogLevel? MinimumLevel,
    string LogPath)
{
    public void ConfigureLogging(ILoggingBuilder logging)
    {
        if (MinimumLevel is { } level)
        {
            logging.SetMinimumLevel(level);
        }

        logging.ClearProviders();
        logging.AddProvider(new HostFileLoggerProvider(LogPath));
    }
}
