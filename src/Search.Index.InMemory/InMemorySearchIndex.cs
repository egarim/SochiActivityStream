using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Search.Abstractions;
using Search.Core;

namespace Search.Index.InMemory;

/// <summary>
/// In-memory search index for development and testing.
/// Uses a simple inverted index with TF-IDF-like scoring.
/// </summary>
public sealed class InMemorySearchIndex : ISearchIndex
{
    private readonly ConcurrentDictionary<string, SearchDocument> _documents = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _invertedIndex = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly SearchIndexOptions _options;
    private readonly ITextAnalyzer _analyzer;
    private DateTimeOffset _lastUpdated = DateTimeOffset.MinValue;

    /// <summary>
    /// Creates a new InMemorySearchIndex.
    /// </summary>
    public InMemorySearchIndex(SearchIndexOptions? options = null, ITextAnalyzer? analyzer = null)
    {
        _options = options ?? new SearchIndexOptions();
        _analyzer = analyzer ?? new SimpleTextAnalyzer(_options.MinTokenLength, _options.MaxTokenLength);
    }

    #region ISearchIndexer

    /// <inheritdoc />
    public Task IndexAsync(SearchDocument document, CancellationToken ct = default)
    {
        SearchValidator.ValidateDocument(document);

        _lock.EnterWriteLock();
        try
        {
            var key = GetDocumentKey(document.TenantId, document.DocumentType, document.Id);

            // Remove old entry from inverted index if exists
            if (_documents.TryGetValue(key, out var existing))
            {
                RemoveFromInvertedIndex(existing);
            }

            // Store document with timestamp
            document.IndexedAt = DateTimeOffset.UtcNow;
            _documents[key] = CloneDocument(document);

            // Add to inverted index
            AddToInvertedIndex(document);
            _lastUpdated = DateTimeOffset.UtcNow;

            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public async Task IndexBatchAsync(IEnumerable<SearchDocument> documents, CancellationToken ct = default)
    {
        foreach (var doc in documents)
        {
            await IndexAsync(doc, ct);
            ct.ThrowIfCancellationRequested();
        }
    }

    /// <inheritdoc />
    public Task RemoveAsync(string tenantId, string documentType, string id, CancellationToken ct = default)
    {
        _lock.EnterWriteLock();
        try
        {
            var key = GetDocumentKey(tenantId, documentType, id);
            if (_documents.TryRemove(key, out var doc))
            {
                RemoveFromInvertedIndex(doc);
                _lastUpdated = DateTimeOffset.UtcNow;
            }
            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public Task RemoveByTypeAsync(string tenantId, string documentType, CancellationToken ct = default)
    {
        _lock.EnterWriteLock();
        try
        {
            var keysToRemove = _documents.Keys
                .Where(k => k.StartsWith($"{tenantId}|{documentType}|"))
                .ToList();

            foreach (var key in keysToRemove)
            {
                if (_documents.TryRemove(key, out var doc))
                {
                    RemoveFromInvertedIndex(doc);
                }
            }

            if (keysToRemove.Count > 0)
                _lastUpdated = DateTimeOffset.UtcNow;

            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public Task RemoveByTenantAsync(string tenantId, CancellationToken ct = default)
    {
        _lock.EnterWriteLock();
        try
        {
            var keysToRemove = _documents.Keys
                .Where(k => k.StartsWith($"{tenantId}|"))
                .ToList();

            foreach (var key in keysToRemove)
            {
                if (_documents.TryRemove(key, out var doc))
                {
                    RemoveFromInvertedIndex(doc);
                }
            }

            if (keysToRemove.Count > 0)
                _lastUpdated = DateTimeOffset.UtcNow;

            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public Task<IndexStats> GetStatsAsync(string? tenantId = null, CancellationToken ct = default)
    {
        _lock.EnterReadLock();
        try
        {
            var docs = tenantId != null
                ? _documents.Values.Where(d => d.TenantId == tenantId)
                : _documents.Values;

            var countByType = docs
                .GroupBy(d => d.DocumentType)
                .ToDictionary(g => g.Key, g => (long)g.Count());

            return Task.FromResult(new IndexStats
            {
                DocumentCount = docs.Count(),
                CountByType = countByType,
                LastUpdated = _lastUpdated == DateTimeOffset.MinValue ? null : _lastUpdated
            });
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public Task OptimizeAsync(string? tenantId = null, CancellationToken ct = default)
    {
        // No-op for in-memory implementation
        return Task.CompletedTask;
    }

    #endregion

    #region ISearchService

    /// <inheritdoc />
    public Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken ct = default)
    {
        SearchValidator.ValidateSearchRequest(request);

        var sw = Stopwatch.StartNew();

        _lock.EnterReadLock();
        try
        {
            // 1. Get candidate documents
            IEnumerable<SearchDocument> candidates = _documents.Values
                .Where(d => d.TenantId == request.TenantId);

            // Filter by document types
            if (request.DocumentTypes.Count > 0)
            {
                candidates = candidates.Where(d => request.DocumentTypes.Contains(d.DocumentType));
            }

            // 2. Apply filters
            foreach (var filter in request.Filters)
            {
                candidates = ApplyFilter(candidates, filter);
            }

            // Materialize for counting
            var candidateList = candidates.ToList();

            // 3. Score and rank by query relevance
            List<(SearchDocument Doc, double Score)> scored;
            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                var queryTokens = _analyzer.Tokenize(request.Query);
                scored = new List<(SearchDocument, double)>();

                foreach (var doc in candidateList)
                {
                    var score = CalculateScore(doc, queryTokens, request.SearchFields);
                    if (score > 0)
                    {
                        scored.Add((doc, score * doc.Boost));
                    }
                }
            }
            else
            {
                // No query text - include all candidates with neutral score
                scored = candidateList.Select(d => (d, 1.0 * d.Boost)).ToList();
            }

            // 4. Sort
            var sorted = ApplySorting(scored, request.Sorts);

            // 5. Pagination
            var offset = DecodeCursor(request.Cursor);
            var effectiveLimit = Math.Min(request.Limit, _options.MaxResultsPerQuery);
            var page = sorted.Skip(offset).Take(effectiveLimit + 1).ToList();
            var hasMore = page.Count > effectiveLimit;
            if (hasMore) page = page.Take(effectiveLimit).ToList();

            // 6. Build result
            var hits = page.Select(item => new SearchHit
            {
                Id = item.Doc.Id,
                DocumentType = item.Doc.DocumentType,
                Score = item.Score,
                Source = request.IncludeSource ? item.Doc.SourceEntity : null,
                Highlights = request.Highlight && !string.IsNullOrWhiteSpace(request.Query)
                    ? GenerateHighlights(item.Doc, request.Query)
                    : null
            }).ToList();

            // 7. Compute facets
            var facets = ComputeFacets(sorted.Select(s => s.Doc), request.Facets);

            sw.Stop();

            return Task.FromResult(new SearchResult
            {
                Hits = hits,
                TotalCount = sorted.Count,
                NextCursor = hasMore ? EncodeCursor(offset + effectiveLimit) : null,
                HasMore = hasMore,
                Facets = facets,
                ElapsedMs = sw.ElapsedMilliseconds
            });
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public Task<AutocompleteResult> AutocompleteAsync(AutocompleteRequest request, CancellationToken ct = default)
    {
        SearchValidator.ValidateAutocompleteRequest(request);

        _lock.EnterReadLock();
        try
        {
            var prefix = request.Prefix.ToLowerInvariant();
            var fields = request.Fields.Count > 0
                ? request.Fields
                : new List<string> { "displayName", "handle", "name", "title" };

            var effectiveLimit = Math.Min(request.Limit, _options.MaxAutocompleteSuggestions);

            var suggestions = _documents.Values
                .Where(d => d.TenantId == request.TenantId)
                .Where(d => request.DocumentTypes.Count == 0 || request.DocumentTypes.Contains(d.DocumentType))
                .SelectMany(d => fields
                    .Where(f => d.TextFields.ContainsKey(f))
                    .Select(f => new
                    {
                        Text = d.TextFields[f],
                        Doc = d,
                        Field = f
                    }))
                .Where(x => !string.IsNullOrEmpty(x.Text) && x.Text.ToLowerInvariant().Contains(prefix))
                .OrderByDescending(x => x.Text.ToLowerInvariant().StartsWith(prefix))
                .ThenByDescending(x => x.Doc.Boost)
                .Take(effectiveLimit)
                .Select(x => new AutocompleteSuggestion
                {
                    Text = x.Text,
                    Highlighted = HighlightMatch(x.Text, prefix),
                    DocumentId = x.Doc.Id,
                    DocumentType = x.Doc.DocumentType,
                    Score = x.Doc.Boost
                })
                .ToList();

            return Task.FromResult(new AutocompleteResult { Suggestions = suggestions });
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public Task<SearchDocument?> GetDocumentAsync(string tenantId, string documentType, string id, CancellationToken ct = default)
    {
        _lock.EnterReadLock();
        try
        {
            var key = GetDocumentKey(tenantId, documentType, id);
            if (_documents.TryGetValue(key, out var doc))
            {
                return Task.FromResult<SearchDocument?>(CloneDocument(doc));
            }
            return Task.FromResult<SearchDocument?>(null);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string tenantId, string documentType, string id, CancellationToken ct = default)
    {
        _lock.EnterReadLock();
        try
        {
            var key = GetDocumentKey(tenantId, documentType, id);
            return Task.FromResult(_documents.ContainsKey(key));
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    #endregion

    #region Private Helpers

    private static string GetDocumentKey(string tenantId, string documentType, string id)
        => $"{tenantId}|{documentType}|{id}";

    private static string EncodeCursor(int offset)
        => Convert.ToBase64String(BitConverter.GetBytes(offset));

    private static int DecodeCursor(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor)) return 0;
        try
        {
            return BitConverter.ToInt32(Convert.FromBase64String(cursor), 0);
        }
        catch
        {
            return 0;
        }
    }

    private void AddToInvertedIndex(SearchDocument doc)
    {
        var key = GetDocumentKey(doc.TenantId, doc.DocumentType, doc.Id);

        foreach (var (fieldName, text) in doc.TextFields)
        {
            var tokens = _analyzer.Tokenize(text);
            foreach (var token in tokens)
            {
                var indexKey = $"{doc.TenantId}|{token}";
                if (!_invertedIndex.TryGetValue(indexKey, out var docSet))
                {
                    docSet = new HashSet<string>();
                    _invertedIndex[indexKey] = docSet;
                }
                docSet.Add(key);
            }
        }
    }

    private void RemoveFromInvertedIndex(SearchDocument doc)
    {
        var key = GetDocumentKey(doc.TenantId, doc.DocumentType, doc.Id);

        foreach (var (fieldName, text) in doc.TextFields)
        {
            var tokens = _analyzer.Tokenize(text);
            foreach (var token in tokens)
            {
                var indexKey = $"{doc.TenantId}|{token}";
                if (_invertedIndex.TryGetValue(indexKey, out var docSet))
                {
                    docSet.Remove(key);
                    if (docSet.Count == 0)
                    {
                        _invertedIndex.TryRemove(indexKey, out _);
                    }
                }
            }
        }
    }

    private double CalculateScore(SearchDocument doc, List<string> queryTokens, List<string> searchFields)
    {
        double score = 0;
        var fieldsToSearch = searchFields.Count > 0
            ? searchFields
            : doc.TextFields.Keys.ToList();

        foreach (var field in fieldsToSearch)
        {
            if (!doc.TextFields.TryGetValue(field, out var text)) continue;

            var tokens = _analyzer.Tokenize(text);
            var totalTokens = tokens.Count;
            if (totalTokens == 0) continue;

            foreach (var queryToken in queryTokens)
            {
                // Count term frequency
                var tf = tokens.Count(t => t == queryToken || t.StartsWith(queryToken));
                if (tf > 0)
                {
                    // Simple TF scoring with dampening
                    score += 1 + Math.Log(tf);
                }
            }
        }

        // Only apply recency boost if there's a term match (score > 0)
        if (score > 0 && _options.RecencyBoostWeight > 0 && doc.DateFields.TryGetValue("createdAt", out var createdAt))
        {
            var daysSinceCreation = (DateTimeOffset.UtcNow - createdAt).TotalDays;
            var recencyFactor = Math.Max(0, 1 - (daysSinceCreation / _options.RecencyBoostDays));
            score += recencyFactor * _options.RecencyBoostWeight;
        }

        return score;
    }

    private IEnumerable<SearchDocument> ApplyFilter(IEnumerable<SearchDocument> docs, SearchFilter filter)
    {
        return filter.Operator switch
        {
            SearchFilterOperator.Equals => docs.Where(d => MatchesEquals(d, filter)),
            SearchFilterOperator.NotEquals => docs.Where(d => !MatchesEquals(d, filter)),
            SearchFilterOperator.In => docs.Where(d => MatchesIn(d, filter)),
            SearchFilterOperator.NotIn => docs.Where(d => !MatchesIn(d, filter)),
            SearchFilterOperator.GreaterThan => docs.Where(d => CompareField(d, filter) > 0),
            SearchFilterOperator.GreaterThanOrEqual => docs.Where(d => CompareField(d, filter) >= 0),
            SearchFilterOperator.LessThan => docs.Where(d => CompareField(d, filter) < 0),
            SearchFilterOperator.LessThanOrEqual => docs.Where(d => CompareField(d, filter) <= 0),
            SearchFilterOperator.StartsWith => docs.Where(d => MatchesStartsWith(d, filter)),
            SearchFilterOperator.Exists => docs.Where(d => FieldExists(d, filter.Field)),
            SearchFilterOperator.Between => docs.Where(d => MatchesBetween(d, filter)),
            _ => docs
        };
    }

    private bool MatchesEquals(SearchDocument doc, SearchFilter filter)
    {
        var valueStr = filter.Value?.ToString()?.ToLowerInvariant();
        if (valueStr == null) return false;

        if (doc.KeywordFields.TryGetValue(filter.Field, out var keywords))
        {
            return keywords.Any(k => k.ToLowerInvariant() == valueStr);
        }

        if (doc.TextFields.TryGetValue(filter.Field, out var text))
        {
            return text.ToLowerInvariant() == valueStr;
        }

        return false;
    }

    private bool MatchesIn(SearchDocument doc, SearchFilter filter)
    {
        var values = filter.Value as IEnumerable<object> ?? new[] { filter.Value };
        var valueSet = values.Select(v => v?.ToString()?.ToLowerInvariant()).Where(v => v != null).ToHashSet();

        if (doc.KeywordFields.TryGetValue(filter.Field, out var keywords))
        {
            return keywords.Any(k => valueSet.Contains(k.ToLowerInvariant()));
        }

        if (doc.TextFields.TryGetValue(filter.Field, out var text))
        {
            return valueSet.Contains(text.ToLowerInvariant());
        }

        return false;
    }

    private int CompareField(SearchDocument doc, SearchFilter filter)
    {
        if (doc.NumericFields.TryGetValue(filter.Field, out var numValue))
        {
            var filterValue = Convert.ToDouble(filter.Value);
            return numValue.CompareTo(filterValue);
        }

        if (doc.DateFields.TryGetValue(filter.Field, out var dateValue))
        {
            var filterValue = filter.Value is DateTimeOffset dto ? dto : DateTimeOffset.Parse(filter.Value.ToString()!);
            return dateValue.CompareTo(filterValue);
        }

        return 0;
    }

    private bool MatchesStartsWith(SearchDocument doc, SearchFilter filter)
    {
        var prefix = filter.Value?.ToString()?.ToLowerInvariant();
        if (prefix == null) return false;

        if (doc.KeywordFields.TryGetValue(filter.Field, out var keywords))
        {
            return keywords.Any(k => k.ToLowerInvariant().StartsWith(prefix));
        }

        if (doc.TextFields.TryGetValue(filter.Field, out var text))
        {
            return text.ToLowerInvariant().StartsWith(prefix);
        }

        return false;
    }

    private bool MatchesBetween(SearchDocument doc, SearchFilter filter)
    {
        if (filter.Value is not object[] range || range.Length != 2) return false;

        if (doc.NumericFields.TryGetValue(filter.Field, out var numValue))
        {
            var min = Convert.ToDouble(range[0]);
            var max = Convert.ToDouble(range[1]);
            return numValue >= min && numValue <= max;
        }

        if (doc.DateFields.TryGetValue(filter.Field, out var dateValue))
        {
            var min = range[0] is DateTimeOffset dto1 ? dto1 : DateTimeOffset.Parse(range[0].ToString()!);
            var max = range[1] is DateTimeOffset dto2 ? dto2 : DateTimeOffset.Parse(range[1].ToString()!);
            return dateValue >= min && dateValue <= max;
        }

        return false;
    }

    private bool FieldExists(SearchDocument doc, string field)
    {
        return doc.TextFields.ContainsKey(field) ||
               doc.KeywordFields.ContainsKey(field) ||
               doc.NumericFields.ContainsKey(field) ||
               doc.DateFields.ContainsKey(field);
    }

    private List<(SearchDocument Doc, double Score)> ApplySorting(
        List<(SearchDocument Doc, double Score)> docs,
        List<SearchSort> sorts)
    {
        if (sorts.Count == 0)
        {
            // Default: sort by score descending
            return docs.OrderByDescending(x => x.Score).ToList();
        }

        IOrderedEnumerable<(SearchDocument Doc, double Score)>? ordered = null;

        foreach (var sort in sorts)
        {
            Func<(SearchDocument Doc, double Score), object> selector = sort.Field switch
            {
                "_score" => x => x.Score,
                _ when sort.Field.StartsWith("numeric:") => x =>
                    x.Doc.NumericFields.GetValueOrDefault(sort.Field[8..], 0),
                _ when sort.Field.StartsWith("date:") => x =>
                    x.Doc.DateFields.GetValueOrDefault(sort.Field[5..], DateTimeOffset.MinValue),
                _ => x => x.Doc.TextFields.GetValueOrDefault(sort.Field, "") ?? ""
            };

            if (ordered == null)
            {
                ordered = sort.Direction == SortDirection.Ascending
                    ? docs.OrderBy(selector)
                    : docs.OrderByDescending(selector);
            }
            else
            {
                ordered = sort.Direction == SortDirection.Ascending
                    ? ordered.ThenBy(selector)
                    : ordered.ThenByDescending(selector);
            }
        }

        return ordered?.ToList() ?? docs;
    }

    private Dictionary<string, string> GenerateHighlights(SearchDocument doc, string query)
    {
        var highlights = new Dictionary<string, string>();
        var queryTokens = _analyzer.Tokenize(query);

        foreach (var (field, text) in doc.TextFields)
        {
            var highlighted = text;
            foreach (var token in queryTokens)
            {
                var pattern = $@"\b({Regex.Escape(token)})\b";
                highlighted = Regex.Replace(
                    highlighted,
                    pattern,
                    $"{_options.HighlightPreTag}$1{_options.HighlightPostTag}",
                    RegexOptions.IgnoreCase);
            }

            if (highlighted != text)
            {
                highlights[field] = highlighted;
            }
        }

        return highlights;
    }

    private string HighlightMatch(string text, string prefix)
    {
        var idx = text.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return text;

        var pre = text[..idx];
        var match = text.Substring(idx, prefix.Length);
        var post = text[(idx + prefix.Length)..];

        return $"{pre}{_options.HighlightPreTag}{match}{_options.HighlightPostTag}{post}";
    }

    private Dictionary<string, List<FacetValue>> ComputeFacets(
        IEnumerable<SearchDocument> docs,
        List<string> facetFields)
    {
        if (facetFields.Count == 0) return new Dictionary<string, List<FacetValue>>();

        var docList = docs.ToList();
        var facets = new Dictionary<string, List<FacetValue>>();

        foreach (var field in facetFields)
        {
            var valueCounts = new Dictionary<string, long>();

            foreach (var doc in docList)
            {
                if (doc.KeywordFields.TryGetValue(field, out var keywords))
                {
                    foreach (var keyword in keywords)
                    {
                        var key = keyword.ToLowerInvariant();
                        valueCounts[key] = valueCounts.GetValueOrDefault(key, 0) + 1;
                    }
                }
            }

            facets[field] = valueCounts
                .Select(kv => new FacetValue { Value = kv.Key, Count = kv.Value })
                .OrderByDescending(f => f.Count)
                .ToList();
        }

        return facets;
    }

    private static SearchDocument CloneDocument(SearchDocument doc)
    {
        return new SearchDocument
        {
            Id = doc.Id,
            TenantId = doc.TenantId,
            DocumentType = doc.DocumentType,
            TextFields = new Dictionary<string, string>(doc.TextFields),
            KeywordFields = doc.KeywordFields.ToDictionary(kv => kv.Key, kv => new List<string>(kv.Value)),
            NumericFields = new Dictionary<string, double>(doc.NumericFields),
            DateFields = new Dictionary<string, DateTimeOffset>(doc.DateFields),
            IndexedAt = doc.IndexedAt,
            Boost = doc.Boost,
            SourceEntity = doc.SourceEntity
        };
    }

    #endregion
}
