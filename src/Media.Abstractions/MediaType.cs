namespace Media.Abstractions;

/// <summary>
/// Type of media content.
/// </summary>
public enum MediaType
{
    /// <summary>
    /// Image file (JPEG, PNG, GIF, WebP).
    /// </summary>
    Image = 1,

    /// <summary>
    /// Video file (MP4, WebM).
    /// </summary>
    Video = 2,

    /// <summary>
    /// Document file (PDF).
    /// </summary>
    Document = 3
}
