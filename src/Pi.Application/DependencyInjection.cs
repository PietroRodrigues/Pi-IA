using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pi.Application.Options;
using Pi.Application.Services;
using Pi.Application.Tools;
using Pi.Application.UseCases;
using Pi.Domain.Conversation;

namespace Pi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AssistantOptions>>().Value;
            return new Conversation(options.MaxHistoryMessages);
        });

        services.AddTransient<ToolRouter>();
        services.AddTransient<AgentService>();
        services.AddTransient<ProcessMessageUseCase>();

        return services;
    }
}
