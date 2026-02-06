namespace Identity.Abstractions;

/// <summary>
/// Request to add a user as a member to a profile (immediate, no invite flow).
/// </summary>
public sealed class AddMemberRequest
{
    /// <summary>
    /// User identifier to add.
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// Role to assign.
    /// </summary>
    public ProfileRole Role { get; set; } = ProfileRole.Member;
}
