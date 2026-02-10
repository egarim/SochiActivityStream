using Chat.Abstractions;
using ActivityStream.Abstractions;
using Chat.Core;
using Chat.Store.InMemory;
using Xunit;

namespace ActivityStream.Tests.Chat;

public class DirectConversationTests
{
    private readonly ChatService _service;
    private readonly InMemoryConversationStore _conversationStore;
    private readonly InMemoryMessageStore _messageStore;
    private readonly TestChatNotifier _notifier;

    public DirectConversationTests()
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
    public async Task GetOrCreateDirect_NewConversation_CreatesWithBothParticipants()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);

        Assert.NotNull(conv.Id);
        Assert.Equal("tenant1", conv.TenantId);
        Assert.Equal(ConversationType.Direct, conv.Type);
        Assert.Equal(2, conv.Participants.Count);
        Assert.Contains(conv.Participants, p => p.Profile.Id == "user1");
        Assert.Contains(conv.Participants, p => p.Profile.Id == "user2");
    }

    [Fact]
    public async Task GetOrCreateDirect_ExistingConversation_ReturnsSame()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv1 = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);
        var conv2 = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);
        var conv3 = await _service.GetOrCreateDirectConversationAsync("tenant1", user2, user1);

        Assert.Equal(conv1.Id, conv2.Id);
        Assert.Equal(conv1.Id, conv3.Id);
    }

    [Fact]
    public async Task GetOrCreateDirect_DifferentTenants_CreatesSeparate()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv1 = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);
        var conv2 = await _service.GetOrCreateDirectConversationAsync("tenant2", user1, user2);

        Assert.NotEqual(conv1.Id, conv2.Id);
    }

    [Fact]
    public async Task DirectConversation_CannotAddParticipant_ThrowsException()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");
        var user3 = User("user3", "Charlie");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);

        var ex = await Assert.ThrowsAsync<ChatValidationException>(() =>
            _service.AddParticipantAsync(new AddParticipantRequest
            {
                TenantId = "tenant1",
                ConversationId = conv.Id!,
                Actor = user1,
                NewParticipant = user3
            }));

        Assert.Equal(ChatValidationError.DirectConversationCannotAddParticipants, ex.Error);
    }

    [Fact]
    public async Task DirectConversation_CannotRemoveParticipant_ThrowsException()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);

        var ex = await Assert.ThrowsAsync<ChatValidationException>(() =>
            _service.RemoveParticipantAsync(new RemoveParticipantRequest
            {
                TenantId = "tenant1",
                ConversationId = conv.Id!,
                Actor = user1,
                Participant = user2
            }));

        Assert.Equal(ChatValidationError.DirectConversationCannotRemoveParticipants, ex.Error);
    }

    [Fact]
    public async Task GetConversation_AsParticipant_ReturnsConversation()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);
        var result = await _service.GetConversationAsync("tenant1", conv.Id!, user1);

        Assert.NotNull(result);
        Assert.Equal(conv.Id, result!.Id);
    }

    [Fact]
    public async Task GetConversation_AsNonParticipant_ReturnsNull()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");
        var user3 = User("user3", "Charlie");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);
        var result = await _service.GetConversationAsync("tenant1", conv.Id!, user3);

        Assert.Null(result);
    }
}
