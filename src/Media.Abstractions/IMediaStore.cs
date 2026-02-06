namespace Media.Abstractions;

/// <summary>
/// Persistence layer for media metadata.
/// </summary>
public interface IMediaStore
{
    /// <summary>
    /// Add or update a media record.
    /// </summary>
    Task<MediaDto> UpsertAsync(MediaDto media, CancellationToken ct = default);

    /// <summary>
    /// Get a media record by ID.
    /// </summary>
    Task<MediaDto?> GetByIdAsync(string tenantId, string mediaId, CancellationToken ct = default);

    /// <summary>
    /// Get multiple media records by IDs.
    /// </summary>
    Task<List<MediaDto>> GetByIdsAsync(string tenantId, IEnumerable<string> mediaIds, CancellationToken ct = default);

    /// <summary>
    /// Delete a media record.
    /// </summary>
    Task DeleteAsync(string tenantId, string mediaId, CancellationToken ct = default);

    /// <summary>
    /// Query media records with filtering and pagination.
    /// </summary>
    Task<MediaPageResult> QueryAsync(MediaQuery query, CancellationToken ct = default);

    /// <summary>
    /// Find expired pending uploads for cleanup.
    /// </summary>
    Task<List<MediaDto>> GetExpiredPendingAsync(
        string tenantId,
        DateTimeOffset expiredBefore,
        int limit = 100,
        CancellationToken ct = default);

    /// <summary>
    /// Find soft-deleted items for blob cleanup.
    /// </summary>
    Task<List<MediaDto>> GetDeletedForCleanupAsync(
        string tenantId,
        DateTimeOffset deletedBefore,
        int limit = 100,
        CancellationToken ct = default);
}
