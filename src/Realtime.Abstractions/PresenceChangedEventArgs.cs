namespace Realtime.Abstractions;

/// <summary>
/// Event arguments for presence changes.
/// </summary>
public sealed class PresenceChangedEventArgs : EventArgs
{
    /// <summary>The tenant where the presence change occurred.</summary>
    public required string TenantId { get; set; }

    /// <summary>The profile whose presence changed.</summary>
    public required EntityRefDto Profile { get; set; }

    /// <summary>The previous presence status.</summary>
    public required PresenceStatus OldStatus { get; set; }

    /// <summary>The new presence status.</summary>
    public required PresenceStatus NewStatus { get; set; }

    /// <summary>When the change occurred.</summary>
    public DateTimeOffset Timestamp { get; set; }
}
