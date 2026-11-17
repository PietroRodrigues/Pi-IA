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

builder.Services.AddHostedService<ConsoleInputWorker>();
builder.Services.AddHostedService<MessageProcessingWorker>();

var host = builder.Build();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    host.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
};

await host.RunAsync();
