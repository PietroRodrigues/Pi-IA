using System.Threading.Channels;

namespace Pi.App.Assistant
{
    public class AssistantMessageBus
    {
        private readonly Channel<AssistantMessage> _channel;

        public AssistantMessageBus()
        {
            _channel = Channel.CreateUnbounded<AssistantMessage>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
        }

         public async Task PublishAsync(AssistantMessage message)
        {
            await _channel.Writer.WriteAsync(message);
        }

        public async Task<AssistantMessage> ReadAsync()
        {
            return await _channel.Reader.ReadAsync();
        }
    }
}