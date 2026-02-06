using System.Collections.Concurrent;
using Realtime.Abstractions;

namespace Realtime.Transport.InMemory;

/// <summary>
/// In-memory connection manager for testing.
/// Thread-safe using ConcurrentDictionary.
/// </summary>
public sealed class InMemoryConnectionManager : IConnectionManager
{
    private readonly ConcurrentDictionary<string, ConnectionInfo> _connections = new();

    /// <summary>
    /// Event fired when a connection is added. For presence tracking integration.
    /// </summary>
    public event EventHandler<(string TenantId, EntityRefDto Profile, int PreviousCount)>? ConnectionAdded;

    /// <summary>
    /// Event fired when a connection is removed. For presence tracking integration.
    /// </summary>
    public event EventHandler<(string TenantId, EntityRefDto Profile, int RemainingCount)>? ConnectionRemoved;

    /// <inheritdoc />
    public Task<ConnectionInfo> AddConnectionAsync(
        string connectionId,
        string tenantId,
        EntityRefDto profile,
        Dictionary<string, string>? metadata = null,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        // Count existing connections for this profile (before adding)
        var previousCount = _connections.Values
            .Count(c => c.TenantId == tenantId && c.Profile.Id == profile.Id);

        var info = new ConnectionInfo
        {
            ConnectionId = connectionId,
            TenantId = tenantId,
            Profile = profile,
            ConnectedAt = now,
            LastActivityAt = now,
            TransportType = "InMemory",
            Metadata = metadata
        };

        _connections[connectionId] = info;

        ConnectionAdded?.Invoke(this, (tenantId, profile, previousCount));

        return Task.FromResult(info);
    }

    /// <inheritdoc />
    public Task RemoveConnectionAsync(string connectionId, CancellationToken ct = default)
    {
        if (_connections.TryRemove(connectionId, out var info))
        {
            // Count remaining connections for this profile
            var remainingCount = _connections.Values
                .Count(c => c.TenantId == info.TenantId && c.Profile.Id == info.Profile.Id);

            ConnectionRemoved?.Invoke(this, (info.TenantId, info.Profile, remainingCount));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task TouchConnectionAsync(string connectionId, CancellationToken ct = default)
    {
        if (_connections.TryGetValue(connectionId, out var info))
        {
            // Update last activity - create new instance to maintain immutability pattern
            var updated = new ConnectionInfo
            {
                ConnectionId = info.ConnectionId,
                TenantId = info.TenantId,
                Profile = info.Profile,
                ConnectedAt = info.ConnectedAt,
                LastActivityAt = DateTimeOffset.UtcNow,
                TransportType = info.TransportType,
                Metadata = info.Metadata
            };
            _connections[connectionId] = updated;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ConnectionInfo>> GetConnectionsForProfileAsync(
        string tenantId,
        EntityRefDto profile,
        CancellationToken ct = default)
    {
        var result = _connections.Values
            .Where(c => c.TenantId == tenantId && c.Profile.Id == profile.Id)
            .ToList();

        return Task.FromResult<IReadOnlyList<ConnectionInfo>>(result);
    }

    /// <inheritdoc />
    public Task<ConnectionInfo?> GetConnectionAsync(string connectionId, CancellationToken ct = default)
    {
        _connections.TryGetValue(connectionId, out var info);
        return Task.FromResult(info);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ConnectionInfo>> GetTenantConnectionsAsync(
        string tenantId,
        CancellationToken ct = default)
    {
        var result = _connections.Values
            .Where(c => c.TenantId == tenantId)
            .ToList();

        return Task.FromResult<IReadOnlyList<ConnectionInfo>>(result);
    }

    /// <inheritdoc />
    public Task<int> GetConnectionCountAsync(
        string tenantId,
        EntityRefDto profile,
        CancellationToken ct = default)
    {
        var count = _connections.Values
            .Count(c => c.TenantId == tenantId && c.Profile.Id == profile.Id);

        return Task.FromResult(count);
    }

    /// <summary>
    /// Clears all connections. For testing.
    /// </summary>
    public void Clear() => _connections.Clear();

    /// <summary>
    /// Gets total connection count. For testing.
    /// </summary>
    public int Count => _connections.Count;
}
