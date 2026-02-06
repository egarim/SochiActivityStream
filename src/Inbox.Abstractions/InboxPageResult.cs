namespace Inbox.Abstractions;

/// <summary>
/// Paginated result of inbox items.
/// </summary>
public sealed class InboxPageResult
{
    /// <summary>
    /// Items in this page.
    /// </summary>
    public List<InboxItemDto> Items { get; set; } = new();

    /// <summary>
    /// Cursor for the next page. Null if no more items.
    /// </summary>
    public string? NextCursor { get; set; }
}
