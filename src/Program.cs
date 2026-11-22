using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pi;
using Pi.Backend.Chat;

var (useBridge, hostArgs) = ParseArgs(args);

// Ancora a configuração no diretório de execução, independentemente do cwd.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = hostArgs,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Services.AddBackend();
builder.Services.AddInfrastructure(builder.Configuration);

// O modo padrão continua sendo a CLI textual. A ponte NDJSON é opt-in e
// somente ela troca o IChatView pelo worker de stdin/stdout.
if (useBridge)
{
    builder.Services.AddBridge();
}
else
{
    builder.Services.AddFrontend();

    // O OllamaBootstrapper vem de AddInfrastructure e é registrado antes do worker,
    // então o console só começa a ler depois que o Ollama responde.
    builder.Services.AddHostedService<ChatWorker>();
}

var host = builder.Build();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    host.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
};

await host.RunAsync();

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
