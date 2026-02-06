namespace Search.Abstractions;

/// <summary>
/// Search result container.
/// </summary>
public sealed class SearchResult
{
    /// <summary>
    /// Matching documents.
    /// </summary>
    public List<SearchHit> Hits { get; set; } = new();

    /// <summary>
    /// Total number of matching documents.
    /// </summary>
    public long TotalCount { get; set; }

    /// <summary>
    /// Cursor for next page (null if no more results).
    /// </summary>
    public string? NextCursor { get; set; }

    /// <summary>
    /// Whether more results are available.
    /// </summary>
    public bool HasMore { get; set; }

    /// <summary>
    /// Facet results (if requested).
    /// </summary>
    public Dictionary<string, List<FacetValue>> Facets { get; set; } = new();

    /// <summary>
    /// Query execution time in milliseconds.
    /// </summary>
    public long ElapsedMs { get; set; }
}

/// <summary>
/// A single search hit.
/// </summary>
public sealed class SearchHit
{
    /// <summary>
    /// Document ID.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Document type.
    /// </summary>
    public required string DocumentType { get; set; }

    /// <summary>
    /// Relevance score.
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Highlighted text snippets (field → highlighted text).
    /// </summary>
    public Dictionary<string, string>? Highlights { get; set; }

    /// <summary>
    /// The source entity (if IncludeSource was true).
    /// </summary>
    public object? Source { get; set; }
}

/// <summary>
/// A facet value with count.
/// </summary>
public sealed class FacetValue
{
    /// <summary>
    /// The value.
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// Number of documents with this value.
    /// </summary>
    public long Count { get; set; }
}
