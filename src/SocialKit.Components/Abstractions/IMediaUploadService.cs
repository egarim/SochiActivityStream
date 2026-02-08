namespace SocialKit.Components.Abstractions;

/// <summary>
/// Service for uploading media files.
/// </summary>
public interface IMediaUploadService
{
    /// <summary>
    /// Upload a file and return the media ID.
    /// </summary>
    Task<UploadedMedia> UploadAsync(
        string fileName,
        string contentType,
        byte[] data,
        CancellationToken ct = default);
}

/// <summary>
/// Result of a media upload.
/// </summary>
public class UploadedMedia
{
    public string MediaId { get; set; } = "";
    public string Url { get; set; } = "";
    public string? ThumbnailUrl { get; set; }
}
