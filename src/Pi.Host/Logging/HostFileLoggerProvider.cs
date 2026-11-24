// Provider que direciona todos os logs do host para um arquivo em artifacts/debug.
using Microsoft.Extensions.Logging;

namespace Pi.Host.Logging;

internal sealed class HostFileLoggerProvider(string path) : ILoggerProvider
{
    private readonly object _gate = new();

    public ILogger CreateLogger(string categoryName) => new HostFileLogger(path, categoryName, _gate);

    public void Dispose()
    {
    }
}
