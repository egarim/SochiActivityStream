namespace Search.Abstractions;

/// <summary>
/// Search query parameters.
/// </summary>
public sealed class SearchRequest
{
    /// <summary>
    /// Multi-tenancy partition key.
    /// </summary>
    public required string TenantId { get; set; }

    /// <summary>
    /// Search query text (full-text search).
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// Document types to search (empty = all types).
    /// </summary>
    public List<string> DocumentTypes { get; set; } = new();

    /// <summary>
    /// Text fields to search in (empty = all text fields).
    /// </summary>
    public List<string> SearchFields { get; set; } = new();

    /// <summary>
    /// Filters to apply.
    /// </summary>
    public List<SearchFilter> Filters { get; set; } = new();

    /// <summary>
    /// Sorting options.
    /// </summary>
    public List<SearchSort> Sorts { get; set; } = new();

    /// <summary>
    /// Facets to compute.
    /// </summary>
    public List<string> Facets { get; set; } = new();

    /// <summary>
    /// Pagination cursor (opaque string from previous result).
    /// </summary>
    public string? Cursor { get; set; }

    /// <summary>
    /// Maximum results to return.
    /// </summary>
    public int Limit { get; set; } = 20;

    /// <summary>
    /// Whether to include the source entity in results.
    /// </summary>
    public bool IncludeSource { get; set; } = true;

    /// <summary>
    /// Whether to highlight matching text.
    /// </summary>
    public bool Highlight { get; set; } = false;
}
