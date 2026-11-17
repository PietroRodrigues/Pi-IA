using Pi.Domain.Conversation;

namespace Pi.Application.Models;

/// <summary>
/// Mensagem transportada pelo barramento interno.
/// </summary>
public sealed record AssistantMessage(string Text, MessageSource Source)
{
    public static AssistantMessage ForText(string text) => new(text, MessageSource.Text);

    public static AssistantMessage ForVoice(string text) => new(text, MessageSource.Voice);
}
