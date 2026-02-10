namespace ActivityStream.Abstractions.Identity;

public sealed class UserRecord
{
    public string? Id { get; set; }
    public string? Login { get; set; }
    public PasswordHash? Password { get; set; }
}
