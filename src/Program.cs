using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pi;
using Pi.Workers;

// Ancora a configuração no diretório de execução, independentemente do cwd.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// O OllamaServerBootstrapper vem de AddInfrastructure e é registrado antes do
// worker do chat, então o console só começa a ler depois que o Ollama responde.
builder.Services.AddHostedService<ChatSessionWorker>();

var host = builder.Build();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    host.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
};

await host.RunAsync();
