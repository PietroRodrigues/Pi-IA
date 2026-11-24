// Canal de saída dos eventos do agente. O Host implementa sobre o escritor NDJSON.
namespace Pi.Core.Abstractions;

public interface IEventSink
{
    Task PublishAsync<T>(
        string type,
        T payload,
        string correlationId = "",
        CancellationToken cancellationToken = default);
}
