using Chat.Abstractions;
using Chat.Core;
using Chat.Store.InMemory;
using Xunit;

namespace Chat.Tests;

public class ConversationTests
{
    private readonly ChatService _service;
    private readonly InMemoryConversationStore _conversationStore;
    private readonly InMemoryMessageStore _messageStore;
    private readonly TestChatNotifier _notifier;

    public ConversationTests()
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
    public async Task SetArchived_ArchivesConversation()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);

        await _service.SetArchivedAsync("tenant1", conv.Id!, user1, true);

        var archived = await _service.GetConversationAsync("tenant1", conv.Id!, user1);
        Assert.True(archived!.IsArchived);

        // User2 should see it as not archived
        var notArchived = await _service.GetConversationAsync("tenant1", conv.Id!, user2);
        Assert.False(notArchived!.IsArchived);
    }

    [Fact]
    public async Task SetMuted_MutesConversation()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);

        await _service.SetMutedAsync("tenant1", conv.Id!, user1, true);

        var muted = await _service.GetConversationAsync("tenant1", conv.Id!, user1);
        Assert.True(muted!.IsMuted);

        // User2 should see it as not muted
        var notMuted = await _service.GetConversationAsync("tenant1", conv.Id!, user2);
        Assert.False(notMuted!.IsMuted);
    }

    [Fact]
    public async Task GetConversations_ExcludesArchivedByDefault()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");
        var user3 = User("user3", "Charlie");

        var conv1 = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);
        var conv2 = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user3);

        // Archive first conversation
        await _service.SetArchivedAsync("tenant1", conv1.Id!, user1, true);

        // Query without archived
        var result = await _service.GetConversationsAsync(new ConversationQuery
        {
            TenantId = "tenant1",
            Participant = user1,
            IncludeArchived = false
        });

        Assert.Single(result.Items);
        Assert.Equal(conv2.Id, result.Items[0].Id);
    }

    [Fact]
    public async Task GetConversations_IncludesArchivedWhenRequested()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");
        var user3 = User("user3", "Charlie");

        var conv1 = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);
        await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user3);

        // Archive first conversation
        await _service.SetArchivedAsync("tenant1", conv1.Id!, user1, true);

        // Query with archived
        var result = await _service.GetConversationsAsync(new ConversationQuery
        {
            TenantId = "tenant1",
            Participant = user1,
            IncludeArchived = true
        });

        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetConversations_FiltersByType()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        // Create direct conversation
        await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);

        // Create group conversation
        await _service.CreateGroupConversationAsync(new CreateConversationRequest
        {
            TenantId = "tenant1",
            Creator = user1,
            Type = ConversationType.Group,
            Participants = [user1, user2],
            Title = "Group"
        });

        // Query only direct
        var directResult = await _service.GetConversationsAsync(new ConversationQuery
        {
            TenantId = "tenant1",
            Participant = user1,
            Type = ConversationType.Direct
        });
        Assert.Single(directResult.Items);
        Assert.Equal(ConversationType.Direct, directResult.Items[0].Type);

        // Query only group
        var groupResult = await _service.GetConversationsAsync(new ConversationQuery
        {
            TenantId = "tenant1",
            Participant = user1,
            Type = ConversationType.Group
        });
        Assert.Single(groupResult.Items);
        Assert.Equal(ConversationType.Group, groupResult.Items[0].Type);
    }

    [Fact]
    public async Task GetConversations_OrdersByUpdatedAtDescending()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");
        var user3 = User("user3", "Charlie");

        var conv1 = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);
        await Task.Delay(10);
        var conv2 = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user3);

        var result = await _service.GetConversationsAsync(new ConversationQuery
        {
            TenantId = "tenant1",
            Participant = user1
        });

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(conv2.Id, result.Items[0].Id); // Newer first
        Assert.Equal(conv1.Id, result.Items[1].Id);
    }

    [Fact]
    public async Task GetConversations_PaginatesWithCursor()
    {
        var user1 = User("user1", "Alice");

        // Create 5 conversations
        for (int i = 0; i < 5; i++)
        {
            await _service.GetOrCreateDirectConversationAsync("tenant1", user1, User($"user{i + 2}", $"User{i + 2}"));
            await Task.Delay(10);
        }

        // Get first page
        var page1 = await _service.GetConversationsAsync(new ConversationQuery
        {
            TenantId = "tenant1",
            Participant = user1,
            Limit = 2
        });

        Assert.Equal(2, page1.Items.Count);
        Assert.True(page1.HasMore);
        Assert.NotNull(page1.NextCursor);

        // Get second page
        var page2 = await _service.GetConversationsAsync(new ConversationQuery
        {
            TenantId = "tenant1",
            Participant = user1,
            Limit = 2,
            Cursor = page1.NextCursor
        });

        Assert.Equal(2, page2.Items.Count);
        Assert.True(page2.HasMore);

        // Get third page
        var page3 = await _service.GetConversationsAsync(new ConversationQuery
        {
            TenantId = "tenant1",
            Participant = user1,
            Limit = 2,
            Cursor = page2.NextCursor
        });

        Assert.Single(page3.Items);
        Assert.False(page3.HasMore);
    }

    [Fact]
    public async Task GetConversation_PopulatesLastMessage()
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
            Body = "Last"
        });

        var result = await _service.GetConversationAsync("tenant1", conv.Id!, user1);

        Assert.NotNull(result!.LastMessage);
        Assert.Equal("Last", result.LastMessage!.Body);
    }

    [Fact]
    public async Task UpdateConversation_TitleChange_CreatesSystemMessage()
    {
        var creator = User("user1", "Alice");

        var conv = await _service.CreateGroupConversationAsync(new CreateConversationRequest
        {
            TenantId = "tenant1",
            Creator = creator,
            Type = ConversationType.Group,
            Participants = [creator, User("user2", "Bob")],
            Title = "Original"
        });

        await _service.UpdateConversationAsync(new UpdateConversationRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Actor = creator,
            Title = "New Title"
        });

        // Check for system message
        var messages = await _service.GetMessagesAsync(new MessageQuery
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Viewer = creator
        });

        Assert.Contains(messages.Items, m => m.SystemMessageType == SystemMessageType.TitleChanged);
    }
}
