namespace Identity.Abstractions;

/// <summary>
/// Represents an active session for a user in a tenant.
/// </summary>
public sealed class SessionDto
{
    /// <summary>
    /// Unique session identifier.
    /// </summary>
    public required string SessionId { get; set; }

    /// <summary>
    /// User identifier.
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// Tenant identifier this session is scoped to.
    /// </summary>
    public required string TenantId { get; set; }

    /// <summary>
    /// Opaque access token for API/SignalR authentication.
    /// </summary>
    public required string AccessToken { get; set; }

    /// <summary>
    /// When this session expires.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Profile IDs the user can act as in this tenant (cached for convenience).
    /// </summary>
    public List<string> ProfileIds { get; set; } = new();
}
