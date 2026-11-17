using Pi.Domain.Conversation;
using Xunit;

namespace Pi.Domain.Tests;

public class ChatMessageTests
{
    [Fact]
    public void ForUser_RecordsRoleSourceAndContent()
    {
        var message = ChatMessage.ForUser("  olá  ", MessageSource.Voice);

        Assert.Equal(MessageRole.User, message.Role);
        Assert.Equal(MessageSource.Voice, message.Source);
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
