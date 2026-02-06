namespace Search.Abstractions;

/// <summary>
/// Exception thrown when search validation fails.
/// </summary>
public sealed class SearchValidationException : Exception
{
    /// <summary>
    /// The validation error that occurred.
    /// </summary>
    public SearchValidationError Error { get; }

    /// <summary>
    /// The field that failed validation (if applicable).
    /// </summary>
    public string? Field { get; }

    /// <summary>
    /// Creates a new SearchValidationException.
    /// </summary>
    public SearchValidationException(SearchValidationError error, string? field = null)
        : base($"Search validation failed: {error}" + (field != null ? $" ({field})" : ""))
    {
        Error = error;
        Field = field;
    }
}
