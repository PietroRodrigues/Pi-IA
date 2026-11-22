using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pi.Backend.Chat;
using Pi.Frontend.Bridge;

namespace Pi;

/// <summary>
/// Registro do frontend NDJSON. Chamado apenas com <c>--bridge stdio</c>.
/// </summary>
public static class BridgeRegistration
{
    public static IServiceCollection AddBridge(this IServiceCollection services)
    {
        // Necessário para o ValidateOnBuild do ambiente Development. A saída
        // continua exclusivamente no JsonLinesWriter (stdout NDJSON).
        services.AddSingleton<IChatView, NullChatView>();
        services.AddSingleton(_ => new JsonLinesWriter(Console.Out));
        services.AddSingleton(provider =>
            ActivatorUtilities.CreateInstance<StdioBridgeWorker>(provider, Console.In));
        services.AddHostedService(provider => provider.GetRequiredService<StdioBridgeWorker>());

        return services;
    }
}
