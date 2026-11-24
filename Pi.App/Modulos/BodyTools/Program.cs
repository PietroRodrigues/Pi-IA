// Ponto de entrada do backend. Aqui só existe o modo ponte: a TUI é a interface,
// e este processo fica limitado a NDJSON no stdout e logs no stderr.
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pi;

var (useBridge, hostArgs) = ParseArgs(args);

// A TUI é a única interface: sem a ponte não há quem leia stdin/escreva stdout.
// O host nem é criado nesse caminho, então ValidateOnBuild não entra em jogo.
if (!useBridge)
{
    Console.Error.WriteLine("Uso: dotnet BodyTools.dll --bridge stdio");
    Console.Error.WriteLine("A interface fica em Pi.App/Modulos/interfaceTui/: rode 'bun run dev' dentro dela.");
    return 1;
}

// Ancora a configuração no diretório de execução, independentemente do cwd.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = hostArgs,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Services.AddModel();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddView();
builder.Services.AddController();

var host = builder.Build();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    host.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
};

await host.RunAsync();
return 0;

// Aceita as duas formas da flag; o que sobrar vai para o host (configuração).
static (bool UseBridge, string[] HostArgs) ParseArgs(string[] args)
{
    var useBridge = false;
    var hostArgs = new List<string>();

    for (var index = 0; index < args.Length; index++)
    {
        var argument = args[index];

        if (argument.Equals("--bridge", StringComparison.OrdinalIgnoreCase))
        {
            useBridge = true;

            if (index + 1 < args.Length &&
                args[index + 1].Equals("stdio", StringComparison.OrdinalIgnoreCase))
            {
                index++;
            }

            continue;
        }

        if (argument.StartsWith("--bridge=", StringComparison.OrdinalIgnoreCase))
        {
            useBridge = true;
            continue;
        }

        hostArgs.Add(argument);
    }

    return (useBridge, hostArgs.ToArray());
}
