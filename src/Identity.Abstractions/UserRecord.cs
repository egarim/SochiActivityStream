namespace Identity.Abstractions;

/// <summary>
/// Internal record for storing user data including password hash.
/// Used by stores; password hash is never exposed in DTOs.
/// </summary>
public sealed class UserRecord
{
    /// <summary>
    /// The user data.
    /// </summary>
    public required UserDto User { get; set; }

    /// <summary>
    /// The user's hashed password.
    /// </summary>
    public required PasswordHash Hash { get; set; }
}
