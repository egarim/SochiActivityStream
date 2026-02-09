using ActivityStream.Abstractions;

namespace Media.Abstractions;

/// <summary>
/// Service for managing media uploads and metadata.
/// </summary>
public interface IMediaService
{
    /// <summary>
    /// Request a signed URL for uploading a file.
    /// Creates a Pending media record.
    /// </summary>
    Task<UploadUrlResult> RequestUploadUrlAsync(
        RequestUploadRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Confirm that an upload completed successfully.
    /// Verifies blob exists and transitions to Ready status.
    /// </summary>
    Task<MediaDto> ConfirmUploadAsync(
        string tenantId,
        string mediaId,
        ConfirmUploadRequest? request = null,
        CancellationToken ct = default);

    /// <summary>
    /// Get media metadata with signed download URL.
    /// </summary>
    Task<MediaDto?> GetMediaAsync(
        string tenantId,
        string mediaId,
        CancellationToken ct = default);

    /// <summary>
    /// Get multiple media items by IDs.
    /// </summary>
    Task<List<MediaDto>> GetMediaBatchAsync(
        string tenantId,
        IEnumerable<string> mediaIds,
        CancellationToken ct = default);

    /// <summary>
    /// Update media metadata (alt text, etc.).
    /// </summary>
    Task<MediaDto> UpdateMediaAsync(
        UpdateMediaRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Soft delete a media item. Blob cleanup happens later.
    /// </summary>
    Task DeleteMediaAsync(
        string tenantId,
        string mediaId,
        EntityRefDto actor,
        CancellationToken ct = default);

    /// <summary>
    /// List media owned by an entity.
    /// </summary>
    Task<MediaPageResult> ListMediaAsync(
        MediaQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Clean up expired pending uploads and deleted items.
    /// Call periodically from a background job.
    /// </summary>
    Task<CleanupResult> CleanupAsync(
        string tenantId,
        CleanupOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Upload media directly from bytes (simplified server-side upload).
    /// Creates media record and uploads data in one operation.
    /// </summary>
    Task<MediaDto> UploadFromBytesAsync(
        string tenantId,
        EntityRefDto owner,
        string fileName,
        string contentType,
        byte[] data,
        CancellationToken ct = default);
}
