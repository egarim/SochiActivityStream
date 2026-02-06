namespace Search.Abstractions;

/// <summary>
/// Indexer for adding/updating/removing documents.
/// </summary>
public interface ISearchIndexer
{
    /// <summary>
    /// Index a single document (add or update).
    /// </summary>
    Task IndexAsync(
        SearchDocument document,
        CancellationToken ct = default);

    /// <summary>
    /// Index multiple documents in batch.
    /// </summary>
    Task IndexBatchAsync(
        IEnumerable<SearchDocument> documents,
        CancellationToken ct = default);

    /// <summary>
    /// Remove a document from the index.
    /// </summary>
    Task RemoveAsync(
        string tenantId,
        string documentType,
        string id,
        CancellationToken ct = default);

    /// <summary>
    /// Remove all documents of a type for a tenant.
    /// </summary>
    Task RemoveByTypeAsync(
        string tenantId,
        string documentType,
        CancellationToken ct = default);

    /// <summary>
    /// Remove all documents for a tenant.
    /// </summary>
    Task RemoveByTenantAsync(
        string tenantId,
        CancellationToken ct = default);

    /// <summary>
    /// Get index statistics.
    /// </summary>
    Task<IndexStats> GetStatsAsync(
        string? tenantId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Optimize the index (implementation-specific).
    /// </summary>
    Task OptimizeAsync(
        string? tenantId = null,
        CancellationToken ct = default);
}
