using Chat.Abstractions;
using Chat.Core;
using Chat.Store.InMemory;
using Xunit;

namespace Chat.Tests;

public class ValidationTests
{
    private readonly ChatService _service;
    private readonly ChatServiceOptions _options;

    public ValidationTests()
    {
        _options = new ChatServiceOptions
        {
            MaxMessageBodyLength = 100,
            MaxTitleLength = 50,
            MaxGroupParticipants = 5,
            MinGroupParticipants = 2,
            EditWindowDuration = TimeSpan.FromMinutes(15)
        };

        _service = new ChatService(
            new InMemoryConversationStore(),
            new InMemoryMessageStore(),
            new NullChatNotifier(),
            new UlidIdGenerator(),
            _options);
    }

    private static EntityRefDto User(string id, string name) =>
        EntityRefDto.Profile(id, name);

    // ─────────────────────────────────────────────────────────────────
    // Tenant Validation
    // ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyTenantId_ThrowsException(string? tenantId)
    {
        var ex = await Assert.ThrowsAsync<ChatValidationException>(() =>
            _service.GetOrCreateDirectConversationAsync(tenantId!, User("u1", "A"), User("u2", "B")));
        Assert.Equal(ChatValidationError.TenantIdRequired, ex.Error);
    }

    // ─────────────────────────────────────────────────────────────────
    // Message Body Validation
    // ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyMessageBody_ThrowsException(string? body)
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
                Body = body!
            }));

        Assert.Equal(ChatValidationError.BodyRequired, ex.Error);
    }

    [Fact]
    public async Task MessageBodyTooLong_ThrowsException()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);

        var longBody = new string('A', 101); // Max is 100

        var ex = await Assert.ThrowsAsync<ChatValidationException>(() =>
            _service.SendMessageAsync(new SendMessageRequest
            {
                TenantId = "tenant1",
                ConversationId = conv.Id!,
                Sender = user1,
                Body = longBody
            }));

        Assert.Equal(ChatValidationError.BodyTooLong, ex.Error);
    }

    // ─────────────────────────────────────────────────────────────────
    // Group Title Validation
    // ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyGroupTitle_ThrowsException(string? title)
    {
        var creator = User("user1", "Alice");

        var ex = await Assert.ThrowsAsync<ChatValidationException>(() =>
            _service.CreateGroupConversationAsync(new CreateConversationRequest
            {
                TenantId = "tenant1",
                Creator = creator,
                Type = ConversationType.Group,
                Participants = [creator, User("user2", "Bob")],
                Title = title
            }));

        Assert.Equal(ChatValidationError.TitleRequired, ex.Error);
    }

    [Fact]
    public async Task GroupTitleTooLong_ThrowsException()
    {
        var creator = User("user1", "Alice");
        var longTitle = new string('A', 51); // Max is 50

        var ex = await Assert.ThrowsAsync<ChatValidationException>(() =>
            _service.CreateGroupConversationAsync(new CreateConversationRequest
            {
                TenantId = "tenant1",
                Creator = creator,
                Type = ConversationType.Group,
                Participants = [creator, User("user2", "Bob")],
                Title = longTitle
            }));

        Assert.Equal(ChatValidationError.TitleTooLong, ex.Error);
    }

    // ─────────────────────────────────────────────────────────────────
    // Participant Validation
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TooFewParticipants_ThrowsException()
    {
        var creator = User("user1", "Alice");

        var ex = await Assert.ThrowsAsync<ChatValidationException>(() =>
            _service.CreateGroupConversationAsync(new CreateConversationRequest
            {
                TenantId = "tenant1",
                Creator = creator,
                Type = ConversationType.Group,
                Participants = [creator], // Only 1
                Title = "Group"
            }));

        Assert.Equal(ChatValidationError.TooFewParticipants, ex.Error);
    }

    [Fact]
    public async Task TooManyParticipants_ThrowsException()
    {
        var creator = User("user1", "Alice");
        var participants = Enumerable.Range(1, 6)
            .Select(i => User($"user{i}", $"User{i}"))
            .ToList(); // 6 participants, max is 5

        var ex = await Assert.ThrowsAsync<ChatValidationException>(() =>
            _service.CreateGroupConversationAsync(new CreateConversationRequest
            {
                TenantId = "tenant1",
                Creator = creator,
                Type = ConversationType.Group,
                Participants = participants,
                Title = "Group"
            }));

        Assert.Equal(ChatValidationError.TooManyParticipants, ex.Error);
    }

    // ─────────────────────────────────────────────────────────────────
    // Authorization Validation
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendMessage_NotParticipant_ThrowsException()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");
        var user3 = User("user3", "Charlie");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);

        var ex = await Assert.ThrowsAsync<ChatValidationException>(() =>
            _service.SendMessageAsync(new SendMessageRequest
            {
                TenantId = "tenant1",
                ConversationId = conv.Id!,
                Sender = user3,
                Body = "Hello!"
            }));

        Assert.Equal(ChatValidationError.NotParticipant, ex.Error);
    }

    [Fact]
    public async Task ConversationNotFound_ThrowsException()
    {
        var user1 = User("user1", "Alice");

        var ex = await Assert.ThrowsAsync<ChatValidationException>(() =>
            _service.SendMessageAsync(new SendMessageRequest
            {
                TenantId = "tenant1",
                ConversationId = "nonexistent",
                Sender = user1,
                Body = "Hello!"
            }));

        Assert.Equal(ChatValidationError.ConversationNotFound, ex.Error);
    }

    [Fact]
    public async Task MessageNotFound_ThrowsException()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);

        var ex = await Assert.ThrowsAsync<ChatValidationException>(() =>
            _service.EditMessageAsync(new EditMessageRequest
            {
                TenantId = "tenant1",
                ConversationId = conv.Id!,
                MessageId = "nonexistent",
                Actor = user1,
                Body = "Edited"
            }));

        Assert.Equal(ChatValidationError.MessageNotFound, ex.Error);
    }

    // ─────────────────────────────────────────────────────────────────
    // Edit Window Validation
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EditMessage_WithinWindow_Succeeds()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);

        var msg = await _service.SendMessageAsync(new SendMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Sender = user1,
            Body = "Original"
        });

        // Edit immediately (within 15 minute window)
        var edited = await _service.EditMessageAsync(new EditMessageRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            MessageId = msg.Id!,
            Actor = user1,
            Body = "Edited"
        });

        Assert.Equal("Edited", edited.Body);
    }

    // ─────────────────────────────────────────────────────────────────
    // Null/Empty Entity Reference Validation
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task NullSender_ThrowsException()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");

        var conv = await _service.GetOrCreateDirectConversationAsync("tenant1", user1, user2);

        var ex = await Assert.ThrowsAsync<ChatValidationException>(() =>
            _service.SendMessageAsync(new SendMessageRequest
            {
                TenantId = "tenant1",
                ConversationId = conv.Id!,
                Sender = null!,
                Body = "Hello!"
            }));

        Assert.Equal(ChatValidationError.SenderRequired, ex.Error);
    }

    [Fact]
    public async Task NullProfile_ThrowsException()
    {
        var ex = await Assert.ThrowsAsync<ChatValidationException>(() =>
            _service.GetOrCreateDirectConversationAsync("tenant1", null!, User("u1", "A")));
        Assert.Equal(ChatValidationError.ProfileRequired, ex.Error);
    }
}
