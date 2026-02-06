namespace Media.Abstractions;

/// <summary>
/// Paginated result for media queries.
/// </summary>
public sealed class MediaPageResult
{
    /// <summary>
    /// Media items in this page.
    /// </summary>
    public List<MediaDto> Items { get; set; } = new();

    /// <summary>
    /// Cursor for fetching the next page.
    /// </summary>
    public string? NextCursor { get; set; }

    /// <summary>
    /// Whether more results are available.
    /// </summary>
    public bool HasMore { get; set; }
}
