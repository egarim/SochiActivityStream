namespace Content.Abstractions;

/// <summary>
/// Paginated result for content queries.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class ContentPageResult<T>
{
    /// <summary>
    /// The items in this page.
    /// </summary>
    public required IReadOnlyList<T> Items { get; set; }

    /// <summary>
    /// Cursor for the next page (null if no more pages).
    /// </summary>
    public string? NextCursor { get; set; }

    /// <summary>
    /// Whether there are more items.
    /// </summary>
    public bool HasMore => NextCursor != null;

    /// <summary>
    /// Total count of matching items (-1 if unknown).
    /// </summary>
    public int TotalCount { get; set; } = -1;
}
