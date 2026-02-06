# Search.Core

Core components for the Search Service including text analysis, validation, and utilities.

## Overview

This project provides:
- Text analyzer for tokenizing and normalizing search terms
- Validation logic for search requests and documents
- Configuration options for search behavior

## Key Components

### SimpleTextAnalyzer

Basic text analyzer that:
- Converts to lowercase
- Removes punctuation
- Splits on whitespace
- Filters by token length

```csharp
var analyzer = new SimpleTextAnalyzer();
var tokens = analyzer.Tokenize("Hello, World!");
// Returns: ["hello", "world"]
```

### SearchIndexOptions

Configuration for search behavior:
- `MaxResultsPerQuery` - Limit on search results (default: 100)
- `MaxAutocompleteSuggestions` - Limit on autocomplete (default: 20)
- `RecencyBoostWeight` - Weight for recency scoring (default: 0.1)
- `HighlightPreTag`/`HighlightPostTag` - HTML tags for highlights

### SearchValidator

Validates:
- Search requests (tenant, query length, limit)
- Autocomplete requests (prefix required)
- Documents for indexing (required fields)

## Dependencies

- `Search.Abstractions` - Interfaces and DTOs
