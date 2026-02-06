namespace Identity.Abstractions;

/// <summary>
/// Internal record for storing profile data.
/// </summary>
public sealed class ProfileRecord
{
    /// <summary>
    /// The profile data.
    /// </summary>
    public required ProfileDto Profile { get; set; }
}
