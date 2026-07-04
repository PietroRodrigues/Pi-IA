using System.Collections.Concurrent;

namespace Pi.App.Assistant
{
    public class AssistantMessageBus
    {
        private readonly ConcurrentQueue<AssistantMessage> _messages = new();

        public void Publish(AssistantMessage message)
        {
            _messages.Enqueue(message);
        }

        public bool TryRead(out AssistantMessage? message)
        {
            return _messages.TryDequeue(out message);
        }
    }
}