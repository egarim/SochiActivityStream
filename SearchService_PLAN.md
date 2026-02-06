# Search Service (Agnostic) — C# Library Plan for an LLM Agent Programmer

**Goal:** Build a **Search Service** as a C# library that provides full-text search capabilities with pluggable backends. The base implementation uses an in-memory index for development and testing, with extension points for production backends (Elasticsearch, Azure Cognitive Search, Algolia).

The service supports:
- Full-text search across entities (Profiles, Posts, Groups)
- Autocomplete/typeahead suggestions
- Faceted filtering
- Ranking and relevance scoring
- Real-time index updates

> **Note:** This is an **agnostic abstraction layer**. The in-memory implementation is for development/demos. Production deployments should use a proper search engine.

---

## 0) Definition of Done (v1 / MVP)

### 0.1 Project References

```
Search.Abstractions
  └── (no dependencies - pure contracts)

Search.Core
  └── Search.Abstractions

Search.Index.InMemory
  └── Search.Abstractions
  └── (uses Lucene.NET or simple in-memory inverted index)

Search.Tests
  └── All of the above
```

### 0.2 Deliverables (projects)

1. **Search.Abstractions**
   - DTOs for search requests/results
   - Interfaces: service + indexer + provider
   - No implementation dependencies

2. **Search.Core**
   - `SearchService` implementing `ISearchService`
   - Normalization, tokenization helpers
   - Query parsing

3. **Search.Index.InMemory**
   - Reference implementation using in-memory inverted index
   - Supports basic full-text search, filtering, ranking
   - Good enough for demos and tests

4. **Search.Tests**
   - Unit tests for search logic
   - Integration tests with in-memory index

### 0.3 Future Extensions (not in v1)

- `Search.Index.Elasticsearch` - Elasticsearch backend
- `Search.Index.AzureCognitiveSearch` - Azure Cognitive Search backend
- `Search.Index.Algolia` - Algolia backend

### 0.4 Success Criteria

- All tests green
- Search profiles by name/handle works
- Search posts by content works
- Autocomplete suggestions work
- Pagination and cursor support
- Facet counting (optional but included)

---

## 1) Core Concepts

### 1.1 Document-Centric Model
Everything is a **SearchDocument** with:
- `DocumentType` (Profile, Post, Group, etc.)
- `Id` (unique within type)
- `TenantId` (multi-tenancy)
- Fields (text, keyword, numeric, date)

### 1.2 Index vs. Search
- **Indexer** (`ISearchIndexer`): Adds/updates/removes documents
- **Searcher** (`ISearchService`): Queries documents

### 1.3 Field Types
| Type | Description | Use Case |
|------|-------------|----------|
| Text | Full-text analyzed | Body, description |
| Keyword | Exact match only | Status, type, tags |
| Numeric | Numbers (int, long, double) | Count, score |
| Date | DateTimeOffset | CreatedAt, UpdatedAt |

### 1.4 Analysis Pipeline
```
Input Text → Lowercase → Remove Punctuation → Tokenize → Stem (optional) → Index
```

### 1.5 Ranking Factors
Default ranking by:
1. Text relevance (TF-IDF or BM25)
2. Recency boost (optional)
3. Popularity boost (optional, e.g., follower count)

### 1.6 Multi-Tenancy
All documents and queries are scoped by `TenantId`.

---

## 2) DTOs

### 2.1 SearchDocument

```csharp
/// <summary>
/// A document to be indexed for search.
/// </summary>
public sealed class SearchDocument
{
    /// <summary>
    /// Unique identifier within the document type.
    /// </summary>
    public required string Id { get; set; }
    
    /// <summary>
    /// Multi-tenancy partition key.
    /// </summary>
    public required string TenantId { get; set; }
    
    /// <summary>
    /// Document type (e.g., "Profile", "Post", "Group").
    /// </summary>
    public required string DocumentType { get; set; }
    
    /// <summary>
    /// Text fields for full-text search.
    /// Key = field name, Value = text content.
    /// </summary>
    public Dictionary<string, string> TextFields { get; set; } = new();
    
    /// <summary>
    /// Keyword fields for exact matching/filtering.
    /// Key = field name, Value = keyword value(s).
    /// </summary>
    public Dictionary<string, List<string>> KeywordFields { get; set; } = new();
    
    /// <summary>
    /// Numeric fields for range queries and sorting.
    /// </summary>
    public Dictionary<string, double> NumericFields { get; set; } = new();
    
    /// <summary>
    /// Date fields for range queries and sorting.
    /// </summary>
    public Dictionary<string, DateTimeOffset> DateFields { get; set; } = new();
    
    /// <summary>
    /// When the document was last indexed.
    /// </summary>
    public DateTimeOffset IndexedAt { get; set; }
    
    /// <summary>
    /// Optional boost factor for ranking (default 1.0).
    /// </summary>
    public double Boost { get; set; } = 1.0;
    
    /// <summary>
    /// Store the original entity for retrieval (optional).
    /// </summary>
    public object? SourceEntity { get; set; }
}
```

### 2.2 SearchRequest

```csharp
/// <summary>
/// Search query parameters.
/// </summary>
public sealed class SearchRequest
{
    /// <summary>
    /// Multi-tenancy partition key.
    /// </summary>
    public required string TenantId { get; set; }
    
    /// <summary>
    /// Search query text (full-text search).
    /// </summary>
    public string? Query { get; set; }
    
    /// <summary>
    /// Document types to search (empty = all types).
    /// </summary>
    public List<string> DocumentTypes { get; set; } = new();
    
    /// <summary>
    /// Text fields to search in (empty = all text fields).
    /// </summary>
    public List<string> SearchFields { get; set; } = new();
    
    /// <summary>
    /// Filters to apply.
    /// </summary>
    public List<SearchFilter> Filters { get; set; } = new();
    
    /// <summary>
    /// Sorting options.
    /// </summary>
    public List<SearchSort> Sorts { get; set; } = new();
    
    /// <summary>
    /// Facets to compute.
    /// </summary>
    public List<string> Facets { get; set; } = new();
    
    /// <summary>
    /// Pagination cursor (opaque string from previous result).
    /// </summary>
    public string? Cursor { get; set; }
    
    /// <summary>
    /// Maximum results to return.
    /// </summary>
    public int Limit { get; set; } = 20;
    
    /// <summary>
    /// Whether to include the source entity in results.
    /// </summary>
    public bool IncludeSource { get; set; } = true;
    
    /// <summary>
    /// Whether to highlight matching text.
    /// </summary>
    public bool Highlight { get; set; } = false;
}
```

### 2.3 SearchFilter

```csharp
/// <summary>
/// A filter condition for search queries.
/// </summary>
public sealed class SearchFilter
{
    /// <summary>
    /// Field name to filter on.
    /// </summary>
    public required string Field { get; set; }
    
    /// <summary>
    /// Filter operator.
    /// </summary>
    public SearchFilterOperator Operator { get; set; } = SearchFilterOperator.Equals;
    
    /// <summary>
    /// Value(s) to filter by.
    /// </summary>
    public required object Value { get; set; }
}

public enum SearchFilterOperator
{
    /// <summary>
    /// Exact match (keyword fields).
    /// </summary>
    Equals = 0,
    
    /// <summary>
    /// Not equal.
    /// </summary>
    NotEquals = 1,
    
    /// <summary>
    /// Contains any of the values (keyword arrays).
    /// </summary>
    In = 2,
    
    /// <summary>
    /// Does not contain any of the values.
    /// </summary>
    NotIn = 3,
    
    /// <summary>
    /// Greater than (numeric/date).
    /// </summary>
    GreaterThan = 4,
    
    /// <summary>
    /// Greater than or equal (numeric/date).
    /// </summary>
    GreaterThanOrEqual = 5,
    
    /// <summary>
    /// Less than (numeric/date).
    /// </summary>
    LessThan = 6,
    
    /// <summary>
    /// Less than or equal (numeric/date).
    /// </summary>
    LessThanOrEqual = 7,
    
    /// <summary>
    /// Range (between two values, inclusive).
    /// </summary>
    Between = 8,
    
    /// <summary>
    /// Prefix match (starts with).
    /// </summary>
    StartsWith = 9,
    
    /// <summary>
    /// Field exists (not null).
    /// </summary>
    Exists = 10
}
```

### 2.4 SearchSort

```csharp
/// <summary>
/// Sorting specification for search results.
/// </summary>
public sealed class SearchSort
{
    /// <summary>
    /// Field name to sort by. Use "_score" for relevance.
    /// </summary>
    public required string Field { get; set; }
    
    /// <summary>
    /// Sort direction.
    /// </summary>
    public SortDirection Direction { get; set; } = SortDirection.Descending;
}

public enum SortDirection
{
    Ascending = 0,
    Descending = 1
}
```

### 2.5 SearchResult

```csharp
/// <summary>
/// Search result container.
/// </summary>
public sealed class SearchResult
{
    /// <summary>
    /// Matching documents.
    /// </summary>
    public List<SearchHit> Hits { get; set; } = new();
    
    /// <summary>
    /// Total number of matching documents.
    /// </summary>
    public long TotalCount { get; set; }
    
    /// <summary>
    /// Cursor for next page (null if no more results).
    /// </summary>
    public string? NextCursor { get; set; }
    
    /// <summary>
    /// Whether more results are available.
    /// </summary>
    public bool HasMore { get; set; }
    
    /// <summary>
    /// Facet results (if requested).
    /// </summary>
    public Dictionary<string, List<FacetValue>> Facets { get; set; } = new();
    
    /// <summary>
    /// Query execution time in milliseconds.
    /// </summary>
    public long ElapsedMs { get; set; }
}

/// <summary>
/// A single search hit.
/// </summary>
public sealed class SearchHit
{
    /// <summary>
    /// Document ID.
    /// </summary>
    public required string Id { get; set; }
    
    /// <summary>
    /// Document type.
    /// </summary>
    public required string DocumentType { get; set; }
    
    /// <summary>
    /// Relevance score.
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// Highlighted text snippets (field → highlighted text).
    /// </summary>
    public Dictionary<string, string>? Highlights { get; set; }
    
    /// <summary>
    /// The source entity (if IncludeSource was true).
    /// </summary>
    public object? Source { get; set; }
}

/// <summary>
/// A facet value with count.
/// </summary>
public sealed class FacetValue
{
    /// <summary>
    /// The value.
    /// </summary>
    public required string Value { get; set; }
    
    /// <summary>
    /// Number of documents with this value.
    /// </summary>
    public long Count { get; set; }
}
```

### 2.6 Autocomplete Types

```csharp
/// <summary>
/// Autocomplete/typeahead request.
/// </summary>
public sealed class AutocompleteRequest
{
    /// <summary>
    /// Multi-tenancy partition key.
    /// </summary>
    public required string TenantId { get; set; }
    
    /// <summary>
    /// Partial query text.
    /// </summary>
    public required string Prefix { get; set; }
    
    /// <summary>
    /// Document types to include (empty = all).
    /// </summary>
    public List<string> DocumentTypes { get; set; } = new();
    
    /// <summary>
    /// Fields to search in (empty = default autocomplete fields).
    /// </summary>
    public List<string> Fields { get; set; } = new();
    
    /// <summary>
    /// Maximum suggestions to return.
    /// </summary>
    public int Limit { get; set; } = 10;
}

/// <summary>
/// Autocomplete result.
/// </summary>
public sealed class AutocompleteResult
{
    /// <summary>
    /// Suggestions.
    /// </summary>
    public List<AutocompleteSuggestion> Suggestions { get; set; } = new();
}

/// <summary>
/// A single autocomplete suggestion.
/// </summary>
public sealed class AutocompleteSuggestion
{
    /// <summary>
    /// Suggested text.
    /// </summary>
    public required string Text { get; set; }
    
    /// <summary>
    /// Highlighted text with matching portions marked.
    /// </summary>
    public string? Highlighted { get; set; }
    
    /// <summary>
    /// Document ID (if suggestion is from a specific document).
    /// </summary>
    public string? DocumentId { get; set; }
    
    /// <summary>
    /// Document type.
    /// </summary>
    public string? DocumentType { get; set; }
    
    /// <summary>
    /// Score/weight of the suggestion.
    /// </summary>
    public double Score { get; set; }
}
```

---

## 3) Interfaces

### 3.1 ISearchService

```csharp
/// <summary>
/// Search service for querying indexed documents.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Execute a search query.
    /// </summary>
    Task<SearchResult> SearchAsync(
        SearchRequest request, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Get autocomplete suggestions.
    /// </summary>
    Task<AutocompleteResult> AutocompleteAsync(
        AutocompleteRequest request, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Get a single document by ID.
    /// </summary>
    Task<SearchDocument?> GetDocumentAsync(
        string tenantId, 
        string documentType, 
        string id, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Check if a document exists.
    /// </summary>
    Task<bool> ExistsAsync(
        string tenantId, 
        string documentType, 
        string id, 
        CancellationToken ct = default);
}
```

### 3.2 ISearchIndexer

```csharp
/// <summary>
/// Indexer for adding/updating/removing documents.
/// </summary>
public interface ISearchIndexer
{
    /// <summary>
    /// Index a single document (add or update).
    /// </summary>
    Task IndexAsync(
        SearchDocument document, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Index multiple documents in batch.
    /// </summary>
    Task IndexBatchAsync(
        IEnumerable<SearchDocument> documents, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Remove a document from the index.
    /// </summary>
    Task RemoveAsync(
        string tenantId, 
        string documentType, 
        string id, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Remove all documents of a type for a tenant.
    /// </summary>
    Task RemoveByTypeAsync(
        string tenantId, 
        string documentType, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Remove all documents for a tenant.
    /// </summary>
    Task RemoveByTenantAsync(
        string tenantId, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Get index statistics.
    /// </summary>
    Task<IndexStats> GetStatsAsync(
        string? tenantId = null, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Optimize the index (implementation-specific).
    /// </summary>
    Task OptimizeAsync(
        string? tenantId = null, 
        CancellationToken ct = default);
}

/// <summary>
/// Index statistics.
/// </summary>
public sealed class IndexStats
{
    /// <summary>
    /// Total document count.
    /// </summary>
    public long DocumentCount { get; set; }
    
    /// <summary>
    /// Document count by type.
    /// </summary>
    public Dictionary<string, long> CountByType { get; set; } = new();
    
    /// <summary>
    /// Index size in bytes (if available).
    /// </summary>
    public long? SizeBytes { get; set; }
    
    /// <summary>
    /// When the index was last updated.
    /// </summary>
    public DateTimeOffset? LastUpdated { get; set; }
}
```

### 3.3 ISearchIndex (Combined)

```csharp
/// <summary>
/// Combined interface for implementations that provide both search and indexing.
/// </summary>
public interface ISearchIndex : ISearchService, ISearchIndexer
{
}
```

---

## 4) Document Builders

### 4.1 Profile Document Builder

```csharp
/// <summary>
/// Helper to build search documents from domain entities.
/// </summary>
public static class SearchDocumentBuilders
{
    /// <summary>
    /// Build a search document from a Profile.
    /// </summary>
    public static SearchDocument FromProfile(ProfileDto profile, string tenantId)
    {
        return new SearchDocument
        {
            Id = profile.Id!,
            TenantId = tenantId,
            DocumentType = "Profile",
            TextFields = new Dictionary<string, string>
            {
                ["displayName"] = profile.DisplayName ?? "",
                ["handle"] = profile.Handle,
                ["bio"] = profile.Bio ?? ""
            },
            KeywordFields = new Dictionary<string, List<string>>
            {
                ["handle_exact"] = new() { profile.Handle.ToLowerInvariant() },
                ["isPrivate"] = new() { profile.IsPrivate.ToString().ToLowerInvariant() }
            },
            NumericFields = new Dictionary<string, double>
            {
                ["followerCount"] = profile.FollowerCount
            },
            DateFields = new Dictionary<string, DateTimeOffset>
            {
                ["createdAt"] = profile.CreatedAt
            },
            Boost = 1.0 + Math.Log10(Math.Max(1, profile.FollowerCount)) * 0.1,
            SourceEntity = profile,
            IndexedAt = DateTimeOffset.UtcNow
        };
    }
    
    /// <summary>
    /// Build a search document from a Post.
    /// </summary>
    public static SearchDocument FromPost(PostDto post, string tenantId)
    {
        return new SearchDocument
        {
            Id = post.Id!,
            TenantId = tenantId,
            DocumentType = "Post",
            TextFields = new Dictionary<string, string>
            {
                ["body"] = post.Body,
                ["authorName"] = post.Author.Display ?? ""
            },
            KeywordFields = new Dictionary<string, List<string>>
            {
                ["authorId"] = new() { post.Author.Id },
                ["visibility"] = new() { post.Visibility.ToString().ToLowerInvariant() },
                ["hashtags"] = ExtractHashtags(post.Body)
            },
            NumericFields = new Dictionary<string, double>
            {
                ["commentCount"] = post.CommentCount,
                ["reactionCount"] = post.ReactionCounts.Values.Sum()
            },
            DateFields = new Dictionary<string, DateTimeOffset>
            {
                ["createdAt"] = post.CreatedAt
            },
            SourceEntity = post,
            IndexedAt = DateTimeOffset.UtcNow
        };
    }
    
    /// <summary>
    /// Build a search document from a Group.
    /// </summary>
    public static SearchDocument FromGroup(GroupDto group, string tenantId)
    {
        return new SearchDocument
        {
            Id = group.Id!,
            TenantId = tenantId,
            DocumentType = "Group",
            TextFields = new Dictionary<string, string>
            {
                ["name"] = group.Name,
                ["handle"] = group.Handle,
                ["description"] = group.Description ?? ""
            },
            KeywordFields = new Dictionary<string, List<string>>
            {
                ["handle_exact"] = new() { group.Handle.ToLowerInvariant() },
                ["privacy"] = new() { group.Privacy.ToString().ToLowerInvariant() }
            },
            NumericFields = new Dictionary<string, double>
            {
                ["memberCount"] = group.MemberCount
            },
            DateFields = new Dictionary<string, DateTimeOffset>
            {
                ["createdAt"] = group.CreatedAt
            },
            Boost = 1.0 + Math.Log10(Math.Max(1, group.MemberCount)) * 0.1,
            SourceEntity = group,
            IndexedAt = DateTimeOffset.UtcNow
        };
    }
    
    private static List<string> ExtractHashtags(string text)
    {
        var matches = System.Text.RegularExpressions.Regex.Matches(
            text, 
            @"#(\w+)", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        return matches
            .Select(m => m.Groups[1].Value.ToLowerInvariant())
            .Distinct()
            .ToList();
    }
}
```

---

## 5) In-Memory Index Implementation

### 5.1 InMemorySearchIndex

```csharp
/// <summary>
/// In-memory search index for development and testing.
/// Uses a simple inverted index with TF-IDF scoring.
/// </summary>
public sealed class InMemorySearchIndex : ISearchIndex
{
    private readonly ConcurrentDictionary<string, SearchDocument> _documents = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _invertedIndex = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly SearchIndexOptions _options;
    private readonly ITextAnalyzer _analyzer;

    public InMemorySearchIndex(SearchIndexOptions? options = null)
    {
        _options = options ?? new SearchIndexOptions();
        _analyzer = new SimpleTextAnalyzer();
    }

    public Task IndexAsync(SearchDocument document, CancellationToken ct = default)
    {
        _lock.EnterWriteLock();
        try
        {
            var key = GetDocumentKey(document.TenantId, document.DocumentType, document.Id);
            
            // Remove old entry from inverted index if exists
            if (_documents.TryGetValue(key, out var existing))
            {
                RemoveFromInvertedIndex(existing);
            }
            
            // Store document
            document.IndexedAt = DateTimeOffset.UtcNow;
            _documents[key] = document;
            
            // Add to inverted index
            AddToInvertedIndex(document);
            
            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        _lock.EnterReadLock();
        try
        {
            // 1. Get candidate documents
            var candidates = _documents.Values
                .Where(d => d.TenantId == request.TenantId)
                .Where(d => !request.DocumentTypes.Any() || request.DocumentTypes.Contains(d.DocumentType));
            
            // 2. Apply filters
            foreach (var filter in request.Filters)
            {
                candidates = ApplyFilter(candidates, filter);
            }
            
            // 3. Score and rank by query relevance
            var scored = new List<(SearchDocument Doc, double Score)>();
            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                var queryTokens = _analyzer.Tokenize(request.Query);
                foreach (var doc in candidates)
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
                scored = candidates.Select(d => (d, 1.0 * d.Boost)).ToList();
            }
            
            // 4. Sort
            var sorted = ApplySorting(scored, request.Sorts);
            
            // 5. Pagination
            var offset = DecodeCursor(request.Cursor);
            var page = sorted.Skip(offset).Take(request.Limit + 1).ToList();
            var hasMore = page.Count > request.Limit;
            if (hasMore) page = page.Take(request.Limit).ToList();
            
            // 6. Build result
            var hits = page.Select(item => new SearchHit
            {
                Id = item.Doc.Id,
                DocumentType = item.Doc.DocumentType,
                Score = item.Score,
                Source = request.IncludeSource ? item.Doc.SourceEntity : null,
                Highlights = request.Highlight 
                    ? GenerateHighlights(item.Doc, request.Query!) 
                    : null
            }).ToList();
            
            // 7. Compute facets
            var facets = ComputeFacets(sorted.Select(s => s.Doc), request.Facets);
            
            sw.Stop();
            
            return Task.FromResult(new SearchResult
            {
                Hits = hits,
                TotalCount = sorted.Count,
                NextCursor = hasMore ? EncodeCursor(offset + request.Limit) : null,
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

    public Task<AutocompleteResult> AutocompleteAsync(AutocompleteRequest request, CancellationToken ct = default)
    {
        _lock.EnterReadLock();
        try
        {
            var prefix = request.Prefix.ToLowerInvariant();
            var fields = request.Fields.Any() 
                ? request.Fields 
                : new List<string> { "displayName", "handle", "name" };
            
            var suggestions = _documents.Values
                .Where(d => d.TenantId == request.TenantId)
                .Where(d => !request.DocumentTypes.Any() || request.DocumentTypes.Contains(d.DocumentType))
                .SelectMany(d => fields
                    .Where(f => d.TextFields.ContainsKey(f))
                    .Select(f => new
                    {
                        Text = d.TextFields[f],
                        Doc = d,
                        Field = f
                    }))
                .Where(x => x.Text.ToLowerInvariant().Contains(prefix))
                .OrderByDescending(x => x.Text.ToLowerInvariant().StartsWith(prefix))
                .ThenByDescending(x => x.Doc.Boost)
                .Take(request.Limit)
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

    // ... remaining implementation methods
    
    private double CalculateScore(SearchDocument doc, List<string> queryTokens, List<string> searchFields)
    {
        double score = 0;
        var fieldsToSearch = searchFields.Any() 
            ? searchFields 
            : doc.TextFields.Keys.ToList();
        
        foreach (var field in fieldsToSearch)
        {
            if (!doc.TextFields.TryGetValue(field, out var text)) continue;
            
            var tokens = _analyzer.Tokenize(text);
            foreach (var queryToken in queryTokens)
            {
                var tf = tokens.Count(t => t == queryToken);
                if (tf > 0)
                {
                    // Simple TF scoring
                    score += 1 + Math.Log(tf);
                }
            }
        }
        
        return score;
    }
    
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
}
```

### 5.2 ITextAnalyzer

```csharp
/// <summary>
/// Text analysis for indexing and searching.
/// </summary>
public interface ITextAnalyzer
{
    /// <summary>
    /// Tokenize text into searchable tokens.
    /// </summary>
    List<string> Tokenize(string text);
    
    /// <summary>
    /// Normalize a single term.
    /// </summary>
    string Normalize(string term);
}

/// <summary>
/// Simple text analyzer: lowercase, remove punctuation, split on whitespace.
/// </summary>
public sealed class SimpleTextAnalyzer : ITextAnalyzer
{
    private static readonly char[] Separators = { ' ', '\t', '\n', '\r', ',', '.', '!', '?', ';', ':', '-', '_', '(', ')', '[', ']', '{', '}', '"', '\'' };

    public List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();
        
        return text
            .ToLowerInvariant()
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= 2)
            .Distinct()
            .ToList();
    }

    public string Normalize(string term)
    {
        return term.ToLowerInvariant().Trim();
    }
}
```

---

## 6) Configuration

### 6.1 SearchIndexOptions

```csharp
public sealed class SearchIndexOptions
{
    /// <summary>
    /// Maximum documents to return in a single query.
    /// </summary>
    public int MaxResultsPerQuery { get; set; } = 100;
    
    /// <summary>
    /// Maximum autocomplete suggestions.
    /// </summary>
    public int MaxAutocompleteSuggestions { get; set; } = 20;
    
    /// <summary>
    /// Minimum query length for search.
    /// </summary>
    public int MinQueryLength { get; set; } = 1;
    
    /// <summary>
    /// Default boost for recency (newer documents score higher).
    /// </summary>
    public double RecencyBoostWeight { get; set; } = 0.1;
    
    /// <summary>
    /// How many days until recency boost decays to zero.
    /// </summary>
    public int RecencyBoostDays { get; set; } = 30;
}
```

---

## 7) Validation Rules

| Field | Rules |
|-------|-------|
| TenantId | Required, max 100 chars |
| DocumentType | Required, max 50 chars, alphanumeric |
| Id | Required, max 100 chars |
| Query | Max 500 chars |
| Prefix (autocomplete) | Min 1 char, max 100 chars |
| Limit | 1-100 |

### 7.1 SearchValidationError

```csharp
public enum SearchValidationError
{
    TenantIdRequired,
    TenantIdTooLong,
    DocumentTypeRequired,
    DocumentTypeTooLong,
    IdRequired,
    IdTooLong,
    QueryTooLong,
    PrefixRequired,
    PrefixTooLong,
    LimitOutOfRange,
    InvalidFilterValue,
    UnknownField
}
```

---

## 8) Integration with Domain Services

### 8.1 Event-Driven Indexing Pattern

```csharp
/// <summary>
/// Listens to domain events and updates the search index.
/// </summary>
public sealed class SearchIndexEventHandler
{
    private readonly ISearchIndexer _indexer;
    
    public SearchIndexEventHandler(ISearchIndexer indexer)
    {
        _indexer = indexer;
    }
    
    public async Task OnProfileCreatedAsync(ProfileDto profile, string tenantId)
    {
        var doc = SearchDocumentBuilders.FromProfile(profile, tenantId);
        await _indexer.IndexAsync(doc);
    }
    
    public async Task OnProfileUpdatedAsync(ProfileDto profile, string tenantId)
    {
        var doc = SearchDocumentBuilders.FromProfile(profile, tenantId);
        await _indexer.IndexAsync(doc);
    }
    
    public async Task OnProfileDeletedAsync(string profileId, string tenantId)
    {
        await _indexer.RemoveAsync(tenantId, "Profile", profileId);
    }
    
    public async Task OnPostCreatedAsync(PostDto post, string tenantId)
    {
        var doc = SearchDocumentBuilders.FromPost(post, tenantId);
        await _indexer.IndexAsync(doc);
    }
    
    // ... similar for other entity types
}
```

### 8.2 Typed Search Helpers

```csharp
/// <summary>
/// Typed helpers for common search operations.
/// </summary>
public static class SearchHelpers
{
    /// <summary>
    /// Search for profiles by name or handle.
    /// </summary>
    public static async Task<List<ProfileDto>> SearchProfilesAsync(
        this ISearchService search,
        string tenantId,
        string query,
        int limit = 20,
        CancellationToken ct = default)
    {
        var result = await search.SearchAsync(new SearchRequest
        {
            TenantId = tenantId,
            Query = query,
            DocumentTypes = new() { "Profile" },
            SearchFields = new() { "displayName", "handle" },
            Limit = limit,
            IncludeSource = true
        }, ct);
        
        return result.Hits
            .Select(h => h.Source as ProfileDto)
            .Where(p => p != null)
            .ToList()!;
    }
    
    /// <summary>
    /// Search for posts by content.
    /// </summary>
    public static async Task<List<PostDto>> SearchPostsAsync(
        this ISearchService search,
        string tenantId,
        string query,
        string? authorId = null,
        int limit = 20,
        CancellationToken ct = default)
    {
        var request = new SearchRequest
        {
            TenantId = tenantId,
            Query = query,
            DocumentTypes = new() { "Post" },
            SearchFields = new() { "body" },
            Limit = limit,
            IncludeSource = true
        };
        
        if (authorId != null)
        {
            request.Filters.Add(new SearchFilter
            {
                Field = "authorId",
                Operator = SearchFilterOperator.Equals,
                Value = authorId
            });
        }
        
        var result = await search.SearchAsync(request, ct);
        
        return result.Hits
            .Select(h => h.Source as PostDto)
            .Where(p => p != null)
            .ToList()!;
    }
    
    /// <summary>
    /// Search for hashtags.
    /// </summary>
    public static async Task<List<string>> SearchHashtagsAsync(
        this ISearchService search,
        string tenantId,
        string prefix,
        int limit = 10,
        CancellationToken ct = default)
    {
        var request = new SearchRequest
        {
            TenantId = tenantId,
            DocumentTypes = new() { "Post" },
            Facets = new() { "hashtags" },
            Limit = 0 // We just want facets
        };
        
        // Add prefix filter if provided
        if (!string.IsNullOrEmpty(prefix))
        {
            request.Filters.Add(new SearchFilter
            {
                Field = "hashtags",
                Operator = SearchFilterOperator.StartsWith,
                Value = prefix.ToLowerInvariant()
            });
        }
        
        var result = await search.SearchAsync(request, ct);
        
        return result.Facets
            .GetValueOrDefault("hashtags", new())
            .OrderByDescending(f => f.Count)
            .Take(limit)
            .Select(f => f.Value)
            .ToList();
    }
}
```

---

## 9) DI Registration

```csharp
public static class SearchServiceExtensions
{
    /// <summary>
    /// Add in-memory search index (for development/testing).
    /// </summary>
    public static IServiceCollection AddInMemorySearchIndex(
        this IServiceCollection services,
        Action<SearchIndexOptions>? configure = null)
    {
        var options = new SearchIndexOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        
        services.AddSingleton<InMemorySearchIndex>();
        services.AddSingleton<ISearchService>(sp => sp.GetRequiredService<InMemorySearchIndex>());
        services.AddSingleton<ISearchIndexer>(sp => sp.GetRequiredService<InMemorySearchIndex>());
        services.AddSingleton<ISearchIndex>(sp => sp.GetRequiredService<InMemorySearchIndex>());
        
        return services;
    }
}

// Usage
builder.Services.AddInMemorySearchIndex(options =>
{
    options.MaxResultsPerQuery = 50;
});
```

---

## 10) Implementation Order

| Step | Task | Time |
|------|------|------|
| 1 | Create `Search.Abstractions` with DTOs + interfaces | 0.5 day |
| 2 | Implement `ITextAnalyzer` (SimpleTextAnalyzer) | 0.25 day |
| 3 | Implement `InMemorySearchIndex` (core search logic) | 1 day |
| 4 | Implement filtering and sorting | 0.5 day |
| 5 | Implement autocomplete | 0.25 day |
| 6 | Implement facets | 0.25 day |
| 7 | Add document builders | 0.25 day |
| 8 | Write unit tests | 0.5 day |
| 9 | DI extensions and documentation | 0.25 day |
| **Total** | | **3.75 days** |

---

## 11) Future Extensions

### 11.1 Elasticsearch Backend

```csharp
// Future: Search.Index.Elasticsearch
public sealed class ElasticsearchSearchIndex : ISearchIndex
{
    private readonly ElasticClient _client;
    
    // Map SearchRequest to Elasticsearch query DSL
    // Map results back to SearchResult
}
```

### 11.2 Azure Cognitive Search Backend

```csharp
// Future: Search.Index.AzureCognitiveSearch
public sealed class AzureCognitiveSearchIndex : ISearchIndex
{
    private readonly SearchClient _client;
    
    // Map SearchRequest to Azure Search API
    // Map results back to SearchResult
}
```

### 11.3 Advanced Features (Future)

- Fuzzy matching (typo tolerance)
- Synonym support
- Phrase matching
- Geo search (nearby)
- Personalized ranking
- Query suggestions

---

## 12) Next Steps

1. Create project structure
2. Implement DTOs and interfaces (Abstractions)
3. Implement SimpleTextAnalyzer
4. Implement InMemorySearchIndex
5. Add filtering, sorting, facets, autocomplete
6. Write unit tests
7. Add to demo project with event-driven indexing
