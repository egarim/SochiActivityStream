namespace Search.Abstractions;

/// <summary>
/// Combined interface for implementations that provide both search and indexing.
/// </summary>
public interface ISearchIndex : ISearchService, ISearchIndexer
{
}
