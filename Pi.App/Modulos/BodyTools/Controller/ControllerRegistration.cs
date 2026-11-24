// Registra o Controller da ponte como hosted service. Vem depois da View e
// depois do bootstrap do Ollama para manter a ordem de StartAsync do host.
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pi.Controller;

namespace Pi;

// Registro do Controller NDJSON. O BodyTools só roda com --bridge stdio.
public static class ControllerRegistration
{
    public static IServiceCollection AddController(this IServiceCollection services)
    {
        services.AddSingleton(provider =>
            ActivatorUtilities.CreateInstance<BridgeController>(provider, Console.In));
        services.AddHostedService(provider => provider.GetRequiredService<BridgeController>());

        return services;
    }
}
