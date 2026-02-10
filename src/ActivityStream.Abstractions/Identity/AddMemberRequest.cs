namespace ActivityStream.Abstractions.Identity;

public sealed class AddMemberRequest
{
    public required string UserId { get; set; }
    public ProfileRole Role { get; set; } = ProfileRole.Member;
}
