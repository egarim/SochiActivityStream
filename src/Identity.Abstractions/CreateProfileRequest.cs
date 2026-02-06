namespace Identity.Abstractions;

/// <summary>
/// Request to create a new profile.
/// </summary>
public sealed class CreateProfileRequest
{
    /// <summary>
    /// Globally unique handle for the profile.
    /// </summary>
    public required string Handle { get; set; }

    /// <summary>
    /// Optional display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// If true, following this profile requires approval.
    /// </summary>
    public bool IsPrivate { get; set; } = false;
}
