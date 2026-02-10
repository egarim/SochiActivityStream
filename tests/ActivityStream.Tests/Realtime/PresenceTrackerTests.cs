using Realtime.Abstractions;
using Realtime.Core;
using Realtime.Transport.InMemory;
using Xunit;

namespace ActivityStream.Tests.Realtime;

/// <summary>
/// Tests for PresenceTrackerCore.
/// </summary>
public class PresenceTrackerTests
{
    private readonly InMemoryConnectionManager _connections;
    private readonly PresenceTrackerCore _presence;

    public PresenceTrackerTests()
    {
        _connections = new InMemoryConnectionManager();
        _presence = new PresenceTrackerCore(_connections, new RealtimeServiceOptions { IdleTimeoutMinutes = 5 });
    }

    [Fact]
    public async Task GetPresence_NoConnections_ReturnsOffline()
    {
        // Act
        var status = await _presence.GetPresenceAsync("tenant1", EntityRefDto.Profile("user1"));

        // Assert
        Assert.Equal(PresenceStatus.Offline, status);
    }

    [Fact]
    public async Task GetPresence_HasConnection_ReturnsOnline()
    {
        // Arrange
        var profile = EntityRefDto.Profile("user1");
        await _connections.AddConnectionAsync("conn1", "tenant1", profile);

        // Act
        var status = await _presence.GetPresenceAsync("tenant1", profile);

        // Assert
        Assert.Equal(PresenceStatus.Online, status);
    }

    [Fact]
    public async Task GetPresence_MultipleConnections_ReturnsOnline()
    {
        // Arrange
        var profile = EntityRefDto.Profile("user1");
        await _connections.AddConnectionAsync("conn1", "tenant1", profile);
        await _connections.AddConnectionAsync("conn2", "tenant1", profile);

        // Act
        var status = await _presence.GetPresenceAsync("tenant1", profile);

        // Assert
        Assert.Equal(PresenceStatus.Online, status);
    }

    [Fact]
    public async Task GetPresence_AfterDisconnect_ReturnsOffline()
    {
        // Arrange
        var profile = EntityRefDto.Profile("user1");
        await _connections.AddConnectionAsync("conn1", "tenant1", profile);
        await _connections.RemoveConnectionAsync("conn1");

        // Act
        var status = await _presence.GetPresenceAsync("tenant1", profile);

        // Assert
        Assert.Equal(PresenceStatus.Offline, status);
    }

    [Fact]
    public async Task GetPresenceBatch_ReturnsAllStatuses()
    {
        // Arrange
        var user1 = EntityRefDto.Profile("user1");
        var user2 = EntityRefDto.Profile("user2");
        var user3 = EntityRefDto.Profile("user3");

        await _connections.AddConnectionAsync("conn1", "tenant1", user1);
        // user2 has no connections
        await _connections.AddConnectionAsync("conn2", "tenant1", user3);

        // Act
        var statuses = await _presence.GetPresenceBatchAsync("tenant1", [user1, user2, user3]);

        // Assert
        Assert.Equal(3, statuses.Count);
        Assert.Equal(PresenceStatus.Online, statuses["user1"]);
        Assert.Equal(PresenceStatus.Offline, statuses["user2"]);
        Assert.Equal(PresenceStatus.Online, statuses["user3"]);
    }

    [Fact]
    public async Task GetOnlineProfiles_ReturnsOnlyOnlineUsers()
    {
        // Arrange
        await _connections.AddConnectionAsync("conn1", "tenant1", EntityRefDto.Profile("user1"));
        await _connections.AddConnectionAsync("conn2", "tenant1", EntityRefDto.Profile("user2"));
        // user3 has no connections

        // Act
        var online = await _presence.GetOnlineProfilesAsync("tenant1");

        // Assert
        Assert.Equal(2, online.Count);
        Assert.Contains(online, p => p.Id == "user1");
        Assert.Contains(online, p => p.Id == "user2");
    }

    [Fact]
    public async Task GetOnlineProfiles_DeduplicatesMultipleConnections()
    {
        // Arrange - same user with multiple connections
        var profile = EntityRefDto.Profile("user1");
        await _connections.AddConnectionAsync("conn1", "tenant1", profile);
        await _connections.AddConnectionAsync("conn2", "tenant1", profile);

        // Act
        var online = await _presence.GetOnlineProfilesAsync("tenant1");

        // Assert
        Assert.Single(online);
        Assert.Equal("user1", online[0].Id);
    }

    [Fact]
    public async Task GetOnlineProfiles_TenantIsolation()
    {
        // Arrange
        await _connections.AddConnectionAsync("conn1", "tenant1", EntityRefDto.Profile("user1"));
        await _connections.AddConnectionAsync("conn2", "tenant2", EntityRefDto.Profile("user2"));

        // Act
        var tenant1Online = await _presence.GetOnlineProfilesAsync("tenant1");
        var tenant2Online = await _presence.GetOnlineProfilesAsync("tenant2");

        // Assert
        Assert.Single(tenant1Online);
        Assert.Equal("user1", tenant1Online[0].Id);
        Assert.Single(tenant2Online);
        Assert.Equal("user2", tenant2Online[0].Id);
    }

    [Fact]
    public void OnConnectionAdded_FirstConnection_FiresPresenceChanged()
    {
        // Arrange
        PresenceChangedEventArgs? args = null;
        _presence.PresenceChanged += (_, e) => args = e;

        // Act
        _presence.OnConnectionAdded("tenant1", EntityRefDto.Profile("user1"), 0);

        // Assert
        Assert.NotNull(args);
        Assert.Equal("tenant1", args.TenantId);
        Assert.Equal("user1", args.Profile.Id);
        Assert.Equal(PresenceStatus.Offline, args.OldStatus);
        Assert.Equal(PresenceStatus.Online, args.NewStatus);
    }

    [Fact]
    public void OnConnectionAdded_SecondConnection_NoEvent()
    {
        // Arrange
        PresenceChangedEventArgs? args = null;
        _presence.PresenceChanged += (_, e) => args = e;

        // Act
        _presence.OnConnectionAdded("tenant1", EntityRefDto.Profile("user1"), 1);

        // Assert
        Assert.Null(args);
    }

    [Fact]
    public void OnConnectionRemoved_LastConnection_FiresPresenceChanged()
    {
        // Arrange
        PresenceChangedEventArgs? args = null;
        _presence.PresenceChanged += (_, e) => args = e;

        // Act
        _presence.OnConnectionRemoved("tenant1", EntityRefDto.Profile("user1"), 0);

        // Assert
        Assert.NotNull(args);
        Assert.Equal(PresenceStatus.Online, args.OldStatus);
        Assert.Equal(PresenceStatus.Offline, args.NewStatus);
    }

    [Fact]
    public void OnConnectionRemoved_StillHasConnections_NoEvent()
    {
        // Arrange
        PresenceChangedEventArgs? args = null;
        _presence.PresenceChanged += (_, e) => args = e;

        // Act
        _presence.OnConnectionRemoved("tenant1", EntityRefDto.Profile("user1"), 1);

        // Assert
        Assert.Null(args);
    }
}
