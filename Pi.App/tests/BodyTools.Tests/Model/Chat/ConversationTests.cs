// Garante que o histórico respeita o limite, descarta as mensagens antigas e limpa tudo.
using Pi.Model.Chat;
using Xunit;
using ConversationAggregate = Pi.Model.Chat.Conversation;

namespace Pi.BodyTools.Tests;

public class ConversationTests
{
    [Fact]
    public void Add_KeepsOnlyTheConfiguredNumberOfMessages()
    {
        var conversation = new ConversationAggregate(maxMessages: 4);

        for (var index = 0; index < 10; index++)
        {
            conversation.Add(ChatMessage.ForUser($"mensagem-{index}"));
        }

        Assert.Equal(4, conversation.Messages.Count);
        Assert.Equal("mensagem-6", conversation.Messages[0].Content);
        Assert.Equal("mensagem-9", conversation.Messages[^1].Content);
    }

    [Fact]
    public void Constructor_RejectsHistorySmallerThanTwo()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new ConversationAggregate(1));

    [Fact]
    public void Clear_RemovesAllMessages()
    {
        var conversation = new ConversationAggregate();
        conversation.Add(ChatMessage.ForUser("olá"));

        conversation.Clear();

        Assert.Empty(conversation.Messages);
    }
}
