namespace Identity.Abstractions;

/// <summary>
/// Defines the role of a user within a profile.
/// </summary>
public enum ProfileRole
{
    /// <summary>
    /// Full control over the profile, including deletion and ownership transfer.
    /// </summary>
    Owner = 0,

    /// <summary>
    /// Can manage members and profile settings.
    /// </summary>
    Admin = 1,

    /// <summary>
    /// Can act as the profile (post, interact).
    /// </summary>
    Member = 2,

    /// <summary>
    /// Read-only access to private profile content.
    /// </summary>
    Viewer = 3
}
