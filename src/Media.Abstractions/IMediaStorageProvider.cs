namespace Media.Abstractions;

/// <summary>
/// Abstraction over blob storage operations.
/// </summary>
public interface IMediaStorageProvider
{
    /// <summary>
    /// Generate a signed URL for uploading a blob.
    /// </summary>
    Task<string> GenerateUploadUrlAsync(
        string blobPath,
        string contentType,
        long maxSizeBytes,
        TimeSpan expiry,
        CancellationToken ct = default);

    /// <summary>
    /// Generate a signed URL for downloading a blob.
    /// </summary>
    Task<string> GenerateDownloadUrlAsync(
        string blobPath,
        TimeSpan expiry,
        CancellationToken ct = default);

    /// <summary>
    /// Check if a blob exists.
    /// </summary>
    Task<bool> ExistsAsync(string blobPath, CancellationToken ct = default);

    /// <summary>
    /// Get blob properties (size, content type).
    /// </summary>
    Task<BlobProperties?> GetPropertiesAsync(string blobPath, CancellationToken ct = default);

    /// <summary>
    /// Delete a blob.
    /// </summary>
    Task DeleteAsync(string blobPath, CancellationToken ct = default);

    /// <summary>
    /// Copy a blob (for thumbnail generation workflow).
    /// </summary>
    Task CopyAsync(string sourcePath, string destPath, CancellationToken ct = default);

    /// <summary>
    /// Upload blob content directly from bytes (for server-side uploads).
    /// </summary>
    Task UploadBytesAsync(
        string blobPath,
        byte[] data,
        string contentType,
        CancellationToken ct = default);
}
