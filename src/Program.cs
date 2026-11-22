using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pi;

var (useBridge, hostArgs) = ParseArgs(args);

// A TUI é a única interface: sem a ponte não há quem leia stdin/escreva stdout.
// O host nem é criado nesse caminho, então ValidateOnBuild não entra em jogo.
if (!useBridge)
{
    Console.Error.WriteLine("Uso: dotnet Pi.CLI.dll --bridge stdio");
    Console.Error.WriteLine("A interface fica em tui/: rode 'bun run dev' dentro de tui/.");
    return 1;
}

// Ancora a configuração no diretório de execução, independentemente do cwd.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = hostArgs,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Services.AddBackend();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddBridge();

var host = builder.Build();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    host.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
};

await host.RunAsync();
return 0;

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
