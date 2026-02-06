namespace Media.Abstractions;

/// <summary>
/// Request to confirm an upload completed.
/// </summary>
public sealed class ConfirmUploadRequest
{
    /// <summary>
    /// Image/video width (client-provided).
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Image/video height (client-provided).
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Video/audio duration in seconds.
    /// </summary>
    public double? DurationSeconds { get; set; }

    /// <summary>
    /// Alt text for accessibility.
    /// </summary>
    public string? AltText { get; set; }

    /// <summary>
    /// Additional metadata.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}
