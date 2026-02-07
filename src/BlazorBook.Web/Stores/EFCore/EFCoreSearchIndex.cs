using Search.Abstractions;
using Microsoft.EntityFrameworkCore;
using BlazorBook.Web.Data;
using System.Text.Json;

namespace BlazorBook.Web.Stores.EFCore;

/// <summary>
/// EF Core implementation of ISearchIndex.
/// Uses simple LIKE queries for text search. For production, use a proper search engine.
/// </summary>
public sealed class EFCoreSearchIndex : ISearchIndex
{
    private readonly ApplicationDbContext _context;

    public EFCoreSearchIndex(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    #region ISearchIndexer

    public async Task IndexAsync(SearchDocument document, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.IndexedAt = DateTimeOffset.UtcNow;

        var existing = await _context.SearchDocuments
            .FirstOrDefaultAsync(d => 
                d.TenantId == document.TenantId && 
                d.DocumentType == document.DocumentType && 
                d.Id == document.Id, ct);

        if (existing == null)
        {
            _context.SearchDocuments.Add(document);
        }
        else
        {
            existing.TextFields = document.TextFields;
            existing.KeywordFields = document.KeywordFields;
            existing.NumericFields = document.NumericFields;
            existing.DateFields = document.DateFields;
            existing.IndexedAt = document.IndexedAt;
            existing.Boost = document.Boost;
            existing.SourceEntity = document.SourceEntity;
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task IndexBatchAsync(IEnumerable<SearchDocument> documents, CancellationToken ct = default)
    {
        foreach (var doc in documents)
        {
            await IndexAsync(doc, ct);
            ct.ThrowIfCancellationRequested();
        }
    }

    public async Task RemoveAsync(string tenantId, string documentType, string id, CancellationToken ct = default)
    {
        var doc = await _context.SearchDocuments
            .FirstOrDefaultAsync(d => 
                d.TenantId == tenantId && 
                d.DocumentType == documentType && 
                d.Id == id, ct);

        if (doc != null)
        {
            _context.SearchDocuments.Remove(doc);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task RemoveByTypeAsync(string tenantId, string documentType, CancellationToken ct = default)
    {
        var docs = await _context.SearchDocuments
            .Where(d => d.TenantId == tenantId && d.DocumentType == documentType)
            .ToListAsync(ct);

        _context.SearchDocuments.RemoveRange(docs);
        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoveByTenantAsync(string tenantId, CancellationToken ct = default)
    {
        var docs = await _context.SearchDocuments
            .Where(d => d.TenantId == tenantId)
            .ToListAsync(ct);

        _context.SearchDocuments.RemoveRange(docs);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IndexStats> GetStatsAsync(string? tenantId = null, CancellationToken ct = default)
    {
        var query = _context.SearchDocuments.AsQueryable();
        
        if (tenantId != null)
        {
            query = query.Where(d => d.TenantId == tenantId);
        }

        var countByType = await query
            .GroupBy(d => d.DocumentType)
            .Select(g => new { Type = g.Key, Count = g.LongCount() })
            .ToDictionaryAsync(x => x.Type, x => x.Count, ct);

        var lastDoc = await query
            .OrderByDescending(d => d.IndexedAt)
            .FirstOrDefaultAsync(ct);

        return new IndexStats
        {
            DocumentCount = countByType.Values.Sum(),
            CountByType = countByType,
            LastUpdated = lastDoc?.IndexedAt
        };
    }

    public Task OptimizeAsync(string? tenantId = null, CancellationToken ct = default)
    {
        // No-op for EF Core - database handles optimization
        return Task.CompletedTask;
    }

    #endregion

    #region ISearchService

    public async Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken ct = default)
    {
        var query = _context.SearchDocuments
            .Where(d => d.TenantId == request.TenantId)
            .AsQueryable();

        // Filter by document types
        if (request.DocumentTypes.Count > 0)
        {
            query = query.Where(d => request.DocumentTypes.Contains(d.DocumentType));
        }

        // Load all matching documents for in-memory filtering
        // (For production, use proper full-text search)
        var allDocs = await query.ToListAsync(ct);

        // Apply text search in memory
        var results = allDocs.AsEnumerable();
        
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var searchTerms = request.Query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            results = results.Where(doc =>
            {
                var textToSearch = string.Join(" ", doc.TextFields.Values).ToLowerInvariant();
                return searchTerms.All(term => textToSearch.Contains(term));
            });
        }

        // Apply filters
        foreach (var filter in request.Filters)
        {
            results = ApplyFilter(results, filter);
        }

        // Score and sort
        var scoredResults = results
            .Select(doc => new { Doc = doc, Score = CalculateScore(doc, request.Query) })
            .OrderByDescending(x => x.Score);

        // Pagination
        var skip = 0;
        if (!string.IsNullOrEmpty(request.Cursor))
        {
            if (int.TryParse(request.Cursor, out var cursorValue))
            {
                skip = cursorValue;
            }
        }

        var pagedResults = scoredResults.Skip(skip).Take(request.Limit + 1).ToList();
        var hasMore = pagedResults.Count > request.Limit;
        var hits = pagedResults.Take(request.Limit).ToList();

        return new SearchResult
        {
            Hits = hits.Select(x => new SearchHit
            {
                Id = x.Doc.Id,
                DocumentType = x.Doc.DocumentType,
                Score = x.Score,
                Source = request.IncludeSource ? x.Doc.SourceEntity : null,
                Highlights = request.Highlight ? GetHighlights(x.Doc, request.Query) : null
            }).ToList(),
            TotalCount = scoredResults.Count(),
            HasMore = hasMore,
            NextCursor = hasMore ? (skip + request.Limit).ToString() : null
        };
    }

    public async Task<AutocompleteResult> AutocompleteAsync(AutocompleteRequest request, CancellationToken ct = default)
    {
        var query = _context.SearchDocuments
            .Where(d => d.TenantId == request.TenantId)
            .AsQueryable();

        if (request.DocumentTypes.Count > 0)
        {
            query = query.Where(d => request.DocumentTypes.Contains(d.DocumentType));
        }

        var allDocs = await query.Take(1000).ToListAsync(ct);
        var prefix = request.Prefix.ToLowerInvariant();

        var suggestions = allDocs
            .SelectMany(doc => doc.TextFields.Values
                .SelectMany(text => text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Where(word => word.ToLowerInvariant().StartsWith(prefix))
                .Select(word => new { Word = word, Doc = doc }))
            .GroupBy(x => x.Word.ToLowerInvariant())
            .OrderByDescending(g => g.Count())
            .Take(request.Limit)
            .Select(g => new AutocompleteSuggestion
            {
                Text = g.First().Word,
                Highlighted = HighlightPrefix(g.First().Word, prefix),
                DocumentId = g.First().Doc.Id,
                DocumentType = g.First().Doc.DocumentType,
                Score = g.Count()
            })
            .ToList();

        return new AutocompleteResult { Suggestions = suggestions };
    }

    public async Task<SearchDocument?> GetDocumentAsync(
        string tenantId,
        string documentType,
        string id,
        CancellationToken ct = default)
    {
        return await _context.SearchDocuments
            .FirstOrDefaultAsync(d =>
                d.TenantId == tenantId &&
                d.DocumentType == documentType &&
                d.Id == id, ct);
    }

    public async Task<bool> ExistsAsync(
        string tenantId,
        string documentType,
        string id,
        CancellationToken ct = default)
    {
        return await _context.SearchDocuments
            .AnyAsync(d =>
                d.TenantId == tenantId &&
                d.DocumentType == documentType &&
                d.Id == id, ct);
    }

    #endregion

    #region Private Helpers

    private static IEnumerable<SearchDocument> ApplyFilter(IEnumerable<SearchDocument> docs, SearchFilter filter)
    {
        return docs.Where(doc =>
        {
            // Check keyword fields
            if (doc.KeywordFields.TryGetValue(filter.Field, out var keywords))
            {
                var valueStr = filter.Value?.ToString() ?? "";
                return filter.Operator switch
                {
                    SearchFilterOperator.Equals => keywords.Contains(valueStr),
                    SearchFilterOperator.NotEquals => !keywords.Contains(valueStr),
                    SearchFilterOperator.In when filter.Value is IEnumerable<string> values => 
                        values.Any(v => keywords.Contains(v)),
                    _ => true
                };
            }

            // Check numeric fields
            if (doc.NumericFields.TryGetValue(filter.Field, out var numValue) && 
                filter.Value is double filterNum)
            {
                return filter.Operator switch
                {
                    SearchFilterOperator.Equals => Math.Abs(numValue - filterNum) < 0.0001,
                    SearchFilterOperator.GreaterThan => numValue > filterNum,
                    SearchFilterOperator.GreaterThanOrEqual => numValue >= filterNum,
                    SearchFilterOperator.LessThan => numValue < filterNum,
                    SearchFilterOperator.LessThanOrEqual => numValue <= filterNum,
                    _ => true
                };
            }

            // Check date fields
            if (doc.DateFields.TryGetValue(filter.Field, out var dateValue) && 
                filter.Value is DateTimeOffset filterDate)
            {
                return filter.Operator switch
                {
                    SearchFilterOperator.Equals => dateValue == filterDate,
                    SearchFilterOperator.GreaterThan => dateValue > filterDate,
                    SearchFilterOperator.GreaterThanOrEqual => dateValue >= filterDate,
                    SearchFilterOperator.LessThan => dateValue < filterDate,
                    SearchFilterOperator.LessThanOrEqual => dateValue <= filterDate,
                    _ => true
                };
            }

            return true;
        });
    }

    private static double CalculateScore(SearchDocument doc, string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return doc.Boost;

        var terms = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var text = string.Join(" ", doc.TextFields.Values).ToLowerInvariant();
        
        var matchCount = terms.Count(term => text.Contains(term));
        return matchCount * doc.Boost;
    }

    private static Dictionary<string, string>? GetHighlights(SearchDocument doc, string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;

        var highlights = new Dictionary<string, string>();
        var terms = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var (field, text) in doc.TextFields)
        {
            var highlighted = text;
            foreach (var term in terms)
            {
                var index = highlighted.IndexOf(term, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    var match = highlighted.Substring(index, term.Length);
                    highlighted = highlighted.Replace(match, $"<em>{match}</em>");
                }
            }
            
            if (highlighted != text)
            {
                highlights[field] = highlighted;
            }
        }

        return highlights.Count > 0 ? highlights : null;
    }

    private static string HighlightPrefix(string word, string prefix)
    {
        if (word.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return $"<em>{word[..prefix.Length]}</em>{word[prefix.Length..]}";
        }
        return word;
    }

    #endregion
}
