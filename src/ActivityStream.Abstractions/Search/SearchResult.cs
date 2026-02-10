namespace ActivityStream.Abstractions.Search;

public sealed class SearchResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int Total { get; set; }
}
