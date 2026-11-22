using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pi.Backend.AI;
using Pi.Backend.Bootstrap;
using Pi.Backend.Chat;
using Pi.Backend.Tools;
using Pi.Frontend;
using Pi.Options;

namespace Pi;

/// <summary>
/// Registro central de serviços. É o único lugar que conhece Frontend e
/// Backend: as duas camadas não se referenciam diretamente.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBackend(this IServiceCollection services)
    {
        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AssistantOptions>>().Value;
            return new Conversation(options.MaxHistoryMessages);
        });

        services.AddTransient<ToolRouter>();
        services.AddTransient<AgentService>();
        services.AddTransient<ProcessMessage>();
        services.AddSingleton<ChatSession>();

        return services;
    }

    public static IServiceCollection AddFrontend(this IServiceCollection services)
    {
        services.AddSingleton<IChatView>(_ => new ConsoleChatView());

        return services;
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<AssistantOptions>()
            .Bind(configuration.GetSection(AssistantOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<OllamaOptions>()
            .Bind(configuration.GetSection(OllamaOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<ToolOptions>()
            .Bind(configuration.GetSection(ToolOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<ILLMClient, OllamaClient>();

        // Registrado antes do worker: o host aguarda o StartAsync deste serviço
        // antes de liberar a leitura do console.
        services.AddHttpClient(nameof(OllamaBootstrapper));
        services.AddSingleton<IOllamaProcessLauncher, OllamaProcessLauncher>();
        services.AddSingleton<IHostedService, OllamaBootstrapper>();

        services.AddTransient<ITool, SearchFilesTool>();
        services.AddTransient<ITool, OpenAppTool>();

        return services;
    }
}
