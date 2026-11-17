using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pi.Application.Abstractions.AI;
using Pi.Application.Abstractions.Messaging;
using Pi.Application.Abstractions.Tools;
using Pi.Application.Options;
using Pi.Infrastructure.AI.Ollama;
using Pi.Infrastructure.Messaging;
using Pi.Infrastructure.Tools;

namespace Pi.Infrastructure;

public static class DependencyInjection
{
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
