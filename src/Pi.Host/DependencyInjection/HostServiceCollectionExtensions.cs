// Registros do Composition Root agrupados por responsabilidade.
using Microsoft.Extensions.DependencyInjection;
using Pi.Contracts.Protocol;
using Pi.Core.Abstractions;
using Pi.Core.Engine;
using Pi.Core.Policies;
using Pi.Host.Bridge;
using Pi.Host.Configuration;
using Pi.Infrastructure.Llm;
using Pi.Infrastructure.Persistence;
using Pi.Infrastructure.Tools;

namespace Pi.Host.DependencyInjection;

public static class HostServiceCollectionExtensions
{
    public static IServiceCollection AddPiHostCore(
        this IServiceCollection services,
        HostConfiguration configuration)
    {
        services.AddSingleton(configuration.Ollama);
        services.AddSingleton(configuration.Agent);
        services.AddSingleton(configuration.Tools);
        services.AddSingleton(configuration.Sessions);
        services.AddSingleton(configuration.Sandbox);
        services.AddSingleton<IPermissionPolicy>(configuration.Sandbox);
        services.AddSingleton(configuration.InitialSession);
        services.AddSingleton(configuration.AgentCore);
        services.AddSingleton<ContextBudgetManager>();
        services.AddSingleton<ISessionRepository, JsonSessionRepository>();
        return services;
    }

    public static IServiceCollection AddOllamaIntegration(
        this IServiceCollection services,
        HostConfiguration configuration)
    {
        // Um único HttpClient: mesma BaseAddress e timeout de ambos os clients antigos.
        services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri(configuration.Ollama.BaseUrl),
            Timeout = Timeout.InfiniteTimeSpan
        });

        services.AddSingleton<ILlmProvider, OllamaProvider>();
        services.AddSingleton<IOllamaProcessLauncher, OllamaProcessLauncher>();
        services.AddSingleton<OllamaBootstrapper>();
        return services;
    }

    public static IServiceCollection AddAgentTools(
        this IServiceCollection services,
        HostConfiguration configuration)
    {
        services.AddSingleton<IAgentTool, SearchCodeTool>();
        services.AddSingleton<IAgentTool, ReadFileTool>();
        services.AddSingleton<IAgentTool, EditFileTool>();
        services.AddSingleton<IAgentTool, ExecuteShellTool>();

        var options = new ToolExecutionOptions(
            30,
            Math.Max(1, configuration.Tools.ShellTimeoutSeconds));
        services.AddSingleton(options);

        // O container preenche tools, política, eventos e sandbox automaticamente.
        services.AddSingleton(provider =>
            ActivatorUtilities.CreateInstance<ToolExecutionCoordinator>(provider, options));
        return services;
    }

    public static IServiceCollection AddHostBridge(this IServiceCollection services)
    {
        services.AddSingleton(new NdjsonReader(Console.In));
        services.AddSingleton(new NdjsonWriter(Console.Out));
        services.AddSingleton<IEventSink, NdjsonEventSink>();
        services.AddSingleton<AgentOrchestrator>();
        services.AddSingleton<CommandDispatcher>();
        return services;
    }
}
