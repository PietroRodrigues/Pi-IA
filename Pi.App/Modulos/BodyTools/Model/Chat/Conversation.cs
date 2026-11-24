// Histórico da conversa. Cresce até o limite e descarta as mensagens mais velhas.
namespace Pi.Model.Chat;

// Agregado que mantém o histórico limitado da conversa.
public sealed class Conversation
{
    private readonly List<ChatMessage> _messages;
    private readonly int _maxMessages;

    public Conversation(int maxMessages = 20)
    {
        if (maxMessages < 2)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxMessages),
                maxMessages,
                "O histórico precisa manter ao menos 2 mensagens.");
        }

        _maxMessages = maxMessages;
        _messages = new List<ChatMessage>(maxMessages);
    }

    public IReadOnlyList<ChatMessage> Messages => _messages.AsReadOnly();

    public int MaxMessages => _maxMessages;

    public void Add(ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        _messages.Add(message);
        Trim();
    }

    public void Clear() => _messages.Clear();

    private void Trim()
    {
        if (_messages.Count <= _maxMessages)
        {
            return;
        }

        // As primeiras da lista são as mais antigas.
        _messages.RemoveRange(0, _messages.Count - _maxMessages);
    }
}
