namespace Identity.Abstractions;

/// <summary>
/// Request to update an existing profile.
/// </summary>
public sealed class UpdateProfileRequest
{
    /// <summary>
    /// The profile ID to update.
    /// </summary>
    public required string ProfileId { get; set; }

    /// <summary>
    /// New display name (null to keep current).
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// New avatar URL (null to keep current).
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Update privacy setting (null to keep current).
    /// </summary>
    public bool? IsPrivate { get; set; }
}
