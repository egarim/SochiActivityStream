namespace Chat.Abstractions;

/// <summary>
/// Reference to attached media.
/// </summary>
public sealed class MediaRefDto
{
    /// <summary>Media ID (from Media Service).</summary>
    public required string Id { get; set; }

    /// <summary>Media type.</summary>
    public required MediaType Type { get; set; }

    /// <summary>URL to access the media.</summary>
    public required string Url { get; set; }

    /// <summary>Thumbnail URL (for images/videos).</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>Original filename.</summary>
    public string? FileName { get; set; }

    /// <summary>File size in bytes.</summary>
    public long? SizeBytes { get; set; }

    /// <summary>MIME type.</summary>
    public string? ContentType { get; set; }
}
