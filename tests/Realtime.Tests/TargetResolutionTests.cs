using Realtime.Abstractions;
using Realtime.Core;
using Realtime.Transport.InMemory;
using Xunit;

namespace Realtime.Tests;

/// <summary>
/// Tests for target resolution in RealtimePublisher.
/// </summary>
public class TargetResolutionTests
{
    private readonly InMemoryConnectionManager _connections;
    private readonly InMemoryEventDispatcher _dispatcher;
    private readonly TestGroupMembershipResolver _membershipResolver;
    private readonly RealtimePublisher _publisher;

    public TargetResolutionTests()
    {
        _connections = new InMemoryConnectionManager();
        _dispatcher = new InMemoryEventDispatcher(_connections);
        _membershipResolver = new TestGroupMembershipResolver();
        _publisher = new RealtimePublisher(
            _dispatcher,
            _connections,
            _membershipResolver,
            new UlidIdGenerator());
    }

    [Fact]
    public async Task SendToConversation_ResolvesToMemberConnections()
    {
        // Arrange
        var user1 = EntityRefDto.Profile("user1");
        var user2 = EntityRefDto.Profile("user2");
        var user3 = EntityRefDto.Profile("user3");

        _membershipResolver.SetConversationMembers("conv1", [user1, user2, user3]);

        await _connections.AddConnectionAsync("conn1", "tenant1", user1);
        await _connections.AddConnectionAsync("conn2", "tenant1", user2);
        // user3 has no connections

        // Act
        await _publisher.SendToConversationAsync("tenant1", "conv1", "message.received", new { text = "hello" });

        // Assert
        Assert.Single(_dispatcher.DispatchedEvents);
        var dispatched = _dispatcher.DispatchedEvents[0];
        Assert.Equal(2, dispatched.ConnectionIds.Count);
        Assert.Contains("conn1", dispatched.ConnectionIds);
        Assert.Contains("conn2", dispatched.ConnectionIds);
    }

    [Fact]
    public async Task SendToGroup_ResolvesToMemberConnections()
    {
        // Arrange
        var user1 = EntityRefDto.Profile("user1");
        var user2 = EntityRefDto.Profile("user2");

        _membershipResolver.SetGroupMembers("group1", [user1, user2]);

        await _connections.AddConnectionAsync("conn1", "tenant1", user1);
        await _connections.AddConnectionAsync("conn2", "tenant1", user2);

        // Act
        await _publisher.SendToGroupAsync("tenant1", "group1", "group.update", new { action = "member_joined" });

        // Assert
        Assert.Single(_dispatcher.DispatchedEvents);
        var dispatched = _dispatcher.DispatchedEvents[0];
        Assert.Equal(2, dispatched.ConnectionIds.Count);
    }

    [Fact]
    public async Task SendToConversation_NoMembers_NoDispatch()
    {
        // Arrange - conversation with no members
        _membershipResolver.SetConversationMembers("conv1", []);

        // Act
        await _publisher.SendToConversationAsync("tenant1", "conv1", "message.received", new { });

        // Assert
        Assert.Empty(_dispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task SendToConversation_MembersNoConnections_NoDispatch()
    {
        // Arrange - members exist but no one is connected
        _membershipResolver.SetConversationMembers("conv1", [
            EntityRefDto.Profile("user1"),
            EntityRefDto.Profile("user2")
        ]);
        // No connections added

        // Act
        await _publisher.SendToConversationAsync("tenant1", "conv1", "message.received", new { });

        // Assert
        Assert.Empty(_dispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task EventTargetFactory_ToProfile_CreatesCorrectTarget()
    {
        // Arrange
        var profile = EntityRefDto.Profile("user1");
        await _connections.AddConnectionAsync("conn1", "tenant1", profile);

        // Act
        var evt = new RealtimeEvent
        {
            TenantId = "tenant1",
            Type = "test",
            Payload = new { },
            Target = EventTarget.ToProfile(profile)
        };
        await _publisher.PublishAsync(evt);

        // Assert
        Assert.Single(_dispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task EventTargetFactory_ToProfiles_CreatesCorrectTarget()
    {
        // Arrange
        var profiles = new[] { EntityRefDto.Profile("user1"), EntityRefDto.Profile("user2") };
        await _connections.AddConnectionAsync("conn1", "tenant1", profiles[0]);
        await _connections.AddConnectionAsync("conn2", "tenant1", profiles[1]);

        // Act
        var evt = new RealtimeEvent
        {
            TenantId = "tenant1",
            Type = "test",
            Payload = new { },
            Target = EventTarget.ToProfiles(profiles)
        };
        await _publisher.PublishAsync(evt);

        // Assert
        Assert.Single(_dispatcher.DispatchedEvents);
        Assert.Equal(2, _dispatcher.DispatchedEvents[0].ConnectionIds.Count);
    }

    [Fact]
    public async Task EventTargetFactory_ToConversation_CreatesCorrectTarget()
    {
        // Arrange
        _membershipResolver.SetConversationMembers("conv1", [EntityRefDto.Profile("user1")]);
        await _connections.AddConnectionAsync("conn1", "tenant1", EntityRefDto.Profile("user1"));

        // Act
        var evt = new RealtimeEvent
        {
            TenantId = "tenant1",
            Type = "test",
            Payload = new { },
            Target = EventTarget.ToConversation("conv1")
        };
        await _publisher.PublishAsync(evt);

        // Assert
        Assert.Single(_dispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task EventTargetFactory_ToGroup_CreatesCorrectTarget()
    {
        // Arrange
        _membershipResolver.SetGroupMembers("group1", [EntityRefDto.Profile("user1")]);
        await _connections.AddConnectionAsync("conn1", "tenant1", EntityRefDto.Profile("user1"));

        // Act
        var evt = new RealtimeEvent
        {
            TenantId = "tenant1",
            Type = "test",
            Payload = new { },
            Target = EventTarget.ToGroup("group1")
        };
        await _publisher.PublishAsync(evt);

        // Assert
        Assert.Single(_dispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task EventTargetFactory_ToConnection_CreatesCorrectTarget()
    {
        // Arrange
        await _connections.AddConnectionAsync("conn1", "tenant1", EntityRefDto.Profile("user1"));

        // Act
        var evt = new RealtimeEvent
        {
            TenantId = "tenant1",
            Type = "test",
            Payload = new { },
            Target = EventTarget.ToConnection("conn1")
        };
        await _publisher.PublishAsync(evt);

        // Assert
        Assert.Single(_dispatcher.DispatchedEvents);
        Assert.Single(_dispatcher.DispatchedEvents[0].ConnectionIds);
        Assert.Equal("conn1", _dispatcher.DispatchedEvents[0].ConnectionIds[0]);
    }

    /// <summary>
    /// Test implementation of IGroupMembershipResolver.
    /// </summary>
    private sealed class TestGroupMembershipResolver : IGroupMembershipResolver
    {
        private readonly Dictionary<string, IReadOnlyList<EntityRefDto>> _conversationMembers = new();
        private readonly Dictionary<string, IReadOnlyList<EntityRefDto>> _groupMembers = new();

        public void SetConversationMembers(string conversationId, IReadOnlyList<EntityRefDto> members)
        {
            _conversationMembers[conversationId] = members;
        }

        public void SetGroupMembers(string groupId, IReadOnlyList<EntityRefDto> members)
        {
            _groupMembers[groupId] = members;
        }

        public Task<IReadOnlyList<EntityRefDto>> GetConversationMembersAsync(
            string tenantId,
            string conversationId,
            CancellationToken ct = default)
        {
            return Task.FromResult(_conversationMembers.GetValueOrDefault(conversationId, []));
        }

        public Task<IReadOnlyList<EntityRefDto>> GetGroupMembersAsync(
            string tenantId,
            string groupId,
            CancellationToken ct = default)
        {
            return Task.FromResult(_groupMembers.GetValueOrDefault(groupId, []));
        }
    }
}
