# Search.Index.InMemory

In-memory implementation of the Search Service for development and testing.

## Overview

Uses a simple inverted index with TF-IDF-like scoring. Supports:
- Full-text search across text fields
- Filtering by keyword, numeric, and date fields
- Sorting by score, numeric, date, or text fields
- Cursor-based pagination
- Facet counting
- Autocomplete/typeahead
- Hit highlighting

## Usage

```csharp
// Create index
var options = new SearchIndexOptions
{
    MaxResultsPerQuery = 50,
    RecencyBoostWeight = 0.1
};
var index = new InMemorySearchIndex(options);

// Index a document
await index.IndexAsync(new SearchDocument
{
    Id = "user_123",
    TenantId = "acme",
    DocumentType = "Profile",
    TextFields = new Dictionary<string, string>
    {
        ["displayName"] = "John Doe",
        ["handle"] = "johndoe",
        ["bio"] = "Software developer from NYC"
    },
    KeywordFields = new Dictionary<string, List<string>>
    {
        ["handle_exact"] = new() { "johndoe" }
    },
    NumericFields = new Dictionary<string, double>
    {
        ["followerCount"] = 1500
    },
    DateFields = new Dictionary<string, DateTimeOffset>
    {
        ["createdAt"] = DateTimeOffset.UtcNow
    },
    Boost = 1.2
});

// Search
var result = await index.SearchAsync(new SearchRequest
{
    TenantId = "acme",
    Query = "john developer",
    DocumentTypes = new() { "Profile" },
    Limit = 20,
    Highlight = true
});

// Autocomplete
var suggestions = await index.AutocompleteAsync(new AutocompleteRequest
{
    TenantId = "acme",
    Prefix = "joh",
    DocumentTypes = new() { "Profile" },
    Fields = new() { "displayName", "handle" }
});
```

## Scoring

Documents are scored using:
1. **Term Frequency (TF)** - Log-dampened count of query terms found
2. **Document Boost** - Custom boost factor per document
3. **Recency Boost** - Optional boost for newer documents

## Filtering

Supports operators:
- `Equals`, `NotEquals` - Exact match on keyword fields
- `In`, `NotIn` - Match any of multiple values
- `GreaterThan`, `LessThan`, `Between` - Range queries for numeric/date
- `StartsWith` - Prefix matching
- `Exists` - Check field presence

## Limitations

- Not optimized for large datasets (>100K documents)
- No fuzzy matching or typo tolerance
- No stemming or synonyms
- Single-node only (no distributed search)

For production use, consider Elasticsearch or Azure Cognitive Search backends.

## Dependencies

- `Search.Abstractions` - Interfaces and DTOs
- `Search.Core` - Text analyzer and validation
