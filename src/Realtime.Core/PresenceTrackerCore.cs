using Realtime.Abstractions;

namespace Realtime.Core;

/// <summary>
/// Tracks presence status based on connection manager state.
/// </summary>
public sealed class PresenceTrackerCore : IPresenceTracker
{
    private readonly IConnectionManager _connections;
    private readonly RealtimeServiceOptions _options;

    /// <inheritdoc />
    public event EventHandler<PresenceChangedEventArgs>? PresenceChanged;

    public PresenceTrackerCore(IConnectionManager connections, RealtimeServiceOptions? options = null)
    {
        _connections = connections ?? throw new ArgumentNullException(nameof(connections));
        _options = options ?? new RealtimeServiceOptions();
    }

    /// <inheritdoc />
    public async Task<PresenceStatus> GetPresenceAsync(
        string tenantId,
        EntityRefDto profile,
        CancellationToken ct = default)
    {
        var connections = await _connections.GetConnectionsForProfileAsync(tenantId, profile, ct);

        if (connections.Count == 0)
            return PresenceStatus.Offline;

        // Check if any connection is active (not idle)
        var idleThreshold = DateTimeOffset.UtcNow.AddMinutes(-_options.IdleTimeoutMinutes);
        var hasActiveConnection = connections.Any(c => c.LastActivityAt >= idleThreshold);

        return hasActiveConnection ? PresenceStatus.Online : PresenceStatus.Away;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, PresenceStatus>> GetPresenceBatchAsync(
        string tenantId,
        IEnumerable<EntityRefDto> profiles,
        CancellationToken ct = default)
    {
        var result = new Dictionary<string, PresenceStatus>();

        foreach (var profile in profiles)
        {
            var status = await GetPresenceAsync(tenantId, profile, ct);
            result[profile.Id] = status;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EntityRefDto>> GetOnlineProfilesAsync(
        string tenantId,
        CancellationToken ct = default)
    {
        var connections = await _connections.GetTenantConnectionsAsync(tenantId, ct);
        var idleThreshold = DateTimeOffset.UtcNow.AddMinutes(-_options.IdleTimeoutMinutes);

        return connections
            .Where(c => c.LastActivityAt >= idleThreshold)
            .Select(c => c.Profile)
            .DistinctBy(p => p.Id)
            .ToList();
    }

    /// <summary>
    /// Call this when a connection is added to potentially fire presence changed.
    /// </summary>
    public void OnConnectionAdded(string tenantId, EntityRefDto profile, int previousCount)
    {
        if (previousCount == 0)
        {
            RaisePresenceChanged(tenantId, profile, PresenceStatus.Offline, PresenceStatus.Online);
        }
    }

    /// <summary>
    /// Call this when a connection is removed to potentially fire presence changed.
    /// </summary>
    public void OnConnectionRemoved(string tenantId, EntityRefDto profile, int remainingCount)
    {
        if (remainingCount == 0)
        {
            RaisePresenceChanged(tenantId, profile, PresenceStatus.Online, PresenceStatus.Offline);
        }
    }

    private void RaisePresenceChanged(
        string tenantId,
        EntityRefDto profile,
        PresenceStatus oldStatus,
        PresenceStatus newStatus)
    {
        PresenceChanged?.Invoke(this, new PresenceChangedEventArgs
        {
            TenantId = tenantId,
            Profile = profile,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Timestamp = DateTimeOffset.UtcNow
        });
    }
}
