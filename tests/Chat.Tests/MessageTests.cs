using Chat.Abstractions;
using Chat.Core;
using Chat.Store.InMemory;
using Xunit;

namespace Chat.Tests;

public class MessageTests
{
    private readonly ChatService _service;
    private readonly InMemoryConversationStore _conversationStore;
    private readonly InMemoryMessageStore _messageStore;
    private readonly TestChatNotifier _notifier;

    public MessageTests()
    {
        _conversationStore = new InMemoryConversationStore();
        _messageStore = new InMemoryMessageStore();
        _notifier = new TestChatNotifier();
        _service = new ChatService(
            _conversationStore,
            _messageStore,
            _notifier,
            new UlidIdGenerator());
    }

    private static EntityRefDto User(string id, string name) =>
        EntityRefDto.Profile(id, name);

    [Fact]
    public async Task SendMessage_SetsIdAndTimestamp()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);

        var message = await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Sender = user1,
            Body = "Hello!"
        });

        Assert.NotNull(message.Id);
        Assert.Equal("Hello!", message.Body);
        Assert.Equal("user1", message.Sender.Id);
        Assert.True(message.CreatedAt > DateTimeOffset.MinValue);
    }

    [Fact]
    public async Task SendMessage_UpdatesConversationUpdatedAt()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);
        var originalUpdatedAt = conv.UpdatedAt;

        await Task.Delay(10); // Ensure time difference

        await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Sender = user1,
            Body = "Hello!"
        });

        var updatedConv = await _service.GetConversationAsync("tenant1", conv.Id!, user1);
        Assert.True(updatedConv!.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public async Task SendMessage_NotifiesOtherParticipants()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);

        await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Sender = user1,
            Body = "Hello!"
        });

        Assert.Single(_notifier.MessagesReceived);
        Assert.Equal("Hello!", _notifier.MessagesReceived[0].Message.Body);
    }

    [Fact]
    public async Task SendMessage_TrimsWhitespace()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);

        var message = await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Sender = user1,
            Body = "  Hello!  "
        });

        Assert.Equal("Hello!", message.Body);
    }

    [Fact]
    public async Task SendMessage_WithReplyTo_SetsReplyToId()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);

        var msg1 = await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Sender = user1,
            Body = "First message"
        });

        var msg2 = await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Sender = user2,
            Body = "Reply to first",
            ReplyToMessageId = msg1.Id
        });

        Assert.Equal(msg1.Id, msg2.ReplyToMessageId);
    }

    [Fact]
    public async Task SendMessage_InvalidReplyTo_ThrowsException()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);

        var ex = await Assert.ThrowsAsync<ChatValidationException>(() =>
            _service.SendMessageAsync(new SendMessageRequest
            {
                TenantId = "tenant1",
                ConversationId = conv.Id!,
                Sender = user1,
                Body = "Reply",
                ReplyToMessageId = "nonexistent"
            }));

        Assert.Equal(ChatValidationError.InvalidReplyToMessage, ex.Error);
    }

    [Fact]
    public async Task EditMessage_UpdatesBodyAndTimestamp()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);

        var message = await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Sender = user1,
            Body = "Original"
        });

        var edited = await _service.EditMessageAsync(new EditMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            MessageId = message.Id!,
            Actor = user1,
            Body = "Edited"
        });

        Assert.Equal("Edited", edited.Body);
        Assert.NotNull(edited.EditedAt);
    }

    [Fact]
    public async Task EditMessage_ByNonSender_ThrowsException()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);

        var message = await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Sender = user1,
            Body = "Original"
        });

        var ex = await Assert.ThrowsAsync<ChatValidationException>(() =>
            _service.EditMessageAsync(new EditMessageRequest
            {
                TenantId = "tenant1",
                ConversationId = conv.Id!,
                MessageId = message.Id!,
                Actor = user2,
                Body = "Edited"
            }));

        Assert.Equal(ChatValidationError.CannotEditOthersMessage, ex.Error);
    }

    [Fact]
    public async Task DeleteForSelf_HidesFromDeleter()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);

        var message = await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Sender = user1,
            Body = "Test message"
        });

        await _service.DeleteMessageAsync(new DeleteMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            MessageId = message.Id!,
            Actor = user2,
            DeleteForEveryone = false
        });

        // User2 should not see the message
        var user2Messages = await _service.GetMessagesAsync(new MessageQuery
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Viewer = user2
        });
        Assert.Empty(user2Messages.Items);

        // User1 should still see the message
        var user1Messages = await _service.GetMessagesAsync(new MessageQuery
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Viewer = user1
        });
        Assert.Single(user1Messages.Items);
    }

    [Fact]
    public async Task DeleteForEveryone_MarksAsDeleted()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);

        var message = await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Sender = user1,
            Body = "Test message"
        });

        await _service.DeleteMessageAsync(new DeleteMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            MessageId = message.Id!,
            Actor = user1,
            DeleteForEveryone = true
        });

        // Both users should not see the message
        var user1Messages = await _service.GetMessagesAsync(new MessageQuery
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Viewer = user1
        });
        Assert.Empty(user1Messages.Items);

        var user2Messages = await _service.GetMessagesAsync(new MessageQuery
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Viewer = user2
        });
        Assert.Empty(user2Messages.Items);
    }

    [Fact]
    public async Task DeleteForEveryone_ByNonSender_ThrowsException()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);

        var message = await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Sender = user1,
            Body = "Test message"
        });

        var ex = await Assert.ThrowsAsync<ChatValidationException>(() =>
            _service.DeleteMessageAsync(new DeleteMessageRequest
            {
                TenantId = "tenant1",
                ConversationId = conv.Id!,
                MessageId = message.Id!,
                Actor = user2,
                DeleteForEveryone = true
            }));

        Assert.Equal(ChatValidationError.CannotDeleteForEveryoneOthersMessage, ex.Error);
    }

    [Fact]
    public async Task GetMessages_ReturnsInOrder()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);

        await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Sender = user1,
            Body = "First"
        });

        await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Sender = user2,
            Body = "Second"
        });

        await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Sender = user1,
            Body = "Third"
        });

        var messages = await _service.GetMessagesAsync(new MessageQuery
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Viewer = user1,
            Direction = MessageQueryDirection.Older
        });

        Assert.Equal(3, messages.Items.Count);
        // Older direction means newest first
        Assert.Equal("Third", messages.Items[0].Body);
        Assert.Equal("Second", messages.Items[1].Body);
        Assert.Equal("First", messages.Items[2].Body);
    }

    [Fact]
    public async Task GetMessage_PopulatesReplyTo()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);

        var msg1 = await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Sender = user1,
            Body = "Original"
        });

        var msg2 = await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Sender = user2,
            Body = "Reply",
            ReplyToMessageId = msg1.Id
        });

        var retrieved = await _service.GetMessageAsync("tenant1", conv.Id!, msg2.Id!, user2);

        Assert.NotNull(retrieved!.ReplyTo);
        Assert.Equal("Original", retrieved.ReplyTo!.Body);
    }
}
