namespace ActivityStream.Abstractions.Identity;

public sealed class ProfileDto
{
    public string? Id { get; set; }
    public required string Handle { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsPrivate { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; } = default;
}
