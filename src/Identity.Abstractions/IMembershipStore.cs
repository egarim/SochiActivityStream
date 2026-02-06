namespace Identity.Abstractions;

/// <summary>
/// Store for membership records.
/// </summary>
public interface IMembershipStore
{
    /// <summary>
    /// Finds a specific membership.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="profileId">The profile ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The membership record if found, null otherwise.</returns>
    Task<MembershipRecord?> FindAsync(string tenantId, string userId, string profileId, CancellationToken ct = default);

    /// <summary>
    /// Gets all memberships for a user in a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of membership records.</returns>
    Task<IReadOnlyList<MembershipRecord>> GetForUserAsync(string tenantId, string userId, CancellationToken ct = default);

    /// <summary>
    /// Gets all memberships for a profile in a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="profileId">The profile ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of membership records.</returns>
    Task<IReadOnlyList<MembershipRecord>> GetForProfileAsync(string tenantId, string profileId, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates a membership record.
    /// </summary>
    /// <param name="record">The membership record.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpsertAsync(MembershipRecord record, CancellationToken ct = default);

    /// <summary>
    /// Deletes a membership record.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="profileId">The profile ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(string tenantId, string userId, string profileId, CancellationToken ct = default);
}
