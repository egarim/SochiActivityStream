using Realtime.Abstractions;
using Realtime.Core;
using Realtime.Transport.InMemory;
using Xunit;

namespace Realtime.Tests;

/// <summary>
/// Tests for RealtimePublisher.
/// </summary>
public class PublisherTests
{
    private readonly InMemoryConnectionManager _connections;
    private readonly InMemoryEventDispatcher _dispatcher;
    private readonly RealtimePublisher _publisher;

    public PublisherTests()
    {
        _connections = new InMemoryConnectionManager();
        _dispatcher = new InMemoryEventDispatcher(_connections);
        _publisher = new RealtimePublisher(
            _dispatcher,
            _connections,
            new NullGroupMembershipResolver(),
            new UlidIdGenerator());
    }

    [Fact]
    public async Task SendToProfile_DeliversToSingleConnection()
    {
        // Arrange
        var profile = EntityRefDto.Profile("user1");
        await _connections.AddConnectionAsync("conn1", "tenant1", profile);

        // Act
        await _publisher.SendToProfileAsync("tenant1", profile, "test.event", new { data = 123 });

        // Assert
        Assert.Single(_dispatcher.DispatchedEvents);
        Assert.Contains("conn1", _dispatcher.DispatchedEvents[0].ConnectionIds);
        Assert.Equal("test.event", _dispatcher.DispatchedEvents[0].Event.Type);
    }

    [Fact]
    public async Task SendToProfile_DeliversToMultipleConnections()
    {
        // Arrange - same user with 2 connections (e.g., phone + browser)
        var profile = EntityRefDto.Profile("user1");
        await _connections.AddConnectionAsync("conn1", "tenant1", profile);
        await _connections.AddConnectionAsync("conn2", "tenant1", profile);

        // Act
        await _publisher.SendToProfileAsync("tenant1", profile, "test.event", new { data = 456 });

        // Assert
        Assert.Single(_dispatcher.DispatchedEvents);
        var dispatched = _dispatcher.DispatchedEvents[0];
        Assert.Contains("conn1", dispatched.ConnectionIds);
        Assert.Contains("conn2", dispatched.ConnectionIds);
    }

    [Fact]
    public async Task SendToProfile_NoConnectionsNoDispatch()
    {
        // Act - send to user with no connections
        await _publisher.SendToProfileAsync("tenant1", EntityRefDto.Profile("offline-user"), "test.event", new { });

        // Assert
        Assert.Empty(_dispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task SendToProfiles_DeliversToAllProfiles()
    {
        // Arrange
        var user1 = EntityRefDto.Profile("user1");
        var user2 = EntityRefDto.Profile("user2");
        await _connections.AddConnectionAsync("conn1", "tenant1", user1);
        await _connections.AddConnectionAsync("conn2", "tenant1", user2);

        // Act
        await _publisher.SendToProfilesAsync("tenant1", [user1, user2], "group.event", new { msg = "hello" });

        // Assert
        Assert.Single(_dispatcher.DispatchedEvents);
        var dispatched = _dispatcher.DispatchedEvents[0];
        Assert.Contains("conn1", dispatched.ConnectionIds);
        Assert.Contains("conn2", dispatched.ConnectionIds);
    }

    [Fact]
    public async Task SendToProfiles_DeduplicatesConnections()
    {
        // Arrange - same profile listed twice
        var user1 = EntityRefDto.Profile("user1");
        await _connections.AddConnectionAsync("conn1", "tenant1", user1);

        // Act
        await _publisher.SendToProfilesAsync("tenant1", [user1, user1], "test.event", new { });

        // Assert - should only dispatch once to conn1
        Assert.Single(_dispatcher.DispatchedEvents);
        Assert.Single(_dispatcher.DispatchedEvents[0].ConnectionIds);
    }

    [Fact]
    public async Task SendToProfiles_EmptyListNoDispatch()
    {
        // Act
        await _publisher.SendToProfilesAsync("tenant1", [], "test.event", new { });

        // Assert
        Assert.Empty(_dispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task Broadcast_DeliversToAllTenantConnections()
    {
        // Arrange
        await _connections.AddConnectionAsync("conn1", "tenant1", EntityRefDto.Profile("user1"));
        await _connections.AddConnectionAsync("conn2", "tenant1", EntityRefDto.Profile("user2"));
        await _connections.AddConnectionAsync("conn3", "tenant2", EntityRefDto.Profile("user3")); // different tenant

        // Act
        await _publisher.BroadcastAsync("tenant1", "announcement", new { text = "Hello all!" });

        // Assert
        Assert.Single(_dispatcher.DispatchedEvents);
        var dispatched = _dispatcher.DispatchedEvents[0];
        Assert.Equal(2, dispatched.ConnectionIds.Count);
        Assert.Contains("conn1", dispatched.ConnectionIds);
        Assert.Contains("conn2", dispatched.ConnectionIds);
        Assert.DoesNotContain("conn3", dispatched.ConnectionIds);
    }

    [Fact]
    public async Task Publish_AssignsIdIfMissing()
    {
        // Arrange
        await _connections.AddConnectionAsync("conn1", "tenant1", EntityRefDto.Profile("user1"));
        var evt = new RealtimeEvent
        {
            TenantId = "tenant1",
            Type = "test.event",
            Payload = new { data = 1 },
            Target = EventTarget.ToProfile(EntityRefDto.Profile("user1"))
        };

        // Act
        await _publisher.PublishAsync(evt);

        // Assert
        Assert.NotNull(_dispatcher.DispatchedEvents[0].Event.Id);
        Assert.Equal(26, _dispatcher.DispatchedEvents[0].Event.Id!.Length); // ULID length
    }

    [Fact]
    public async Task Publish_PreservesExistingId()
    {
        // Arrange
        await _connections.AddConnectionAsync("conn1", "tenant1", EntityRefDto.Profile("user1"));
        var evt = new RealtimeEvent
        {
            Id = "my-custom-id",
            TenantId = "tenant1",
            Type = "test.event",
            Payload = new { data = 1 },
            Target = EventTarget.ToProfile(EntityRefDto.Profile("user1"))
        };

        // Act
        await _publisher.PublishAsync(evt);

        // Assert
        Assert.Equal("my-custom-id", _dispatcher.DispatchedEvents[0].Event.Id);
    }

    [Fact]
    public async Task Publish_SetsTimestamp()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;
        await _connections.AddConnectionAsync("conn1", "tenant1", EntityRefDto.Profile("user1"));

        // Act
        await _publisher.SendToProfileAsync("tenant1", EntityRefDto.Profile("user1"), "test.event", new { });
        var after = DateTimeOffset.UtcNow;

        // Assert
        var timestamp = _dispatcher.DispatchedEvents[0].Event.Timestamp;
        Assert.True(timestamp >= before && timestamp <= after);
    }

    [Fact]
    public async Task PublishBatch_DispatchesAllEvents()
    {
        // Arrange
        var profile = EntityRefDto.Profile("user1");
        await _connections.AddConnectionAsync("conn1", "tenant1", profile);

        var events = new[]
        {
            new RealtimeEvent { TenantId = "tenant1", Type = "event1", Payload = new { }, Target = EventTarget.ToProfile(profile) },
            new RealtimeEvent { TenantId = "tenant1", Type = "event2", Payload = new { }, Target = EventTarget.ToProfile(profile) },
            new RealtimeEvent { TenantId = "tenant1", Type = "event3", Payload = new { }, Target = EventTarget.ToProfile(profile) }
        };

        // Act
        await _publisher.PublishBatchAsync(events);

        // Assert
        Assert.Equal(3, _dispatcher.DispatchedEvents.Count);
        Assert.Equal("event1", _dispatcher.DispatchedEvents[0].Event.Type);
        Assert.Equal("event2", _dispatcher.DispatchedEvents[1].Event.Type);
        Assert.Equal("event3", _dispatcher.DispatchedEvents[2].Event.Type);
    }

    [Fact]
    public async Task Publish_ToConnection_DeliversDirectly()
    {
        // Arrange
        await _connections.AddConnectionAsync("conn1", "tenant1", EntityRefDto.Profile("user1"));
        await _connections.AddConnectionAsync("conn2", "tenant1", EntityRefDto.Profile("user1"));

        var evt = new RealtimeEvent
        {
            TenantId = "tenant1",
            Type = "direct.message",
            Payload = new { ack = true },
            Target = EventTarget.ToConnection("conn1")
        };

        // Act
        await _publisher.PublishAsync(evt);

        // Assert - only conn1 should receive
        Assert.Single(_dispatcher.DispatchedEvents);
        Assert.Single(_dispatcher.DispatchedEvents[0].ConnectionIds);
        Assert.Equal("conn1", _dispatcher.DispatchedEvents[0].ConnectionIds[0]);
    }

    [Fact]
    public async Task TenantIsolation_ConnectionsAreIsolated()
    {
        // Arrange - same profile ID in different tenants
        var profile = EntityRefDto.Profile("user1");
        await _connections.AddConnectionAsync("conn1", "tenant1", profile);
        await _connections.AddConnectionAsync("conn2", "tenant2", profile);

        // Act - send to tenant1 only
        await _publisher.SendToProfileAsync("tenant1", profile, "test.event", new { });

        // Assert - only tenant1 connection receives
        Assert.Single(_dispatcher.DispatchedEvents);
        Assert.Single(_dispatcher.DispatchedEvents[0].ConnectionIds);
        Assert.Equal("conn1", _dispatcher.DispatchedEvents[0].ConnectionIds[0]);
    }
}
