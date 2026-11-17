using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pi.Abstracts.AI;
using Pi.Abstracts.Messaging;
using Pi.Abstracts.Tools;
using Pi.Messaging;
using Pi.Ollama;
using Pi.Options;
using Pi.Services;
using Pi.Tools;
using Pi.UseCases;

namespace Pi;

/// <summary>
/// Registro central de serviços da aplicação.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AssistantOptions>>().Value;
            return new Pi.Conversation.Conversation(options.MaxHistoryMessages);
        });

        services.AddTransient<ToolRouter>();
        services.AddTransient<AgentService>();
        services.AddTransient<ProcessMessageUseCase>();

        return services;
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<OllamaOptions>()
            .Bind(configuration.GetSection(OllamaOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<ToolOptions>()
            .Bind(configuration.GetSection(ToolOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<ILLMClient, OllamaClient>();

        services.AddSingleton<IMessageBus, ChannelMessageBus>();

        services.AddTransient<ITool, SearchFilesTool>();
        services.AddTransient<ITool, OpenAppTool>();

        return services;
    }
}
