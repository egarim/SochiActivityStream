namespace ActivityStream.Abstractions.Search;

public sealed class SearchRequest
{
    public required string Query { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
