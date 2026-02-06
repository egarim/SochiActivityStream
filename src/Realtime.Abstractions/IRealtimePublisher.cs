namespace Realtime.Abstractions;

/// <summary>
/// Main interface for publishing real-time events.
/// Services (Content, Chat, Inbox) inject this to push events.
/// </summary>
public interface IRealtimePublisher
{
    /// <summary>
    /// Publishes an event to the specified target.
    /// </summary>
    Task PublishAsync(RealtimeEvent evt, CancellationToken ct = default);

    /// <summary>
    /// Publishes multiple events atomically.
    /// </summary>
    Task PublishBatchAsync(IEnumerable<RealtimeEvent> events, CancellationToken ct = default);

    /// <summary>
    /// Convenience: Send to a single profile.
    /// </summary>
    Task SendToProfileAsync(
        string tenantId,
        EntityRefDto profile,
        string eventType,
        object payload,
        CancellationToken ct = default);

    /// <summary>
    /// Convenience: Send to multiple profiles.
    /// </summary>
    Task SendToProfilesAsync(
        string tenantId,
        IEnumerable<EntityRefDto> profiles,
        string eventType,
        object payload,
        CancellationToken ct = default);

    /// <summary>
    /// Convenience: Send to all conversation participants.
    /// </summary>
    Task SendToConversationAsync(
        string tenantId,
        string conversationId,
        string eventType,
        object payload,
        CancellationToken ct = default);

    /// <summary>
    /// Convenience: Send to all group members.
    /// </summary>
    Task SendToGroupAsync(
        string tenantId,
        string groupId,
        string eventType,
        object payload,
        CancellationToken ct = default);

    /// <summary>
    /// Convenience: Broadcast to all users in tenant.
    /// </summary>
    Task BroadcastAsync(
        string tenantId,
        string eventType,
        object payload,
        CancellationToken ct = default);
}
