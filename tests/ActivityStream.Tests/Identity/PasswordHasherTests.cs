using Identity.Core;

namespace ActivityStream.Tests.Identity;

public class PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher;

    public PasswordHasherTests()
    {
        _hasher = new Pbkdf2PasswordHasher(1000); // Lower iterations for faster tests
    }

    [Fact]
    public void Hash_and_verify_correct_password_succeeds()
    {
        var password = "MySecurePassword123!";

        var hash = _hasher.Hash(password);
        var result = _hasher.Verify(password, hash);

        Assert.True(result);
    }

    [Fact]
    public void Verify_wrong_password_fails()
    {
        var password = "MySecurePassword123!";
        var wrongPassword = "WrongPassword456!";

        var hash = _hasher.Hash(password);
        var result = _hasher.Verify(wrongPassword, hash);

        Assert.False(result);
    }

    [Fact]
    public void Two_hashes_of_same_password_differ()
    {
        var password = "MySecurePassword123!";

        var hash1 = _hasher.Hash(password);
        var hash2 = _hasher.Hash(password);

        // Salt should be different
        Assert.NotEqual(hash1.Salt, hash2.Salt);
        // Hash bytes should be different
        Assert.NotEqual(hash1.HashBytes, hash2.HashBytes);
    }

    [Fact]
    public void Hash_contains_algorithm_metadata()
    {
        var password = "MySecurePassword123!";

        var hash = _hasher.Hash(password);

        Assert.Equal("PBKDF2-SHA256", hash.Algorithm);
        Assert.Equal(1000, hash.Iterations);
        Assert.Equal(16, hash.Salt.Length);
        Assert.Equal(32, hash.HashBytes.Length);
    }
}
