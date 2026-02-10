namespace ActivityStream.Abstractions.Search;

public interface ISearchService
{
    Task<SearchResult<SearchDocument>> SearchAsync(SearchRequest request, CancellationToken ct = default);
}
