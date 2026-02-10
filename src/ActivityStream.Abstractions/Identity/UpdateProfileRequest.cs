namespace ActivityStream.Abstractions.Identity;

public sealed class UpdateProfileRequest
{
    public required string Id { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public bool? IsPrivate { get; set; }
}
