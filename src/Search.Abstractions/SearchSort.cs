namespace Search.Abstractions;

/// <summary>
/// Sorting specification for search results.
/// </summary>
public sealed class SearchSort
{
    /// <summary>
    /// Field name to sort by. Use "_score" for relevance.
    /// </summary>
    public required string Field { get; set; }

    /// <summary>
    /// Sort direction.
    /// </summary>
    public SortDirection Direction { get; set; } = SortDirection.Descending;
}

/// <summary>
/// Sort direction for search results.
/// </summary>
public enum SortDirection
{
    /// <summary>
    /// Sort in ascending order.
    /// </summary>
    Ascending = 0,

    /// <summary>
    /// Sort in descending order.
    /// </summary>
    Descending = 1
}
