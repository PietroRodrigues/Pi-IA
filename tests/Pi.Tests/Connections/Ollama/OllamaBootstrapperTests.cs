using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Pi.Connections.Ollama;
using Pi.Options;
using Xunit;

namespace Pi.Tests;

public sealed class OllamaBootstrapperTests
{
    [Fact]
    public async Task JaAtivo_NaoIniciaProcesso()
    {
        var handler = new SequenceHandler(Ok);
        var launcher = new FakeLauncher();
        var bootstrapper = Criar(handler, launcher);

        await bootstrapper.StartAsync(CancellationToken.None);

        Assert.Equal(0, launcher.Calls);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task SobeAposTentativas_IniciaUmaVez()
    {
        var handler = new SequenceHandler(
            () => throw new HttpRequestException("ainda não"),
            () => throw new HttpRequestException("ainda não"),
            Ok);
        var launcher = new FakeLauncher();
        var bootstrapper = Criar(handler, launcher, opcoes => opcoes.StartupTimeoutSeconds = 5);

        await bootstrapper.StartAsync(CancellationToken.None);

        Assert.Equal(1, launcher.Calls);
        Assert.Equal(3, handler.Calls);
    }

    [Fact]
    public async Task Timeout_AvisaENaoLanca()
    {
        var handler = new SequenceHandler(() => throw new HttpRequestException("fora"));
        var launcher = new FakeLauncher();
        var bootstrapper = Criar(handler, launcher, opcoes => opcoes.StartupTimeoutSeconds = 1);

        var excecao = await Record.ExceptionAsync(
            () => bootstrapper.StartAsync(CancellationToken.None));

        Assert.Null(excecao);
        Assert.Equal(1, launcher.Calls);
        Assert.True(handler.Calls >= 2);
    }

    [Fact]
    public async Task ExecutavelAusente_AvisaENaoLanca()
    {
        var handler = new SequenceHandler(() => throw new HttpRequestException("fora"));
        var launcher = new FakeLauncher(OllamaLaunchResult.Fail("não encontrado"));
        var bootstrapper = Criar(handler, launcher);

        var excecao = await Record.ExceptionAsync(
            () => bootstrapper.StartAsync(CancellationToken.None));

        Assert.Null(excecao);
        Assert.Equal(1, launcher.Calls);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task BaseUrlRemoto_IgnoraAutoStart()
    {
        var handler = new SequenceHandler(Ok);
        var launcher = new FakeLauncher();
        var bootstrapper = Criar(
            handler,
            launcher,
            opcoes => opcoes.BaseUrl = "http://api.example.com:11434");

        await bootstrapper.StartAsync(CancellationToken.None);

        Assert.Equal(0, launcher.Calls);
        Assert.Equal(0, handler.Calls);
    }

    private static OllamaBootstrapper Criar(
        SequenceHandler handler,
        FakeLauncher launcher,
        Action<OllamaOptions>? configurar = null)
    {
        var opcoes = new OllamaOptions { StartupTimeoutSeconds = 5 };
        configurar?.Invoke(opcoes);

        return new OllamaBootstrapper(
            new FakeHttpClientFactory(handler),
            Microsoft.Extensions.Options.Options.Create(opcoes),
            launcher,
            NullLogger<OllamaBootstrapper>.Instance);
    }

    private static HttpResponseMessage Ok() => new(HttpStatusCode.OK);

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public FakeHttpClientFactory(HttpMessageHandler handler) => _handler = handler;

        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }

    private sealed class SequenceHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpResponseMessage>> _respostas;

        public SequenceHandler(params Func<HttpResponseMessage>[] respostas) =>
            _respostas = new Queue<Func<HttpResponseMessage>>(respostas);

        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;

            return _respostas.Count == 0
                ? Task.FromResult(Ok())
                : Task.FromResult(_respostas.Dequeue()());
        }
    }

    private sealed class FakeLauncher : IOllamaProcessLauncher
    {
        private readonly OllamaLaunchResult _resultado;

        public FakeLauncher(OllamaLaunchResult? resultado = null) =>
            _resultado = resultado ?? OllamaLaunchResult.Ok();

        public int Calls { get; private set; }

        public OllamaLaunchResult Start(string? configuredPath)
        {
            Calls++;
            return _resultado;
        }
    }
}
