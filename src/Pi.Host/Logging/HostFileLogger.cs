// Logger mínimo em arquivo: mantém o stdout exclusivo para NDJSON.
using Microsoft.Extensions.Logging;
using Pi.Contracts.Constants;

namespace Pi.Host.Logging;

internal sealed class HostFileLogger(string path, string category, object gate) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var line = string.Format(
            PiConstants.Messages.LogLineTemplate,
            DateTimeOffset.UtcNow.ToString(PiConstants.Formats.TimestampRoundtrip),
            logLevel,
            category,
            formatter(state, exception));
        if (exception is not null)
        {
            line += Environment.NewLine + exception;
        }

        lock (gate)
        {
            File.AppendAllText(path, line + Environment.NewLine);
        }
    }
}
