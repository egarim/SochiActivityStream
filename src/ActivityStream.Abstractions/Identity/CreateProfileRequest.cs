namespace ActivityStream.Abstractions.Identity;

public sealed class CreateProfileRequest
{
    public required string Handle { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
}
