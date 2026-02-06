using ActivityStream.Abstractions;

namespace Media.Abstractions;

/// <summary>
/// Represents a media item with metadata and access URLs.
/// </summary>
public sealed class MediaDto
{
    /// <summary>
    /// Unique identifier (ULID recommended).
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Multi-tenancy partition key.
    /// </summary>
    public required string TenantId { get; set; }

    /// <summary>
    /// Entity that owns this media (typically a Profile).
    /// </summary>
    public required EntityRefDto Owner { get; set; }

    /// <summary>
    /// Type of media (Image, Video, Document).
    /// </summary>
    public MediaType Type { get; set; }

    /// <summary>
    /// Original filename from upload.
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// MIME content type (e.g., "image/jpeg").
    /// </summary>
    public required string ContentType { get; set; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Blob path in storage (internal use).
    /// </summary>
    public string? BlobPath { get; set; }

    /// <summary>
    /// Signed download URL (populated on read, not stored).
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Signed thumbnail URL (if available).
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Thumbnail blob path (if generated).
    /// </summary>
    public string? ThumbnailBlobPath { get; set; }

    /// <summary>
    /// Image/video width in pixels.
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Image/video height in pixels.
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Video/audio duration in seconds.
    /// </summary>
    public double? DurationSeconds { get; set; }

    /// <summary>
    /// Current status of the media item.
    /// </summary>
    public MediaStatus Status { get; set; }

    /// <summary>
    /// When the upload URL was generated.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the upload was confirmed.
    /// </summary>
    public DateTimeOffset? ConfirmedAt { get; set; }

    /// <summary>
    /// When the item was soft deleted.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// Upload URL expiry (for pending items).
    /// </summary>
    public DateTimeOffset? UploadExpiresAt { get; set; }

    /// <summary>
    /// Optional alt text for accessibility.
    /// </summary>
    public string? AltText { get; set; }

    /// <summary>
    /// Optional metadata (e.g., EXIF data).
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}
