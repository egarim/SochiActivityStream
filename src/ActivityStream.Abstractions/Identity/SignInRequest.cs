namespace ActivityStream.Abstractions.Identity;

public sealed class SignInRequest
{
    public required string Login { get; set; }
    public required string Password { get; set; }
}
