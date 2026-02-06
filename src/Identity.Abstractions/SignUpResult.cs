namespace Identity.Abstractions;

/// <summary>
/// Result of a successful sign-up operation.
/// Contains the created user, default profile, and membership.
/// </summary>
public sealed class SignUpResult
{
    /// <summary>
    /// The created user.
    /// </summary>
    public required UserDto User { get; set; }

    /// <summary>
    /// The auto-created default profile (handle = username).
    /// </summary>
    public required ProfileDto Profile { get; set; }

    /// <summary>
    /// The membership linking user to profile as Owner.
    /// </summary>
    public required MembershipDto Membership { get; set; }
}
