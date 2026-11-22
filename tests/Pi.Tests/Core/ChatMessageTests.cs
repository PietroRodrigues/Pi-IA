// Garante que cada mensagem guarda o papel certo e nunca aceita texto vazio.
using Pi.Core.Chat;
using Xunit;

namespace Pi.Tests;

public class ChatMessageTests
{
    [Fact]
    public void ForUser_RecordsRoleAndTrimmedContent()
    {
        var message = ChatMessage.ForUser("  olá  ");

        Assert.Equal(MessageRole.User, message.Role);
        Assert.Equal("olá", message.Content);
    }

    [Fact]
    public void ForAssistant_UsesAssistantRole()
    {
        var message = ChatMessage.ForAssistant("resposta");

        Assert.Equal(MessageRole.Assistant, message.Role);
        Assert.Equal("resposta", message.Content);
    }

    [Fact]
    public void ForUser_RejectsWhitespace()
        => Assert.Throws<ArgumentException>(() => ChatMessage.ForUser("   "));
}
