using System.Collections.Concurrent;
using Identity.Abstractions;

namespace Identity.Store.InMemory;

/// <summary>
/// In-memory implementation of the session store.
/// </summary>
public sealed class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<string, SessionRecord> _sessionsById = new();
    private readonly ConcurrentDictionary<string, string> _tokenIndex = new(); // accessToken -> sessionId
    private readonly object _lock = new();

    /// <inheritdoc />
    public Task CreateAsync(SessionRecord record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(record.Session);

        if (string.IsNullOrEmpty(record.Session.SessionId))
            throw new ArgumentException("Session must have a SessionId.", nameof(record));

        lock (_lock)
        {
            if (!_sessionsById.TryAdd(record.Session.SessionId, record))
                throw new InvalidOperationException($"Session with Id '{record.Session.SessionId}' already exists.");

            _tokenIndex[record.Session.AccessToken] = record.Session.SessionId;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<SessionRecord?> FindByAccessTokenAsync(string accessToken, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(accessToken))
            return Task.FromResult<SessionRecord?>(null);

        lock (_lock)
        {
            if (_tokenIndex.TryGetValue(accessToken, out var sessionId))
            {
                if (_sessionsById.TryGetValue(sessionId, out var record))
                    return Task.FromResult<SessionRecord?>(record);
            }
        }

        return Task.FromResult<SessionRecord?>(null);
    }

    /// <inheritdoc />
    public Task RevokeAsync(string sessionId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(sessionId))
            return Task.CompletedTask;

        lock (_lock)
        {
            if (_sessionsById.TryRemove(sessionId, out var record))
            {
                _tokenIndex.TryRemove(record.Session.AccessToken, out _);
            }
        }

        return Task.CompletedTask;
    }
}
