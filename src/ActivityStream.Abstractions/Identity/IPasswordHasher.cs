namespace ActivityStream.Abstractions.Identity;

public interface IPasswordHasher
{
    PasswordHash Hash(string password);
    bool Verify(string password, PasswordHash hash);
}
