namespace Search.Abstractions;

/// <summary>
/// Search service for querying indexed documents.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Execute a search query.
    /// </summary>
    Task<SearchResult> SearchAsync(
        SearchRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Get autocomplete suggestions.
    /// </summary>
    Task<AutocompleteResult> AutocompleteAsync(
        AutocompleteRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Get a single document by ID.
    /// </summary>
    Task<SearchDocument?> GetDocumentAsync(
        string tenantId,
        string documentType,
        string id,
        CancellationToken ct = default);

    /// <summary>
    /// Check if a document exists.
    /// </summary>
    Task<bool> ExistsAsync(
        string tenantId,
        string documentType,
        string id,
        CancellationToken ct = default);
}
