using ActivityStream.Abstractions;

namespace Media.Abstractions;

/// <summary>
/// Request to generate an upload URL.
/// </summary>
public sealed class RequestUploadRequest
{
    /// <summary>
    /// Tenant ID for multi-tenancy.
    /// </summary>
    public required string TenantId { get; set; }

    /// <summary>
    /// Entity uploading the file (typically a Profile).
    /// </summary>
    public required EntityRefDto Owner { get; set; }

    /// <summary>
    /// Original filename (used for path and download name).
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// MIME content type (e.g., "image/jpeg").
    /// </summary>
    public required string ContentType { get; set; }

    /// <summary>
    /// Expected file size in bytes (for validation).
    /// </summary>
    public long? SizeBytes { get; set; }

    /// <summary>
    /// Optional purpose hint (e.g., "profile-photo", "post-image").
    /// </summary>
    public string? Purpose { get; set; }
}
