namespace Identity.Abstractions;

/// <summary>
/// Internal record for storing session data.
/// </summary>
public sealed class SessionRecord
{
    /// <summary>
    /// The session data.
    /// </summary>
    public required SessionDto Session { get; set; }
}
