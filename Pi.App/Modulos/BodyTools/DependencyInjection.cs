// O mapa de serviços da aplicação. As camadas só se encontram aqui.
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pi.Model.Contracts;
using Pi.Model.Connections.Ollama;
using Pi.Model.Chat;
using Pi.Model.Tools;
using Pi.Options;

namespace Pi;

// Registro central de serviços. É o único lugar que conhece todas as camadas:
// elas não se referenciam diretamente.
public static class DependencyInjection
{
    public static IServiceCollection AddModel(this IServiceCollection services)
    {
        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AssistantOptions>>().Value;
            return new Conversation(options.MaxHistoryMessages);
        });

        services.AddTransient<ToolRouter>();
        services.AddTransient<AgentService>();

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

        // Registrado antes do Controller: o host aguarda o StartAsync deste serviço
        // antes de liberar a leitura do console.
        services.AddHttpClient(nameof(OllamaBootstrapper));
        services.AddSingleton<IOllamaProcessLauncher, OllamaProcessLauncher>();
        services.AddSingleton<IHostedService, OllamaBootstrapper>();

        services.AddTransient<ITool, SearchFilesTool>();
        services.AddTransient<ITool, OpenAppTool>();

        return services;
    }
}
