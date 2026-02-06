namespace Identity.Abstractions;

/// <summary>
/// Status of a user's membership in a profile.
/// </summary>
public enum MembershipStatus
{
    /// <summary>
    /// User is an active member of the profile.
    /// </summary>
    Active = 0,

    /// <summary>
    /// User has been invited but has not yet accepted.
    /// </summary>
    Invited = 1,

    /// <summary>
    /// Membership has been disabled (e.g., declined invite or suspended).
    /// </summary>
    Disabled = 2
}
