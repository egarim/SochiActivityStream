namespace ActivityStream.Abstractions.Identity;

public sealed class SignUpRequest
{
    public required string Login { get; set; }
    public required string Password { get; set; }
}
