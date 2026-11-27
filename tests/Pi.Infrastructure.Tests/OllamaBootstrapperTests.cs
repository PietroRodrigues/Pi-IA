// O Pi reutiliza o Ollama já aberto; só inicia outro quando não há instância.
using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Pi.Infrastructure.Llm;
using Pi.Infrastructure.Options;
using Xunit;

namespace Pi.Infrastructure.Tests;

public class OllamaBootstrapperTests
{
    [Fact]
    public async Task StartAsync_InstanciaPronta_NaoIniciaOutra()
    {
        var launcher = new FakeLauncher(OllamaLaunchResult.Fail("não deveria iniciar"));
        var discovery = new FakeDiscovery([]);
        var sut = Create(new OllamaSettings(), discovery, launcher, new ProbeHandler());

        await sut.StartAsync(CancellationToken.None);

        Assert.Equal(0, launcher.Calls);
        Assert.Equal(0, discovery.Calls);
    }

    [Fact]
    public async Task StartAsync_ProcessoLocalEmSubida_EsperaSemIniciarOutra()
    {
        var existing = StartLongRunningProcess();
        try
        {
            var launcher = new FakeLauncher(OllamaLaunchResult.Fail("não deveria iniciar"));
            var discovery = new FakeDiscovery([existing]);
            var sut = Create(
                new OllamaSettings { StartupTimeoutSeconds = 5 },
                discovery,
                launcher,
                new ProbeHandler(failuresBeforeSuccess: 2));

            await sut.StartAsync(CancellationToken.None);

            Assert.Equal(0, launcher.Calls);
            Assert.Equal(1, discovery.Calls);
        }
        finally
        {
            Cleanup(existing);
        }
    }

    [Fact]
    public async Task StartAsync_SemProcessoLocal_IniciaOllama()
    {
        var owned = StartLongRunningProcess();
        try
        {
            var launcher = new FakeLauncher(OllamaLaunchResult.Ok(owned));
            var sut = Create(
                new OllamaSettings { StartupTimeoutSeconds = 1 },
                new FakeDiscovery([]),
                launcher,
                new ProbeHandler(failuresBeforeSuccess: int.MaxValue));

            await sut.StartAsync(CancellationToken.None);

            Assert.Equal(1, launcher.Calls);
        }
        finally
        {
            Cleanup(owned);
        }
    }

    [Fact]
    public async Task StopOnExit_Padrao_NaoEncerraNemOMesmoProcessoIniciado()
    {
        var owned = StartLongRunningProcess();
        try
        {
            var sut = Create(
                new OllamaSettings { StartupTimeoutSeconds = 1 },
                new FakeDiscovery([]),
                new FakeLauncher(OllamaLaunchResult.Ok(owned)),
                new ProbeHandler(failuresBeforeSuccess: int.MaxValue));

            await sut.StartAsync(CancellationToken.None);
            sut.StopOnExit();

            Assert.False(owned.HasExited);
        }
        finally
        {
            Cleanup(owned);
        }
    }

    [Fact]
    public async Task StopOnExit_OptIn_EncerraApenasOProcessoIniciadoPeloPi()
    {
        var owned = StartLongRunningProcess();
        // Processo alheio: não pode ser encerrado nem mesmo com o opt-in ligado.
        var existing = StartLongRunningProcess();
        try
        {
            var settings = new OllamaSettings
            {
                StopOnExit = true,
                StartupTimeoutSeconds = 1
            };
            var sut = Create(
                settings,
                new FakeDiscovery([]),
                new FakeLauncher(OllamaLaunchResult.Ok(owned)),
                new ProbeHandler(failuresBeforeSuccess: int.MaxValue));

            await sut.StartAsync(CancellationToken.None);
            sut.StopOnExit();
            sut.StopOnExit();

            Assert.True(owned.WaitForExit(5000));
            Assert.False(existing.HasExited);
        }
        finally
        {
            Cleanup(owned);
            Cleanup(existing);
        }
    }

    private static OllamaBootstrapper Create(
        OllamaSettings settings,
        IOllamaProcessDiscovery discovery,
        IOllamaProcessLauncher? launcher = null,
        HttpMessageHandler? handler = null)
    {
        var http = new HttpClient(handler ?? new ProbeHandler())
        {
            BaseAddress = new Uri(settings.BaseUrl)
        };
        return new OllamaBootstrapper(
            http,
            settings,
            launcher ?? new FakeLauncher(OllamaLaunchResult.Fail("não usado")),
            discovery,
            NullLogger<OllamaBootstrapper>.Instance);
    }

    private static Process StartLongRunningProcess()
    {
        var info = OperatingSystem.IsWindows()
            ? new ProcessStartInfo("ping", "-t 127.0.0.1")
            : new ProcessStartInfo("sleep", "30");
        info.UseShellExecute = false;
        info.CreateNoWindow = true;
        info.RedirectStandardOutput = true;
        info.RedirectStandardError = true;

        return Process.Start(info) ?? throw new InvalidOperationException("Processo de teste não iniciado.");
    }

    private static void Cleanup(Process process)
    {
        try
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
        }
        catch
        {
            // O processo já saiu ou não pode mais ser inspecionado.
        }
        finally
        {
            process.Dispose();
        }
    }

    private sealed class FakeDiscovery(IReadOnlyCollection<Process> processes) : IOllamaProcessDiscovery
    {
        public int Calls { get; private set; }

        public IReadOnlyCollection<Process> FindLocal()
        {
            Calls += 1;
            return processes;
        }
    }

    private sealed class FakeLauncher(OllamaLaunchResult result) : IOllamaProcessLauncher
    {
        public int Calls { get; private set; }

        public OllamaLaunchResult Start(string? configuredPath)
        {
            Calls += 1;
            return result;
        }
    }

    // Falha algumas sondas (Ollama em subida) e depois responde pronto.
    private sealed class ProbeHandler(int failuresBeforeSuccess = 0) : HttpMessageHandler
    {
        private int _calls;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            _calls += 1;
            return _calls <= failuresBeforeSuccess
                ? Task.FromException<HttpResponseMessage>(new HttpRequestException("offline"))
                : Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
