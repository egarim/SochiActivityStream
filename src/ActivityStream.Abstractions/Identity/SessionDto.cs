namespace ActivityStream.Abstractions.Identity;

public sealed class SessionDto
{
    public string? Token { get; set; }
    public string? UserId { get; set; }
}
