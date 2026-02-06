namespace Media.Abstractions;

/// <summary>
/// Result of requesting an upload URL.
/// </summary>
public sealed class UploadUrlResult
{
    /// <summary>
    /// Media item ID (use this to confirm upload).
    /// </summary>
    public required string MediaId { get; set; }

    /// <summary>
    /// Signed URL for direct upload to blob storage.
    /// </summary>
    public required string UploadUrl { get; set; }

    /// <summary>
    /// When the upload URL expires.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Maximum allowed file size in bytes.
    /// </summary>
    public long MaxSizeBytes { get; set; }

    /// <summary>
    /// Allowed content types for this upload.
    /// </summary>
    public List<string> AllowedContentTypes { get; set; } = new();
}
