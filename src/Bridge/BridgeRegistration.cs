using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pi.Bridge;

namespace Pi;

/// <summary>
/// Registro da ponte NDJSON. O Pi.CLI só roda com <c>--bridge stdio</c>.
/// </summary>
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
