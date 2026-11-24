// Registra a View da ponte: escritor exclusivo do stdout e fachada tipada.
// Sem --bridge stdio, este registro nunca é chamado.
using Microsoft.Extensions.DependencyInjection;
using Pi.View;

namespace Pi;

// Registro da View NDJSON. O BodyTools só roda com --bridge stdio.
public static class ViewRegistration
{
    public static IServiceCollection AddView(this IServiceCollection services)
    {
        services.AddSingleton(_ => new JsonLinesWriter(Console.Out));
        services.AddSingleton<BridgeView>();

        return services;
    }
}
