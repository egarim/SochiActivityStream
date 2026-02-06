using System.Collections.Concurrent;
using Identity.Abstractions;

namespace Identity.Store.InMemory;

/// <summary>
/// In-memory implementation of the user store.
/// </summary>
public sealed class InMemoryUserStore : IUserStore
{
    private readonly ConcurrentDictionary<string, UserRecord> _usersById = new();
    private readonly ConcurrentDictionary<string, string> _emailIndex = new(); // normalized email -> userId
    private readonly ConcurrentDictionary<string, string> _usernameIndex = new(); // normalized username -> userId
    private readonly object _lock = new();

    /// <inheritdoc />
    public Task<UserRecord?> FindByUsernameAsync(string usernameNormalized, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(usernameNormalized))
            return Task.FromResult<UserRecord?>(null);

        lock (_lock)
        {
            if (_usernameIndex.TryGetValue(usernameNormalized, out var userId))
            {
                if (_usersById.TryGetValue(userId, out var record))
                    return Task.FromResult<UserRecord?>(record);
            }
        }

        return Task.FromResult<UserRecord?>(null);
    }

    /// <inheritdoc />
    public Task<UserRecord?> FindByEmailAsync(string emailNormalized, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(emailNormalized))
            return Task.FromResult<UserRecord?>(null);

        lock (_lock)
        {
            if (_emailIndex.TryGetValue(emailNormalized, out var userId))
            {
                if (_usersById.TryGetValue(userId, out var record))
                    return Task.FromResult<UserRecord?>(record);
            }
        }

        return Task.FromResult<UserRecord?>(null);
    }

    /// <inheritdoc />
    public Task<UserRecord?> GetByIdAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId))
            return Task.FromResult<UserRecord?>(null);

        lock (_lock)
        {
            if (_usersById.TryGetValue(userId, out var record))
                return Task.FromResult<UserRecord?>(record);
        }

        return Task.FromResult<UserRecord?>(null);
    }

    /// <inheritdoc />
    public Task CreateAsync(UserRecord record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(record.User);

        if (string.IsNullOrEmpty(record.User.Id))
            throw new ArgumentException("User must have an Id.", nameof(record));

        lock (_lock)
        {
            if (!_usersById.TryAdd(record.User.Id, record))
                throw new InvalidOperationException($"User with Id '{record.User.Id}' already exists.");

            _emailIndex[record.User.Email.ToLowerInvariant()] = record.User.Id;
            _usernameIndex[record.User.Username.ToLowerInvariant()] = record.User.Id;
        }

        return Task.CompletedTask;
    }
}
