using Chat.Abstractions;
using ActivityStream.Abstractions;
using Chat.Core;
using Chat.Store.InMemory;
using Xunit;

namespace ActivityStream.Tests.Chat;

public class ReadReceiptTests
{
    private readonly ChatService _service;
    private readonly InMemoryConversationStore _conversationStore;
    private readonly InMemoryMessageStore _messageStore;
    private readonly TestChatNotifier _notifier;

    public ReadReceiptTests()
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
    public async Task MarkRead_UpdatesLastReadMessageId()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);

        var msg1 = await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Sender = user1,
            Body = "Message 1"
        });

        var msg2 = await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Sender = user1,
            Body = "Message 2"
        });

        await _service.MarkReadAsync(new MarkReadRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Profile = user2,
            MessageId = msg2.Id!
        });

        // Check read receipts
        var receipts = await _service.GetReadReceiptsAsync("tenant1", conv.Id!, msg2.Id!);
        Assert.Contains(receipts, r => r.Profile.Id == "user2");
    }

    [Fact]
    public async Task MarkRead_NotifiesReceipt()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);

        var msg = await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Sender = user1,
            Body = "Message"
        });

        await _service.MarkReadAsync(new MarkReadRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Profile = user2,
            MessageId = msg.Id!
        });

        Assert.Single(_notifier.ReadReceipts);
        Assert.Equal("user2", _notifier.ReadReceipts[0].Receipt.Profile.Id);
    }

    [Fact]
    public async Task SendMessage_AutoMarksReadForSender()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);

        var msg = await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Sender = user1,
            Body = "Message"
        });

        // Check that sender's read state is updated
        var receipts = await _service.GetReadReceiptsAsync("tenant1", conv.Id!, msg.Id!);
        Assert.Contains(receipts, r => r.Profile.Id == "user1");
    }

    [Fact]
    public async Task UnreadCount_CalculatesCorrectly()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);

        // User1 sends 3 messages
        await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Sender = user1,
            Body = "Message 1"
        });

        await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Sender = user1,
            Body = "Message 2"
        });

        var msg3 = await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Sender = user1,
            Body = "Message 3"
        });

        // User2 has not read any messages
        var convForUser2 = await _service.GetConversationAsync("tenant1", conv.Id!, user2);
        Assert.Equal(3, convForUser2!.UnreadCount);

        // User2 reads up to message 2
        await _service.MarkReadAsync(new MarkReadRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Profile = user2,
            MessageId = msg3.Id! // Read through msg3
        });

        convForUser2 = await _service.GetConversationAsync("tenant1", conv.Id!, user2);
        Assert.Equal(0, convForUser2!.UnreadCount);
    }

    [Fact]
    public async Task GetTotalUnreadCount_SumsAcrossConversations()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");
        var user3 = User("user3", "Charlie");

        // Create two conversations for user1
        var conv1 = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);
        var conv2 = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user3);

        // User2 sends 2 messages
        await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv1.Id!,
            Sender = user2,
            Body = "From Bob 1"
        });
        await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv1.Id!,
            Sender = user2,
            Body = "From Bob 2"
        });

        // User3 sends 3 messages
        await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv2.Id!,
            Sender = user3,
            Body = "From Charlie 1"
        });
        await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv2.Id!,
            Sender = user3,
            Body = "From Charlie 2"
        });
        await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv2.Id!,
            Sender = user3,
            Body = "From Charlie 3"
        });

        var totalUnread = await _service.GetTotalUnreadCountAsync("tenant1", user1);
        Assert.Equal(5, totalUnread);
    }

    [Fact]
    public async Task GetReadReceipts_ReturnsParticipantsWhoRead()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);

        var msg = await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Sender = user1,
            Body = "Message"
        });

        // Initially only sender has read
        var receipts1 = await _service.GetReadReceiptsAsync("tenant1", conv.Id!, msg.Id!);
        Assert.Single(receipts1);
        Assert.Equal("user1", receipts1[0].Profile.Id);

        // User2 reads
        await _service.MarkReadAsync(new MarkReadRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Profile = user2,
            MessageId = msg.Id!
        });

        var receipts2 = await _service.GetReadReceiptsAsync("tenant1", conv.Id!, msg.Id!);
        Assert.Equal(2, receipts2.Count);
    }
}
