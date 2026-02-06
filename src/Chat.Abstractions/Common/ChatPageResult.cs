namespace Chat.Abstractions;

/// <summary>
/// Paginated result for chat queries.
/// </summary>
public sealed class ChatPageResult<T>
{
    public List<T> Items { get; set; } = [];
    public string? NextCursor { get; set; }
    public bool HasMore { get; set; }
}
