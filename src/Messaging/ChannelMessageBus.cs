using System.Threading.Channels;
using Pi.Abstracts.Messaging;
using Pi.Models;

namespace Pi.Messaging;

/// <summary>
/// Barramento em memória baseado em canal limitado.
/// </summary>
public sealed class ChannelMessageBus : IMessageBus
{
    private const int Capacity = 100;

    private readonly Channel<AssistantMessage> _channel = Channel.CreateBounded<AssistantMessage>(
        new BoundedChannelOptions(Capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });

    public ValueTask PublishAsync(AssistantMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return _channel.Writer.WriteAsync(message, cancellationToken);
    }

    public Task<AssistantMessage> ReadAsync(CancellationToken cancellationToken = default)
        => _channel.Reader.ReadAsync(cancellationToken).AsTask();
}
