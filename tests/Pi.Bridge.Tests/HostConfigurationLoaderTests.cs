// O encerramento do Ollama é opt-in e controlado por variável de ambiente.
using Microsoft.Extensions.Configuration;
using Pi.Host.Configuration;
using Xunit;

namespace Pi.Bridge.Tests;

public class HostConfigurationLoaderTests
{
    [Fact]
    public void StopOnExit_SemVariavel_UsaPadraoDesligado()
    {
        WithEnv("OLLAMA_STOP_ON_EXIT", null, () =>
        {
            var configuration = Configuration(includeStopOnExit: false);

            Assert.False(HostConfigurationLoader.Load(configuration).Ollama.StopOnExit);
        });
    }

    [Theory]
    [InlineData("0", false)]
    [InlineData("false", false)]
    [InlineData("1", true)]
    [InlineData("true", true)]
    public void StopOnExit_ComVariavel_SobrescreveConfiguracao(string value, bool expected)
    {
        WithEnv("OLLAMA_STOP_ON_EXIT", value, () =>
        {
            var configuration = Configuration(includeStopOnExit: false);

            Assert.Equal(expected, HostConfigurationLoader.Load(configuration).Ollama.StopOnExit);
        });
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("0", false)]
    public void AllowShell_ContinuaRespeitandoVariavel(string value, bool expected)
    {
        WithEnv("PI_ALLOW_SHELL", value, () =>
        {
            var configuration = Configuration();

            Assert.Equal(expected, HostConfigurationLoader.Load(configuration).Tools.AllowShell);
        });
    }

    private static IConfiguration Configuration(bool includeStopOnExit = true)
    {
        var values = new Dictionary<string, string?>
        {
            ["Ollama:BaseUrl"] = "http://127.0.0.1:11434",
            ["Agent:AgentName"] = "geral"
        };
        if (includeStopOnExit) values["Ollama:StopOnExit"] = "true";

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static void WithEnv(string name, string? value, Action action)
    {
        var previous = Environment.GetEnvironmentVariable(name);
        try
        {
            Environment.SetEnvironmentVariable(name, value);
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, previous);
        }
    }
}
