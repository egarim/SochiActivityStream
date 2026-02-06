namespace ActivityStream.Abstractions;

/// <summary>
/// Paged result of activities.
/// </summary>
public class ActivityPageResult
{
    /// <summary>
    /// Activities in this page.
    /// </summary>
    public List<ActivityDto> Items { get; set; } = new();

    /// <summary>
    /// Cursor to fetch the next page. Null if no more items.
    /// </summary>
    public string? NextCursor { get; set; }
}
