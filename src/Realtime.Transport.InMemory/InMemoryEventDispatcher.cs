using System.Collections.Concurrent;
using Realtime.Abstractions;

namespace Realtime.Transport.InMemory;

/// <summary>
/// Dispatched event record for testing assertions.
/// </summary>
public sealed record DispatchedEvent(
    IReadOnlyList<string> ConnectionIds,
    RealtimeEvent Event,
    DateTimeOffset DispatchedAt);

/// <summary>
/// In-memory event dispatcher for testing.
/// Collects dispatched events for assertions.
/// </summary>
public sealed class InMemoryEventDispatcher : IEventDispatcher
{
    private readonly ConcurrentQueue<DispatchedEvent> _dispatched = new();
    private readonly InMemoryConnectionManager? _connectionManager;

    /// <summary>
    /// Creates a standalone dispatcher.
    /// </summary>
    public InMemoryEventDispatcher()
    {
    }

    /// <summary>
    /// Creates a dispatcher that can resolve tenant broadcasts.
    /// </summary>
    public InMemoryEventDispatcher(InMemoryConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    /// <summary>
    /// All dispatched events.
    /// </summary>
    public IReadOnlyList<DispatchedEvent> DispatchedEvents => _dispatched.ToList();

    /// <summary>
    /// Gets dispatched events of a specific type.
    /// </summary>
    public IReadOnlyList<DispatchedEvent> GetEventsByType(string eventType) =>
        _dispatched.Where(e => e.Event.Type == eventType).ToList();

    /// <summary>
    /// Gets dispatched events for a specific connection.
    /// </summary>
    public IReadOnlyList<DispatchedEvent> GetEventsForConnection(string connectionId) =>
        _dispatched.Where(e => e.ConnectionIds.Contains(connectionId)).ToList();

    /// <summary>
    /// Clears all dispatched events.
    /// </summary>
    public void Clear()
    {
        while (_dispatched.TryDequeue(out _)) { }
    }

    /// <inheritdoc />
    public Task DispatchToConnectionsAsync(
        IEnumerable<string> connectionIds,
        RealtimeEvent evt,
        CancellationToken ct = default)
    {
        var ids = connectionIds.ToList();
        if (ids.Count > 0)
        {
            _dispatched.Enqueue(new DispatchedEvent(ids, evt, DateTimeOffset.UtcNow));
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DispatchToGroupAsync(
        string groupName,
        RealtimeEvent evt,
        CancellationToken ct = default)
    {
        // In-memory doesn't have real groups, just record as a special connection
        _dispatched.Enqueue(new DispatchedEvent([$"group:{groupName}"], evt, DateTimeOffset.UtcNow));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task DispatchToAllAsync(
        string tenantId,
        RealtimeEvent evt,
        CancellationToken ct = default)
    {
        if (_connectionManager != null)
        {
            var connections = await _connectionManager.GetTenantConnectionsAsync(tenantId, ct);
            var ids = connections.Select(c => c.ConnectionId).ToList();
            if (ids.Count > 0)
            {
                _dispatched.Enqueue(new DispatchedEvent(ids, evt, DateTimeOffset.UtcNow));
            }
        }
        else
        {
            // Record as broadcast marker
            _dispatched.Enqueue(new DispatchedEvent([$"broadcast:{tenantId}"], evt, DateTimeOffset.UtcNow));
        }
    }
}
