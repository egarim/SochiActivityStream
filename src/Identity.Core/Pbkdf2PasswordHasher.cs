using System.Security.Cryptography;
using Identity.Abstractions;

namespace Identity.Core;

/// <summary>
/// Password hasher using PBKDF2 with HMAC-SHA256.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string Algorithm = "PBKDF2-SHA256";
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int DefaultIterations = 100_000;

    private readonly int _iterations;

    /// <summary>
    /// Creates a new password hasher with default iterations (100,000).
    /// </summary>
    public Pbkdf2PasswordHasher() : this(DefaultIterations)
    {
    }

    /// <summary>
    /// Creates a new password hasher with the specified iterations.
    /// </summary>
    /// <param name="iterations">Number of PBKDF2 iterations.</param>
    public Pbkdf2PasswordHasher(int iterations)
    {
        if (iterations < 1)
            throw new ArgumentOutOfRangeException(nameof(iterations), "Iterations must be at least 1.");
        _iterations = iterations;
    }

    /// <inheritdoc />
    public PasswordHash Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            _iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return new PasswordHash(salt, _iterations, hashBytes, Algorithm);
    }

    /// <inheritdoc />
    public bool Verify(string password, PasswordHash hash)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(hash);

        if (hash.Algorithm != Algorithm)
            return false;

        var computedHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            hash.Salt,
            hash.Iterations,
            HashAlgorithmName.SHA256,
            hash.HashBytes.Length);

        return CryptographicOperations.FixedTimeEquals(computedHash, hash.HashBytes);
    }
}
