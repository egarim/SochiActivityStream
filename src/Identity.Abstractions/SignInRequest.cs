namespace Identity.Abstractions;

/// <summary>
/// Request to sign in an existing user.
/// </summary>
public sealed class SignInRequest
{
    /// <summary>
    /// Username or email address.
    /// </summary>
    public required string Login { get; set; }

    /// <summary>
    /// Password.
    /// </summary>
    public required string Password { get; set; }
}
