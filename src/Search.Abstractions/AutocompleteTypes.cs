namespace Search.Abstractions;

/// <summary>
/// Autocomplete/typeahead request.
/// </summary>
public sealed class AutocompleteRequest
{
    /// <summary>
    /// Multi-tenancy partition key.
    /// </summary>
    public required string TenantId { get; set; }

    /// <summary>
    /// Partial query text.
    /// </summary>
    public required string Prefix { get; set; }

    /// <summary>
    /// Document types to include (empty = all).
    /// </summary>
    public List<string> DocumentTypes { get; set; } = new();

    /// <summary>
    /// Fields to search in (empty = default autocomplete fields).
    /// </summary>
    public List<string> Fields { get; set; } = new();

    /// <summary>
    /// Maximum suggestions to return.
    /// </summary>
    public int Limit { get; set; } = 10;
}

/// <summary>
/// Autocomplete result.
/// </summary>
public sealed class AutocompleteResult
{
    /// <summary>
    /// Suggestions.
    /// </summary>
    public List<AutocompleteSuggestion> Suggestions { get; set; } = new();
}

/// <summary>
/// A single autocomplete suggestion.
/// </summary>
public sealed class AutocompleteSuggestion
{
    /// <summary>
    /// Suggested text.
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// Highlighted text with matching portions marked.
    /// </summary>
    public string? Highlighted { get; set; }

    /// <summary>
    /// Document ID (if suggestion is from a specific document).
    /// </summary>
    public string? DocumentId { get; set; }

    /// <summary>
    /// Document type.
    /// </summary>
    public string? DocumentType { get; set; }

    /// <summary>
    /// Score/weight of the suggestion.
    /// </summary>
    public double Score { get; set; }
}
