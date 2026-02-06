namespace Identity.Abstractions;

/// <summary>
/// Authentication service for user sign-up, sign-in, and session management.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Signs up a new user, creating a user account and default profile.
    /// </summary>
    /// <param name="tenantId">The tenant to create the default profile membership in.</param>
    /// <param name="request">Sign-up details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created user, profile, and membership.</returns>
    /// <exception cref="IdentityValidationException">When validation fails or email/username is taken.</exception>
    Task<SignUpResult> SignUpAsync(string tenantId, SignUpRequest request, CancellationToken ct = default);

    /// <summary>
    /// Signs in a user and creates a session.
    /// </summary>
    /// <param name="tenantId">The tenant to scope the session to.</param>
    /// <param name="request">Sign-in credentials.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created session with access token.</returns>
    /// <exception cref="IdentityValidationException">When credentials are invalid.</exception>
    Task<SessionDto> SignInAsync(string tenantId, SignInRequest request, CancellationToken ct = default);

    /// <summary>
    /// Signs out by revoking a session.
    /// </summary>
    /// <param name="sessionId">The session to revoke.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SignOutAsync(string sessionId, CancellationToken ct = default);

    /// <summary>
    /// Validates an access token and returns the session if valid.
    /// </summary>
    /// <param name="accessToken">The access token to validate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The session if valid and not expired, null otherwise.</returns>
    Task<SessionDto?> ValidateAccessTokenAsync(string accessToken, CancellationToken ct = default);
}
