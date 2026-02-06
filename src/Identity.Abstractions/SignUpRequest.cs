namespace Identity.Abstractions;

/// <summary>
/// Request to sign up a new user.
/// </summary>
public sealed class SignUpRequest
{
    /// <summary>
    /// Email address. Must be globally unique.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Username. Must be globally unique.
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// Password. Minimum 8 characters.
    /// </summary>
    public required string Password { get; set; }

    /// <summary>
    /// Optional display name for the user and default profile.
    /// </summary>
    public string? DisplayName { get; set; }
}
