using Realtime.Abstractions;
using Realtime.Transport.InMemory;
using Xunit;

namespace Realtime.Tests;

/// <summary>
/// Tests for InMemoryConnectionManager.
/// </summary>
public class ConnectionManagerTests
{
    private readonly InMemoryConnectionManager _manager;

    public ConnectionManagerTests()
    {
        _manager = new InMemoryConnectionManager();
    }

    [Fact]
    public async Task AddConnection_ReturnsConnectionInfo()
    {
        // Arrange
        var profile = EntityRefDto.Profile("user1", "John Doe");

        // Act
        var info = await _manager.AddConnectionAsync("conn1", "tenant1", profile);

        // Assert
        Assert.Equal("conn1", info.ConnectionId);
        Assert.Equal("tenant1", info.TenantId);
        Assert.Equal("user1", info.Profile.Id);
        Assert.Equal("InMemory", info.TransportType);
        Assert.True(info.ConnectedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task AddConnection_WithMetadata_StoresMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            ["device"] = "iPhone",
            ["platform"] = "iOS"
        };

        // Act
        var info = await _manager.AddConnectionAsync("conn1", "tenant1", EntityRefDto.Profile("user1"), metadata);

        // Assert
        Assert.NotNull(info.Metadata);
        Assert.Equal("iPhone", info.Metadata["device"]);
        Assert.Equal("iOS", info.Metadata["platform"]);
    }

    [Fact]
    public async Task GetConnection_ReturnsStoredConnection()
    {
        // Arrange
        await _manager.AddConnectionAsync("conn1", "tenant1", EntityRefDto.Profile("user1"));

        // Act
        var info = await _manager.GetConnectionAsync("conn1");

        // Assert
        Assert.NotNull(info);
        Assert.Equal("conn1", info.ConnectionId);
    }

    [Fact]
    public async Task GetConnection_NotFound_ReturnsNull()
    {
        // Act
        var info = await _manager.GetConnectionAsync("nonexistent");

        // Assert
        Assert.Null(info);
    }

    [Fact]
    public async Task RemoveConnection_RemovesFromStore()
    {
        // Arrange
        await _manager.AddConnectionAsync("conn1", "tenant1", EntityRefDto.Profile("user1"));

        // Act
        await _manager.RemoveConnectionAsync("conn1");

        // Assert
        var info = await _manager.GetConnectionAsync("conn1");
        Assert.Null(info);
    }

    [Fact]
    public async Task RemoveConnection_Nonexistent_NoError()
    {
        // Act - should not throw
        await _manager.RemoveConnectionAsync("nonexistent");

        // Assert - no exception
        Assert.Equal(0, _manager.Count);
    }

    [Fact]
    public async Task GetConnectionsForProfile_ReturnsMatchingConnections()
    {
        // Arrange
        var user1 = EntityRefDto.Profile("user1");
        var user2 = EntityRefDto.Profile("user2");
        await _manager.AddConnectionAsync("conn1", "tenant1", user1);
        await _manager.AddConnectionAsync("conn2", "tenant1", user1);
        await _manager.AddConnectionAsync("conn3", "tenant1", user2);

        // Act
        var connections = await _manager.GetConnectionsForProfileAsync("tenant1", user1);

        // Assert
        Assert.Equal(2, connections.Count);
        Assert.All(connections, c => Assert.Equal("user1", c.Profile.Id));
    }

    [Fact]
    public async Task GetConnectionsForProfile_TenantIsolation()
    {
        // Arrange - same profile ID in different tenants
        var profile = EntityRefDto.Profile("user1");
        await _manager.AddConnectionAsync("conn1", "tenant1", profile);
        await _manager.AddConnectionAsync("conn2", "tenant2", profile);

        // Act
        var tenant1Connections = await _manager.GetConnectionsForProfileAsync("tenant1", profile);
        var tenant2Connections = await _manager.GetConnectionsForProfileAsync("tenant2", profile);

        // Assert
        Assert.Single(tenant1Connections);
        Assert.Equal("conn1", tenant1Connections[0].ConnectionId);
        Assert.Single(tenant2Connections);
        Assert.Equal("conn2", tenant2Connections[0].ConnectionId);
    }

    [Fact]
    public async Task GetTenantConnections_ReturnsAllInTenant()
    {
        // Arrange
        await _manager.AddConnectionAsync("conn1", "tenant1", EntityRefDto.Profile("user1"));
        await _manager.AddConnectionAsync("conn2", "tenant1", EntityRefDto.Profile("user2"));
        await _manager.AddConnectionAsync("conn3", "tenant2", EntityRefDto.Profile("user3"));

        // Act
        var connections = await _manager.GetTenantConnectionsAsync("tenant1");

        // Assert
        Assert.Equal(2, connections.Count);
        Assert.All(connections, c => Assert.Equal("tenant1", c.TenantId));
    }

    [Fact]
    public async Task GetConnectionCount_ReturnsCorrectCount()
    {
        // Arrange
        var profile = EntityRefDto.Profile("user1");
        await _manager.AddConnectionAsync("conn1", "tenant1", profile);
        await _manager.AddConnectionAsync("conn2", "tenant1", profile);

        // Act
        var count = await _manager.GetConnectionCountAsync("tenant1", profile);

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task TouchConnection_UpdatesLastActivity()
    {
        // Arrange
        await _manager.AddConnectionAsync("conn1", "tenant1", EntityRefDto.Profile("user1"));
        var before = (await _manager.GetConnectionAsync("conn1"))!.LastActivityAt;

        await Task.Delay(10); // Small delay to ensure time difference

        // Act
        await _manager.TouchConnectionAsync("conn1");

        // Assert
        var after = (await _manager.GetConnectionAsync("conn1"))!.LastActivityAt;
        Assert.True(after > before);
    }

    [Fact]
    public async Task ConnectionAdded_EventFires()
    {
        // Arrange
        (string TenantId, EntityRefDto Profile, int PreviousCount)? capturedArgs = null;
        _manager.ConnectionAdded += (_, args) => capturedArgs = args;

        // Act
        await _manager.AddConnectionAsync("conn1", "tenant1", EntityRefDto.Profile("user1"));

        // Assert
        Assert.NotNull(capturedArgs);
        Assert.Equal("tenant1", capturedArgs.Value.TenantId);
        Assert.Equal("user1", capturedArgs.Value.Profile.Id);
        Assert.Equal(0, capturedArgs.Value.PreviousCount);
    }

    [Fact]
    public async Task ConnectionAdded_PreviousCountCorrect()
    {
        // Arrange
        var profile = EntityRefDto.Profile("user1");
        await _manager.AddConnectionAsync("conn1", "tenant1", profile);

        int? previousCount = null;
        _manager.ConnectionAdded += (_, args) => previousCount = args.PreviousCount;

        // Act
        await _manager.AddConnectionAsync("conn2", "tenant1", profile);

        // Assert
        Assert.Equal(1, previousCount);
    }

    [Fact]
    public async Task ConnectionRemoved_EventFires()
    {
        // Arrange
        var profile = EntityRefDto.Profile("user1");
        await _manager.AddConnectionAsync("conn1", "tenant1", profile);

        (string TenantId, EntityRefDto Profile, int RemainingCount)? capturedArgs = null;
        _manager.ConnectionRemoved += (_, args) => capturedArgs = args;

        // Act
        await _manager.RemoveConnectionAsync("conn1");

        // Assert
        Assert.NotNull(capturedArgs);
        Assert.Equal("tenant1", capturedArgs.Value.TenantId);
        Assert.Equal("user1", capturedArgs.Value.Profile.Id);
        Assert.Equal(0, capturedArgs.Value.RemainingCount);
    }

    [Fact]
    public async Task ConnectionRemoved_RemainingCountCorrect()
    {
        // Arrange
        var profile = EntityRefDto.Profile("user1");
        await _manager.AddConnectionAsync("conn1", "tenant1", profile);
        await _manager.AddConnectionAsync("conn2", "tenant1", profile);

        int? remainingCount = null;
        _manager.ConnectionRemoved += (_, args) => remainingCount = args.RemainingCount;

        // Act
        await _manager.RemoveConnectionAsync("conn1");

        // Assert
        Assert.Equal(1, remainingCount);
    }
}
