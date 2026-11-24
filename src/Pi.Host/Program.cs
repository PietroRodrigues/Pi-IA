// Composition Root do Pi.Host. Dois modos: sem flags supervisiona a TUI;
// com --bridge stdio vira o backend NDJSON usado pela própria TUI.
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pi.Core.Engine;
using Pi.Host.Bridge;
using Pi.Host.Configuration;
using Pi.Host.DependencyInjection;
using Pi.Host.Process;
using Pi.Infrastructure.Llm;

EnvFileLoader.Load();

var cli = CliArgumentsParser.Parse(args);
if (!cli.UseBridge)
{
    return await new TuiProcessSupervisor().RunAsync();
}

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = cli.HostArgs,
    ContentRootPath = AppContext.BaseDirectory
});

var configuration = HostConfigurationLoader.Load(builder.Configuration);
configuration.ConfigureLogging(builder.Logging);

builder.Services
    .AddPiHostCore(configuration)
    .AddOllamaIntegration(configuration)
    .AddAgentTools(configuration)
    .AddHostBridge();

var host = builder.Build();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    host.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
};

await host.StartAsync();

try
{
    var bootstrapper = host.Services.GetRequiredService<OllamaBootstrapper>();
    await bootstrapper.StartAsync(CancellationToken.None);

    await host.Services.GetRequiredService<AgentOrchestrator>().InitializeAsync(CancellationToken.None);

    var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
    await host.Services.GetRequiredService<CommandDispatcher>().RunAsync(lifetime.ApplicationStopping);
}
finally
{
    await host.StopAsync(CancellationToken.None);
    host.Services.GetRequiredService<OllamaBootstrapper>().StopOnExit();
}

return 0;
