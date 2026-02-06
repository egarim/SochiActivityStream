namespace Identity.Abstractions;

/// <summary>
/// Store for profile records.
/// </summary>
public interface IProfileStore
{
    /// <summary>
    /// Gets a profile by ID.
    /// </summary>
    /// <param name="profileId">The profile ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The profile record if found, null otherwise.</returns>
    Task<ProfileRecord?> GetByIdAsync(string profileId, CancellationToken ct = default);

    /// <summary>
    /// Finds a profile by normalized handle.
    /// </summary>
    /// <param name="handleNormalized">The normalized (lowercase, trimmed) handle.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The profile record if found, null otherwise.</returns>
    Task<ProfileRecord?> FindByHandleAsync(string handleNormalized, CancellationToken ct = default);

    /// <summary>
    /// Creates a new profile record.
    /// </summary>
    /// <param name="record">The profile record to create.</param>
    /// <param name="ct">Cancellation token.</param>
    Task CreateAsync(ProfileRecord record, CancellationToken ct = default);
}
