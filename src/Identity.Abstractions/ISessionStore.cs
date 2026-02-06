namespace Identity.Abstractions;

/// <summary>
/// Store for session records.
/// </summary>
public interface ISessionStore
{
    /// <summary>
    /// Creates a new session record.
    /// </summary>
    /// <param name="record">The session record to create.</param>
    /// <param name="ct">Cancellation token.</param>
    Task CreateAsync(SessionRecord record, CancellationToken ct = default);

    /// <summary>
    /// Finds a session by access token.
    /// </summary>
    /// <param name="accessToken">The access token.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The session record if found, null otherwise.</returns>
    Task<SessionRecord?> FindByAccessTokenAsync(string accessToken, CancellationToken ct = default);

    /// <summary>
    /// Revokes (deletes) a session by ID.
    /// </summary>
    /// <param name="sessionId">The session ID to revoke.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RevokeAsync(string sessionId, CancellationToken ct = default);
}
