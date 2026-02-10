namespace ActivityStream.Abstractions.Identity;

public sealed class MembershipDto
{
    public string? Id { get; set; }
    public required string ProfileId { get; set; }
    public required string UserId { get; set; }
    public MembershipStatus Status { get; set; }
    public ProfileRole Role { get; set; }
}
