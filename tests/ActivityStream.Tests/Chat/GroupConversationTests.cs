using Chat.Abstractions;
using ActivityStream.Abstractions;
using Chat.Core;
using Chat.Store.InMemory;
using Xunit;

namespace ActivityStream.Tests.Chat;

public class GroupConversationTests
{
    private readonly ChatService _service;
    private readonly InMemoryConversationStore _conversationStore;
    private readonly InMemoryMessageStore _messageStore;
    private readonly TestChatNotifier _notifier;

    public GroupConversationTests()
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
    public async Task CreateGroup_WithTitle_CreatesWithAllParticipants()
    {
        var creator = User("user1", "Alice");
        var participants = new List<EntityRefDto>
        {
            creator,
            User("user2", "Bob"),
            User("user3", "Charlie")
        };

        var conv = await _service.CreateGroupConversationAsync(new CreateConversationRequest
        {
            TenantId = "tenant1",
            Creator = creator,
            Type = ConversationType.Group,
            Participants = participants,
            Title = "Test Group"
        });

        Assert.NotNull(conv.Id);
        Assert.Equal(ConversationType.Group, conv.Type);
        Assert.Equal("Test Group", conv.Title);
        Assert.Equal(3, conv.Participants.Count);

        // Creator should be owner
        var creatorParticipant = conv.Participants.First(p => p.Profile.Id == "user1");
        Assert.Equal(ParticipantRole.Owner, creatorParticipant.Role);

        // Others should be members
        Assert.All(conv.Participants.Where(p => p.Profile.Id != "user1"),
            p => Assert.Equal(ParticipantRole.Member, p.Role));
    }

    [Fact]
    public async Task CreateGroup_CreatorNotInList_AddsCreator()
    {
        var creator = User("user1", "Alice");
        var participants = new List<EntityRefDto>
        {
            User("user2", "Bob"),
            User("user3", "Charlie")
        };

        var conv = await _service.CreateGroupConversationAsync(new CreateConversationRequest
        {
            TenantId = "tenant1",
            Creator = creator,
            Type = ConversationType.Group,
            Participants = participants,
            Title = "Test Group"
        });

        Assert.Equal(3, conv.Participants.Count);
        Assert.Contains(conv.Participants, p => p.Profile.Id == "user1");
    }

    [Fact]
    public async Task AddParticipant_AsOwner_AddsSuccessfully()
    {
        var creator = User("user1", "Alice");
        var conv = await _service.CreateGroupConversationAsync(new CreateConversationRequest
        {
            TenantId = "tenant1",
            Creator = creator,
            Type = ConversationType.Group,
            Participants = [creator, User("user2", "Bob")],
            Title = "Test Group"
        });

        var newUser = User("user3", "Charlie");
        await _service.AddParticipantAsync(new AddParticipantRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Actor = creator,
            NewParticipant = newUser
        });

        var updated = await _service.GetConversationAsync("tenant1", conv.Id!, creator);
        Assert.Equal(3, updated!.Participants.Count(p => !p.HasLeft));
    }

    [Fact]
    public async Task AddParticipant_AsMember_ThrowsNotAuthorized()
    {
        var creator = User("user1", "Alice");
        var member = User("user2", "Bob");

        var conv = await _service.CreateGroupConversationAsync(new CreateConversationRequest
        {
            TenantId = "tenant1",
            Creator = creator,
            Type = ConversationType.Group,
            Participants = [creator, member],
            Title = "Test Group"
        });

        var ex = await Assert.ThrowsAsync<ChatValidationException>(() =>
            _service.AddParticipantAsync(new AddParticipantRequest
            {
                TenantId = "tenant1",
                ConversationId = conv.Id!,
                Actor = member,
                NewParticipant = User("user3", "Charlie")
            }));

        Assert.Equal(ChatValidationError.NotAuthorized, ex.Error);
    }

    [Fact]
    public async Task RemoveParticipant_AsOwner_RemovesSuccessfully()
    {
        var creator = User("user1", "Alice");
        var member = User("user2", "Bob");

        var conv = await _service.CreateGroupConversationAsync(new CreateConversationRequest
        {
            TenantId = "tenant1",
            Creator = creator,
            Type = ConversationType.Group,
            Participants = [creator, member],
            Title = "Test Group"
        });

        await _service.RemoveParticipantAsync(new RemoveParticipantRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Actor = creator,
            Participant = member
        });

        var updated = await _service.GetConversationAsync("tenant1", conv.Id!, creator);
        var removedParticipant = updated!.Participants.First(p => p.Profile.Id == "user2");
        Assert.True(removedParticipant.HasLeft);
    }

    [Fact]
    public async Task RemoveParticipant_CannotRemoveOwner_ThrowsException()
    {
        var creator = User("user1", "Alice");
        var member = User("user2", "Bob");

        var conv = await _service.CreateGroupConversationAsync(new CreateConversationRequest
        {
            TenantId = "tenant1",
            Creator = creator,
            Type = ConversationType.Group,
            Participants = [creator, member],
            Title = "Test Group"
        });

        var ex = await Assert.ThrowsAsync<ChatValidationException>(() =>
            _service.RemoveParticipantAsync(new RemoveParticipantRequest
            {
                TenantId = "tenant1",
                ConversationId = conv.Id!,
                Actor = creator,
                Participant = creator
            }));

        Assert.Equal(ChatValidationError.CannotRemoveOwner, ex.Error);
    }

    [Fact]
    public async Task LeaveConversation_MemberLeaves_MarkedAsLeft()
    {
        var creator = User("user1", "Alice");
        var member = User("user2", "Bob");

        var conv = await _service.CreateGroupConversationAsync(new CreateConversationRequest
        {
            TenantId = "tenant1",
            Creator = creator,
            Type = ConversationType.Group,
            Participants = [creator, member],
            Title = "Test Group"
        });

        await _service.LeaveConversationAsync("tenant1", conv.Id!, member);

        var updated = await _service.GetConversationAsync("tenant1", conv.Id!, creator);
        var leftParticipant = updated!.Participants.First(p => p.Profile.Id == "user2");
        Assert.True(leftParticipant.HasLeft);
        Assert.NotNull(leftParticipant.LeftAt);
    }

    [Fact]
    public async Task UpdateConversation_ChangeTitle_UpdatesSuccessfully()
    {
        var creator = User("user1", "Alice");

        var conv = await _service.CreateGroupConversationAsync(new CreateConversationRequest
        {
            TenantId = "tenant1",
            Creator = creator,
            Type = ConversationType.Group,
            Participants = [creator, User("user2", "Bob")],
            Title = "Original Title"
        });

        var updated = await _service.UpdateConversationAsync(new UpdateConversationRequest
        {
            TenantId = "tenant1",
            ConversationId = conv.Id!,
            Actor = creator,
            Title = "New Title"
        });

        Assert.Equal("New Title", updated.Title);
        Assert.True(_notifier.ConversationsUpdated.Count > 0);
    }

    [Fact]
    public async Task GetConversations_FiltersActiveParticipant()
    {
        var user1 = User("user1", "Alice");
        var user2 = User("user2", "Bob");
        var user3 = User("user3", "Charlie");

        // Create first group with user1 and user2
        await _service.CreateGroupConversationAsync(new CreateConversationRequest
        {
            TenantId = "tenant1",
            Creator = user1,
            Type = ConversationType.Group,
            Participants = [user1, user2],
            Title = "Group 1"
        });

        // Create second group with user1 and user3
        await _service.CreateGroupConversationAsync(new CreateConversationRequest
        {
            TenantId = "tenant1",
            Creator = user1,
            Type = ConversationType.Group,
            Participants = [user1, user3],
            Title = "Group 2"
        });

        // User1 should see both
        var user1Convs = await _service.GetConversationsAsync(new ConversationQuery
        {
            TenantId = "tenant1",
            Participant = user1
        });
        Assert.Equal(2, user1Convs.Items.Count);

        // User2 should see only first
        var user2Convs = await _service.GetConversationsAsync(new ConversationQuery
        {
            TenantId = "tenant1",
            Participant = user2
        });
        Assert.Single(user2Convs.Items);
        Assert.Equal("Group 1", user2Convs.Items[0].Title);
    }
}
