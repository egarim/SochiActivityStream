namespace Identity.Abstractions;

/// <summary>
/// Store for user records.
/// </summary>
public interface IUserStore
{
    /// <summary>
    /// Finds a user by normalized username.
    /// </summary>
    /// <param name="usernameNormalized">The normalized (lowercase, trimmed) username.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The user record if found, null otherwise.</returns>
    Task<UserRecord?> FindByUsernameAsync(string usernameNormalized, CancellationToken ct = default);

    /// <summary>
    /// Finds a user by normalized email.
    /// </summary>
    /// <param name="emailNormalized">The normalized (lowercase, trimmed) email.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The user record if found, null otherwise.</returns>
    Task<UserRecord?> FindByEmailAsync(string emailNormalized, CancellationToken ct = default);

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The user record if found, null otherwise.</returns>
    Task<UserRecord?> GetByIdAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new user record.
    /// </summary>
    /// <param name="record">The user record to create.</param>
    /// <param name="ct">Cancellation token.</param>
    Task CreateAsync(UserRecord record, CancellationToken ct = default);
}
