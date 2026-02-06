namespace Identity.Abstractions;

/// <summary>
/// Password hashing and verification service.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plain-text password.
    /// </summary>
    /// <param name="password">The plain-text password.</param>
    /// <returns>The password hash with salt and algorithm metadata.</returns>
    PasswordHash Hash(string password);

    /// <summary>
    /// Verifies a plain-text password against a stored hash.
    /// </summary>
    /// <param name="password">The plain-text password to verify.</param>
    /// <param name="hash">The stored password hash.</param>
    /// <returns>True if the password matches, false otherwise.</returns>
    bool Verify(string password, PasswordHash hash);
}
