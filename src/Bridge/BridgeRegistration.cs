// Registra o que a ponte precisa: writer, worker e o hosted service.
// Sem --bridge stdio, este registro nunca é chamado.
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pi.Bridge;

namespace Pi;

// Registro da ponte NDJSON. O Pi.CLI só roda com --bridge stdio.
public static class BridgeRegistration
{
    public static IServiceCollection AddBridge(this IServiceCollection services)
    {
        services.AddSingleton(_ => new JsonLinesWriter(Console.Out));
        services.AddSingleton(provider =>
            ActivatorUtilities.CreateInstance<StdioBridgeWorker>(provider, Console.In));
        services.AddHostedService(provider => provider.GetRequiredService<StdioBridgeWorker>());

        return services;
    }
}
