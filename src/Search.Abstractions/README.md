# Search.Abstractions

Pure contract definitions for the Search Service - DTOs, interfaces, and validation types.

## Overview

This library contains no implementation dependencies, only contracts that define
how search functionality works across the application.

## Key Types

### DTOs
- **SearchDocument** - Document to be indexed with text, keyword, numeric, and date fields
- **SearchRequest** - Query parameters including filters, sorting, and pagination
- **SearchResult** - Result container with hits, facets, and pagination cursor
- **AutocompleteRequest/Result** - Typeahead suggestion types

### Interfaces
- **ISearchService** - Query interface for searching and autocomplete
- **ISearchIndexer** - Index management (add/update/remove documents)
- **ISearchIndex** - Combined interface for implementations
- **ITextAnalyzer** - Text tokenization and normalization

## Field Types

| Type | Description | Use Case |
|------|-------------|----------|
| TextFields | Full-text analyzed | Body, description |
| KeywordFields | Exact match only | Status, tags |
| NumericFields | Numbers | Counts, scores |
| DateFields | DateTimeOffset | Timestamps |

## Filter Operators

- `Equals`, `NotEquals` - Exact match
- `In`, `NotIn` - Multiple values
- `GreaterThan`, `LessThan`, `Between` - Range queries
- `StartsWith` - Prefix match
- `Exists` - Field existence check

## Dependencies

None - pure contracts only.
