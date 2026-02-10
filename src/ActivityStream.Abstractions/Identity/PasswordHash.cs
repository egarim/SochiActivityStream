namespace ActivityStream.Abstractions.Identity;

public sealed class PasswordHash
{
    public string Hash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
}
