namespace Search.Abstractions;

/// <summary>
/// A document to be indexed for search.
/// </summary>
public sealed class SearchDocument
{
    /// <summary>
    /// Unique identifier within the document type.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Multi-tenancy partition key.
    /// </summary>
    public required string TenantId { get; set; }

    /// <summary>
    /// Document type (e.g., "Profile", "Post", "Group").
    /// </summary>
    public required string DocumentType { get; set; }

    /// <summary>
    /// Text fields for full-text search.
    /// Key = field name, Value = text content.
    /// </summary>
    public Dictionary<string, string> TextFields { get; set; } = new();

    /// <summary>
    /// Keyword fields for exact matching/filtering.
    /// Key = field name, Value = keyword value(s).
    /// </summary>
    public Dictionary<string, List<string>> KeywordFields { get; set; } = new();

    /// <summary>
    /// Numeric fields for range queries and sorting.
    /// </summary>
    public Dictionary<string, double> NumericFields { get; set; } = new();

    /// <summary>
    /// Date fields for range queries and sorting.
    /// </summary>
    public Dictionary<string, DateTimeOffset> DateFields { get; set; } = new();

    /// <summary>
    /// When the document was last indexed.
    /// </summary>
    public DateTimeOffset IndexedAt { get; set; }

    /// <summary>
    /// Optional boost factor for ranking (default 1.0).
    /// </summary>
    public double Boost { get; set; } = 1.0;

    /// <summary>
    /// Store the original entity for retrieval (optional).
    /// </summary>
    public object? SourceEntity { get; set; }
}
