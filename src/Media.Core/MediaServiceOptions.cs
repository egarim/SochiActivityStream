using Media.Abstractions;

namespace Media.Core;

/// <summary>
/// Configuration options for the Media Service.
/// </summary>
public sealed class MediaServiceOptions
{
    /// <summary>
    /// Blob container name. Default: "media".
    /// </summary>
    public string ContainerName { get; set; } = "media";

    /// <summary>
    /// How long upload URLs are valid. Default: 15 minutes.
    /// </summary>
    public TimeSpan UploadUrlExpiry { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// How long download URLs are valid. Default: 1 hour.
    /// </summary>
    public TimeSpan DownloadUrlExpiry { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Maximum image size in bytes. Default: 10 MB.
    /// </summary>
    public long MaxImageSizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Maximum video size in bytes. Default: 100 MB.
    /// </summary>
    public long MaxVideoSizeBytes { get; set; } = 100 * 1024 * 1024;

    /// <summary>
    /// Maximum document size in bytes. Default: 50 MB.
    /// </summary>
    public long MaxDocumentSizeBytes { get; set; } = 50 * 1024 * 1024;

    /// <summary>
    /// Allowed content types per media type.
    /// </summary>
    public Dictionary<MediaType, List<string>> AllowedContentTypes { get; set; } = new()
    {
        [MediaType.Image] = new() { "image/jpeg", "image/png", "image/gif", "image/webp" },
        [MediaType.Video] = new() { "video/mp4", "video/webm" },
        [MediaType.Document] = new() { "application/pdf" }
    };

    /// <summary>
    /// Get maximum size for a media type.
    /// </summary>
    public long GetMaxSize(MediaType type) => type switch
    {
        MediaType.Image => MaxImageSizeBytes,
        MediaType.Video => MaxVideoSizeBytes,
        MediaType.Document => MaxDocumentSizeBytes,
        _ => MaxImageSizeBytes
    };
}
