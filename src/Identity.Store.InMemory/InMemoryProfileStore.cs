using System.Collections.Concurrent;
using Identity.Abstractions;

namespace Identity.Store.InMemory;

/// <summary>
/// In-memory implementation of the profile store.
/// </summary>
public sealed class InMemoryProfileStore : IProfileStore
{
    private readonly ConcurrentDictionary<string, ProfileRecord> _profilesById = new();
    private readonly ConcurrentDictionary<string, string> _handleIndex = new(); // normalized handle -> profileId
    private readonly object _lock = new();

    /// <inheritdoc />
    public Task<ProfileRecord?> GetByIdAsync(string profileId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(profileId))
            return Task.FromResult<ProfileRecord?>(null);

        lock (_lock)
        {
            if (_profilesById.TryGetValue(profileId, out var record))
                return Task.FromResult<ProfileRecord?>(record);
        }

        return Task.FromResult<ProfileRecord?>(null);
    }

    /// <inheritdoc />
    public Task<ProfileRecord?> FindByHandleAsync(string handleNormalized, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(handleNormalized))
            return Task.FromResult<ProfileRecord?>(null);

        lock (_lock)
        {
            if (_handleIndex.TryGetValue(handleNormalized, out var profileId))
            {
                if (_profilesById.TryGetValue(profileId, out var record))
                    return Task.FromResult<ProfileRecord?>(record);
            }
        }

        return Task.FromResult<ProfileRecord?>(null);
    }

    /// <inheritdoc />
    public Task CreateAsync(ProfileRecord record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(record.Profile);

        if (string.IsNullOrEmpty(record.Profile.Id))
            throw new ArgumentException("Profile must have an Id.", nameof(record));

        lock (_lock)
        {
            if (!_profilesById.TryAdd(record.Profile.Id, record))
                throw new InvalidOperationException($"Profile with Id '{record.Profile.Id}' already exists.");

            _handleIndex[record.Profile.Handle.ToLowerInvariant()] = record.Profile.Id;
        }

        return Task.CompletedTask;
    }
}
