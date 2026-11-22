using System.Text.Json;

namespace Pi.Bridge;

/// <summary>
/// Escritor exclusivo do stdout no modo ponte. Uma linha JSON por evento,
/// com flush imediato para a TUI receber tokens sem esperar o buffer.
/// </summary>
public sealed class JsonLinesWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly TextWriter _writer;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonLinesWriter(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        _writer = writer;
    }

    public async Task WriteAsync<T>(T @event, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(@event, SerializerOptions);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await _writer.WriteLineAsync(json);
            await _writer.FlushAsync();
        }
        finally
        {
            _gate.Release();
        }
    }
}
