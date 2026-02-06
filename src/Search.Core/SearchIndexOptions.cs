namespace Search.Core;

/// <summary>
/// Configuration options for the search index.
/// </summary>
public sealed class SearchIndexOptions
{
    /// <summary>
    /// Maximum documents to return in a single query.
    /// </summary>
    public int MaxResultsPerQuery { get; set; } = 100;

    /// <summary>
    /// Maximum autocomplete suggestions.
    /// </summary>
    public int MaxAutocompleteSuggestions { get; set; } = 20;

    /// <summary>
    /// Minimum query length for search.
    /// </summary>
    public int MinQueryLength { get; set; } = 1;

    /// <summary>
    /// Default boost for recency (newer documents score higher).
    /// </summary>
    public double RecencyBoostWeight { get; set; } = 0.1;

    /// <summary>
    /// How many days until recency boost decays to zero.
    /// </summary>
    public int RecencyBoostDays { get; set; } = 30;

    /// <summary>
    /// Minimum token length for indexing.
    /// </summary>
    public int MinTokenLength { get; set; } = 2;

    /// <summary>
    /// Maximum token length for indexing.
    /// </summary>
    public int MaxTokenLength { get; set; } = 100;

    /// <summary>
    /// HTML tag for highlighting match start.
    /// </summary>
    public string HighlightPreTag { get; set; } = "<mark>";

    /// <summary>
    /// HTML tag for highlighting match end.
    /// </summary>
    public string HighlightPostTag { get; set; } = "</mark>";
}
