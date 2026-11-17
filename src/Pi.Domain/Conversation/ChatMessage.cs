namespace Pi.Domain.Conversation;

/// <summary>
/// Uma mensagem imutável da conversa.
/// </summary>
public sealed class ChatMessage
{
    private ChatMessage(
        MessageRole role,
        string content,
        MessageSource source,
        DateTimeOffset timestamp)
    {
        Role = role;
        Content = content;
        Source = source;
        Timestamp = timestamp;
    }

    public MessageRole Role { get; }

    public string Content { get; }

    public MessageSource Source { get; }

    public DateTimeOffset Timestamp { get; }

    public static ChatMessage ForUser(string content, MessageSource source = MessageSource.Text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        return new ChatMessage(MessageRole.User, content.Trim(), source, DateTimeOffset.UtcNow);
    }

    public static ChatMessage ForAssistant(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        return new ChatMessage(MessageRole.Assistant, content.Trim(), MessageSource.Text, DateTimeOffset.UtcNow);
    }

    public static ChatMessage ForSystem(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        return new ChatMessage(MessageRole.System, content.Trim(), MessageSource.Text, DateTimeOffset.UtcNow);
    }
}
