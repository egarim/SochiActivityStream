namespace ActivityStream.Abstractions.Identity;

public sealed class MembershipRecord
{
    public string? Id { get; set; }
    public string? ProfileId { get; set; }
    public string? UserId { get; set; }
    public MembershipStatus Status { get; set; }
    public ProfileRole Role { get; set; }
}
