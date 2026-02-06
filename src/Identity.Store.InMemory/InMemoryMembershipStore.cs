using System.Collections.Concurrent;
using Identity.Abstractions;

namespace Identity.Store.InMemory;

/// <summary>
/// In-memory implementation of the membership store.
/// </summary>
public sealed class InMemoryMembershipStore : IMembershipStore
{
    // Key: "{tenantId}|{userId}|{profileId}"
    private readonly ConcurrentDictionary<string, MembershipRecord> _memberships = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public Task<MembershipRecord?> FindAsync(string tenantId, string userId, string profileId, CancellationToken ct = default)
    {
        var key = BuildKey(tenantId, userId, profileId);

        lock (_lock)
        {
            if (_memberships.TryGetValue(key, out var record))
                return Task.FromResult<MembershipRecord?>(record);
        }

        return Task.FromResult<MembershipRecord?>(null);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<MembershipRecord>> GetForUserAsync(string tenantId, string userId, CancellationToken ct = default)
    {
        var prefix = $"{tenantId.ToLowerInvariant()}|{userId}|";

        lock (_lock)
        {
            var results = _memberships
                .Where(kv => kv.Key.StartsWith(prefix, StringComparison.Ordinal))
                .Select(kv => kv.Value)
                .ToList();
            return Task.FromResult<IReadOnlyList<MembershipRecord>>(results);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<MembershipRecord>> GetForProfileAsync(string tenantId, string profileId, CancellationToken ct = default)
    {
        var tenantPrefix = tenantId.ToLowerInvariant();
        var profileSuffix = $"|{profileId}";

        lock (_lock)
        {
            var results = _memberships
                .Where(kv => kv.Key.StartsWith(tenantPrefix + "|", StringComparison.Ordinal)
                             && kv.Key.EndsWith(profileSuffix, StringComparison.Ordinal))
                .Select(kv => kv.Value)
                .ToList();
            return Task.FromResult<IReadOnlyList<MembershipRecord>>(results);
        }
    }

    /// <inheritdoc />
    public Task UpsertAsync(MembershipRecord record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(record.Membership);

        var key = BuildKey(record.Membership.TenantId, record.Membership.UserId, record.Membership.ProfileId);

        lock (_lock)
        {
            _memberships[key] = record;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(string tenantId, string userId, string profileId, CancellationToken ct = default)
    {
        var key = BuildKey(tenantId, userId, profileId);

        lock (_lock)
        {
            _memberships.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    private static string BuildKey(string tenantId, string userId, string profileId)
    {
        return $"{tenantId.ToLowerInvariant()}|{userId}|{profileId}";
    }
}
