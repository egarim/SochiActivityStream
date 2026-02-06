namespace Realtime.Abstractions;

/// <summary>
/// Tracks user online/offline presence.
/// </summary>
public interface IPresenceTracker
{
    /// <summary>
    /// Gets the presence status for a profile.
    /// </summary>
    Task<PresenceStatus> GetPresenceAsync(
        string tenantId,
        EntityRefDto profile,
        CancellationToken ct = default);

    /// <summary>
    /// Gets presence for multiple profiles.
    /// </summary>
    Task<IReadOnlyDictionary<string, PresenceStatus>> GetPresenceBatchAsync(
        string tenantId,
        IEnumerable<EntityRefDto> profiles,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all online profiles in a tenant.
    /// </summary>
    Task<IReadOnlyList<EntityRefDto>> GetOnlineProfilesAsync(
        string tenantId,
        CancellationToken ct = default);

    /// <summary>
    /// Event fired when presence changes.
    /// </summary>
    event EventHandler<PresenceChangedEventArgs>? PresenceChanged;
}
