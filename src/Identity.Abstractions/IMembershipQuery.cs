namespace Identity.Abstractions;

/// <summary>
/// Query service for membership checks (e.g., for SignalR authorization).
/// </summary>
public interface IMembershipQuery
{
    /// <summary>
    /// Checks if a user is an active member of a profile in a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant to check.</param>
    /// <param name="userId">The user to check.</param>
    /// <param name="profileId">The profile to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the user is an active member.</returns>
    Task<bool> IsActiveMemberAsync(string tenantId, string userId, string profileId, CancellationToken ct = default);

    /// <summary>
    /// Gets all profile IDs a user is actively a member of in a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant to query.</param>
    /// <param name="userId">The user to query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of profile IDs.</returns>
    Task<IReadOnlyList<string>> GetActiveProfileIdsForUserAsync(string tenantId, string userId, CancellationToken ct = default);
}
