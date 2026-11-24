// As mensagens que formam o histórico. São imutáveis para poderem ser reutilizadas.
namespace Pi.Model.Chat;

// Papel de uma mensagem na conversa.
public enum MessageRole
{
    System,
    User,
    Assistant
}

// Uma mensagem imutável da conversa.
public sealed class ChatMessage
{
    private ChatMessage(MessageRole role, string content, DateTimeOffset timestamp)
    {
        Role = role;
        Content = content;
        Timestamp = timestamp;
    }

    public MessageRole Role { get; }

    public string Content { get; }

    public DateTimeOffset Timestamp { get; }

    public static ChatMessage ForUser(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        return new ChatMessage(MessageRole.User, content.Trim(), DateTimeOffset.UtcNow);
    }

    public static ChatMessage ForAssistant(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        return new ChatMessage(MessageRole.Assistant, content.Trim(), DateTimeOffset.UtcNow);
    }

    public static ChatMessage ForSystem(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        return new ChatMessage(MessageRole.System, content.Trim(), DateTimeOffset.UtcNow);
    }
}
