namespace Realtime.Core;

/// <summary>
/// Configuration options for the realtime service.
/// </summary>
public sealed class RealtimeServiceOptions
{
    /// <summary>
    /// Idle timeout in minutes before a connection is marked as Away.
    /// Default: 5 minutes.
    /// </summary>
    public int IdleTimeoutMinutes { get; set; } = 5;

    /// <summary>
    /// Whether to automatically publish presence events when status changes.
    /// Default: true.
    /// </summary>
    public bool AutoPublishPresenceEvents { get; set; } = true;

    /// <summary>
    /// Maximum connections per profile (0 = unlimited).
    /// Default: 0 (unlimited).
    /// </summary>
    public int MaxConnectionsPerProfile { get; set; } = 0;
}
