namespace Realtime.Abstractions;

/// <summary>
/// Internal interface for dispatching events to connections.
/// Transport implementations provide this.
/// </summary>
public interface IEventDispatcher
{
    /// <summary>
    /// Sends an event to specific connections.
    /// </summary>
    Task DispatchToConnectionsAsync(
        IEnumerable<string> connectionIds,
        RealtimeEvent evt,
        CancellationToken ct = default);

    /// <summary>
    /// Sends an event to a transport group (SignalR group or equivalent).
    /// </summary>
    Task DispatchToGroupAsync(
        string groupName,
        RealtimeEvent evt,
        CancellationToken ct = default);

    /// <summary>
    /// Broadcast to all connections in tenant.
    /// </summary>
    Task DispatchToAllAsync(
        string tenantId,
        RealtimeEvent evt,
        CancellationToken ct = default);
}
