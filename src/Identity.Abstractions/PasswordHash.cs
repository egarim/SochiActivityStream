namespace Identity.Abstractions;

/// <summary>
/// Represents a securely hashed password.
/// </summary>
/// <param name="Salt">Random salt used for hashing.</param>
/// <param name="Iterations">Number of iterations used in the hash function.</param>
/// <param name="HashBytes">The resulting hash bytes.</param>
/// <param name="Algorithm">Algorithm identifier (e.g., "PBKDF2-SHA256").</param>
public sealed record PasswordHash(
    byte[] Salt,
    int Iterations,
    byte[] HashBytes,
    string Algorithm);
