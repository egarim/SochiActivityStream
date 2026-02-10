namespace ActivityStream.Abstractions;

public sealed class ContentPageResult<T>
{
    public List<T> Items { get; set; } = new();
    public string? NextCursor { get; set; }
    public int TotalCount { get; set; }

    /// <summary>
    /// Backwards-compatible convenience property used by older tests.
    /// </summary>
    public bool HasMore => NextCursor != null;
}
