namespace Identity.Abstractions;

/// <summary>
/// Request to invite a user to a profile (creates an Invited membership).
/// </summary>
public sealed class InviteMemberRequest
{
    /// <summary>
    /// Username or email of the user to invite.
    /// </summary>
    public required string Login { get; set; }

    /// <summary>
    /// Role to assign when accepted.
    /// </summary>
    public ProfileRole Role { get; set; } = ProfileRole.Member;
}
