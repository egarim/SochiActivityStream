using Search.Abstractions;
using Search.Index.InMemory;
using Search.Core;

namespace Search.Tests;

/// <summary>
/// Tests for InMemorySearchIndex.
/// </summary>
public class InMemorySearchIndexTests
{
    private readonly InMemorySearchIndex _index;

    public InMemorySearchIndexTests()
    {
        _index = new InMemorySearchIndex();
    }

    private static SearchDocument CreateProfileDocument(
        string id,
        string displayName,
        string handle,
        string? bio = null,
        int followerCount = 0,
        string tenantId = "tenant1")
    {
        return new SearchDocument
        {
            Id = id,
            TenantId = tenantId,
            DocumentType = "Profile",
            TextFields = new Dictionary<string, string>
            {
                ["displayName"] = displayName,
                ["handle"] = handle,
                ["bio"] = bio ?? ""
            },
            KeywordFields = new Dictionary<string, List<string>>
            {
                ["handle_exact"] = new() { handle.ToLowerInvariant() }
            },
            NumericFields = new Dictionary<string, double>
            {
                ["followerCount"] = followerCount
            },
            DateFields = new Dictionary<string, DateTimeOffset>
            {
                ["createdAt"] = DateTimeOffset.UtcNow
            },
            Boost = 1.0
        };
    }

    private static SearchDocument CreatePostDocument(
        string id,
        string body,
        string authorId,
        List<string>? hashtags = null,
        string tenantId = "tenant1")
    {
        return new SearchDocument
        {
            Id = id,
            TenantId = tenantId,
            DocumentType = "Post",
            TextFields = new Dictionary<string, string>
            {
                ["body"] = body
            },
            KeywordFields = new Dictionary<string, List<string>>
            {
                ["authorId"] = new() { authorId },
                ["hashtags"] = hashtags ?? new()
            },
            DateFields = new Dictionary<string, DateTimeOffset>
            {
                ["createdAt"] = DateTimeOffset.UtcNow
            }
        };
    }

    #region Indexing Tests

    [Fact]
    public async Task IndexAsync_adds_document()
    {
        var doc = CreateProfileDocument("user_1", "John Doe", "johndoe");

        await _index.IndexAsync(doc);

        var exists = await _index.ExistsAsync("tenant1", "Profile", "user_1");
        Assert.True(exists);
    }

    [Fact]
    public async Task IndexAsync_updates_existing_document()
    {
        var doc = CreateProfileDocument("user_1", "John Doe", "johndoe");
        await _index.IndexAsync(doc);

        doc.TextFields["displayName"] = "John Smith";
        await _index.IndexAsync(doc);

        var retrieved = await _index.GetDocumentAsync("tenant1", "Profile", "user_1");
        Assert.Equal("John Smith", retrieved!.TextFields["displayName"]);
    }

    [Fact]
    public async Task RemoveAsync_deletes_document()
    {
        var doc = CreateProfileDocument("user_1", "John Doe", "johndoe");
        await _index.IndexAsync(doc);

        await _index.RemoveAsync("tenant1", "Profile", "user_1");

        var exists = await _index.ExistsAsync("tenant1", "Profile", "user_1");
        Assert.False(exists);
    }

    [Fact]
    public async Task RemoveByTypeAsync_deletes_all_documents_of_type()
    {
        await _index.IndexAsync(CreateProfileDocument("user_1", "John", "john"));
        await _index.IndexAsync(CreateProfileDocument("user_2", "Jane", "jane"));
        await _index.IndexAsync(CreatePostDocument("post_1", "Hello world", "user_1"));

        await _index.RemoveByTypeAsync("tenant1", "Profile");

        var stats = await _index.GetStatsAsync("tenant1");
        Assert.Equal(1, stats.DocumentCount);
        Assert.False(stats.CountByType.ContainsKey("Profile"));
        Assert.Equal(1, stats.CountByType["Post"]);
    }

    [Fact]
    public async Task RemoveByTenantAsync_deletes_all_tenant_documents()
    {
        await _index.IndexAsync(CreateProfileDocument("user_1", "John", "john", tenantId: "tenant1"));
        await _index.IndexAsync(CreateProfileDocument("user_2", "Jane", "jane", tenantId: "tenant2"));

        await _index.RemoveByTenantAsync("tenant1");

        var stats1 = await _index.GetStatsAsync("tenant1");
        var stats2 = await _index.GetStatsAsync("tenant2");
        Assert.Equal(0, stats1.DocumentCount);
        Assert.Equal(1, stats2.DocumentCount);
    }

    [Fact]
    public async Task GetStatsAsync_returns_correct_counts()
    {
        await _index.IndexAsync(CreateProfileDocument("user_1", "John", "john"));
        await _index.IndexAsync(CreateProfileDocument("user_2", "Jane", "jane"));
        await _index.IndexAsync(CreatePostDocument("post_1", "Hello", "user_1"));

        var stats = await _index.GetStatsAsync("tenant1");

        Assert.Equal(3, stats.DocumentCount);
        Assert.Equal(2, stats.CountByType["Profile"]);
        Assert.Equal(1, stats.CountByType["Post"]);
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task SearchAsync_finds_documents_by_text()
    {
        await _index.IndexAsync(CreateProfileDocument("user_1", "John Doe", "johndoe", "Software developer"));
        await _index.IndexAsync(CreateProfileDocument("user_2", "Jane Smith", "janesmith", "Designer"));

        var result = await _index.SearchAsync(new SearchRequest
        {
            TenantId = "tenant1",
            Query = "developer"
        });

        Assert.Single(result.Hits);
        Assert.Equal("user_1", result.Hits[0].Id);
    }

    [Fact]
    public async Task SearchAsync_filters_by_document_type()
    {
        await _index.IndexAsync(CreateProfileDocument("user_1", "John", "john"));
        await _index.IndexAsync(CreatePostDocument("post_1", "Hello John", "user_1"));

        var result = await _index.SearchAsync(new SearchRequest
        {
            TenantId = "tenant1",
            Query = "john",
            DocumentTypes = new() { "Profile" }
        });

        Assert.Single(result.Hits);
        Assert.Equal("Profile", result.Hits[0].DocumentType);
    }

    [Fact]
    public async Task SearchAsync_returns_results_sorted_by_score()
    {
        await _index.IndexAsync(CreateProfileDocument("user_1", "John", "john", "Developer"));
        await _index.IndexAsync(CreateProfileDocument("user_2", "John Smith", "johnsmith", "John is a developer named John"));

        var result = await _index.SearchAsync(new SearchRequest
        {
            TenantId = "tenant1",
            Query = "john"
        });

        // user_2 should score higher (more occurrences of "john")
        Assert.Equal("user_2", result.Hits[0].Id);
    }

    [Fact]
    public async Task SearchAsync_respects_document_boost()
    {
        var doc1 = CreateProfileDocument("user_1", "John Developer", "john1");
        doc1.Boost = 1.0;
        await _index.IndexAsync(doc1);

        var doc2 = CreateProfileDocument("user_2", "John Developer", "john2");
        doc2.Boost = 5.0;
        await _index.IndexAsync(doc2);

        var result = await _index.SearchAsync(new SearchRequest
        {
            TenantId = "tenant1",
            Query = "developer"
        });

        Assert.Equal("user_2", result.Hits[0].Id);
    }

    [Fact]
    public async Task SearchAsync_returns_empty_for_no_matches()
    {
        await _index.IndexAsync(CreateProfileDocument("user_1", "John", "john"));

        var result = await _index.SearchAsync(new SearchRequest
        {
            TenantId = "tenant1",
            Query = "xyz123nonexistent"
        });

        Assert.Empty(result.Hits);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task SearchAsync_without_query_returns_all_matching_documents()
    {
        await _index.IndexAsync(CreateProfileDocument("user_1", "John", "john"));
        await _index.IndexAsync(CreateProfileDocument("user_2", "Jane", "jane"));

        var result = await _index.SearchAsync(new SearchRequest
        {
            TenantId = "tenant1",
            DocumentTypes = new() { "Profile" }
        });

        Assert.Equal(2, result.Hits.Count);
    }

    [Fact]
    public async Task SearchAsync_respects_limit()
    {
        for (int i = 0; i < 10; i++)
        {
            await _index.IndexAsync(CreateProfileDocument($"user_{i}", $"User {i}", $"user{i}"));
        }

        var result = await _index.SearchAsync(new SearchRequest
        {
            TenantId = "tenant1",
            Limit = 5
        });

        Assert.Equal(5, result.Hits.Count);
        Assert.True(result.HasMore);
        Assert.NotNull(result.NextCursor);
    }

    [Fact]
    public async Task SearchAsync_supports_cursor_pagination()
    {
        for (int i = 0; i < 10; i++)
        {
            await _index.IndexAsync(CreateProfileDocument($"user_{i:D2}", $"User {i}", $"user{i}"));
        }

        var page1 = await _index.SearchAsync(new SearchRequest
        {
            TenantId = "tenant1",
            Limit = 5
        });

        var page2 = await _index.SearchAsync(new SearchRequest
        {
            TenantId = "tenant1",
            Limit = 5,
            Cursor = page1.NextCursor
        });

        Assert.Equal(5, page1.Hits.Count);
        Assert.Equal(5, page2.Hits.Count);

        var allIds = page1.Hits.Select(h => h.Id)
            .Concat(page2.Hits.Select(h => h.Id))
            .ToList();
        Assert.Equal(10, allIds.Distinct().Count());
    }

    [Fact]
    public async Task SearchAsync_includes_source_when_requested()
    {
        var doc = CreateProfileDocument("user_1", "John", "john");
        doc.SourceEntity = new { Name = "John", Email = "john@example.com" };
        await _index.IndexAsync(doc);

        var result = await _index.SearchAsync(new SearchRequest
        {
            TenantId = "tenant1",
            IncludeSource = true
        });

        Assert.NotNull(result.Hits[0].Source);
    }

    [Fact]
    public async Task SearchAsync_excludes_source_when_not_requested()
    {
        var doc = CreateProfileDocument("user_1", "John", "john");
        doc.SourceEntity = new { Name = "John" };
        await _index.IndexAsync(doc);

        var result = await _index.SearchAsync(new SearchRequest
        {
            TenantId = "tenant1",
            IncludeSource = false
        });

        Assert.Null(result.Hits[0].Source);
    }

    [Fact]
    public async Task SearchAsync_generates_highlights()
    {
        await _index.IndexAsync(CreateProfileDocument("user_1", "John Doe", "johndoe", "Software developer from NYC"));

        var result = await _index.SearchAsync(new SearchRequest
        {
            TenantId = "tenant1",
            Query = "developer",
            Highlight = true
        });

        Assert.NotNull(result.Hits[0].Highlights);
        Assert.Contains("<mark>developer</mark>", result.Hits[0].Highlights!["bio"]);
    }

    #endregion

    #region Filter Tests

    [Fact]
    public async Task SearchAsync_filters_by_equals()
    {
        await _index.IndexAsync(CreatePostDocument("post_1", "Hello", "user_1"));
        await _index.IndexAsync(CreatePostDocument("post_2", "World", "user_2"));

        var result = await _index.SearchAsync(new SearchRequest
        {
            TenantId = "tenant1",
            Filters = new()
            {
                new SearchFilter { Field = "authorId", Operator = SearchFilterOperator.Equals, Value = "user_1" }
            }
        });

        Assert.Single(result.Hits);
        Assert.Equal("post_1", result.Hits[0].Id);
    }

    [Fact]
    public async Task SearchAsync_filters_by_in()
    {
        await _index.IndexAsync(CreatePostDocument("post_1", "Hello", "user_1"));
        await _index.IndexAsync(CreatePostDocument("post_2", "World", "user_2"));
        await _index.IndexAsync(CreatePostDocument("post_3", "Test", "user_3"));

        var result = await _index.SearchAsync(new SearchRequest
        {
            TenantId = "tenant1",
            Filters = new()
            {
                new SearchFilter { Field = "authorId", Operator = SearchFilterOperator.In, Value = new[] { "user_1", "user_2" } }
            }
        });

        Assert.Equal(2, result.Hits.Count);
    }

    [Fact]
    public async Task SearchAsync_filters_by_numeric_range()
    {
        await _index.IndexAsync(CreateProfileDocument("user_1", "John", "john", followerCount: 100));
        await _index.IndexAsync(CreateProfileDocument("user_2", "Jane", "jane", followerCount: 500));
        await _index.IndexAsync(CreateProfileDocument("user_3", "Bob", "bob", followerCount: 1000));

        var result = await _index.SearchAsync(new SearchRequest
        {
            TenantId = "tenant1",
            Filters = new()
            {
                new SearchFilter { Field = "followerCount", Operator = SearchFilterOperator.GreaterThan, Value = 200 }
            }
        });

        Assert.Equal(2, result.Hits.Count);
    }

    [Fact]
    public async Task SearchAsync_filters_by_starts_with()
    {
        await _index.IndexAsync(CreateProfileDocument("user_1", "John Doe", "johndoe"));
        await _index.IndexAsync(CreateProfileDocument("user_2", "Jane Smith", "janesmith"));

        var result = await _index.SearchAsync(new SearchRequest
        {
            TenantId = "tenant1",
            Filters = new()
            {
                new SearchFilter { Field = "handle_exact", Operator = SearchFilterOperator.StartsWith, Value = "john" }
            }
        });

        Assert.Single(result.Hits);
        Assert.Equal("user_1", result.Hits[0].Id);
    }

    [Fact]
    public async Task SearchAsync_filters_by_exists()
    {
        var doc1 = CreateProfileDocument("user_1", "John", "john");
        doc1.NumericFields["score"] = 100;
        await _index.IndexAsync(doc1);

        var doc2 = CreateProfileDocument("user_2", "Jane", "jane");
        // No score field
        await _index.IndexAsync(doc2);

        var result = await _index.SearchAsync(new SearchRequest
        {
            TenantId = "tenant1",
            Filters = new()
            {
                new SearchFilter { Field = "score", Operator = SearchFilterOperator.Exists, Value = true }
            }
        });

        Assert.Single(result.Hits);
        Assert.Equal("user_1", result.Hits[0].Id);
    }

    #endregion

    #region Facet Tests

    [Fact]
    public async Task SearchAsync_computes_facets()
    {
        await _index.IndexAsync(CreatePostDocument("post_1", "Hello", "user_1", new() { "tech", "coding" }));
        await _index.IndexAsync(CreatePostDocument("post_2", "World", "user_1", new() { "tech" }));
        await _index.IndexAsync(CreatePostDocument("post_3", "Test", "user_2", new() { "music" }));

        var result = await _index.SearchAsync(new SearchRequest
        {
            TenantId = "tenant1",
            Facets = new() { "hashtags" }
        });

        Assert.True(result.Facets.ContainsKey("hashtags"));
        var techFacet = result.Facets["hashtags"].First(f => f.Value == "tech");
        Assert.Equal(2, techFacet.Count);
    }

    #endregion

    #region Autocomplete Tests

    [Fact]
    public async Task AutocompleteAsync_returns_matching_suggestions()
    {
        await _index.IndexAsync(CreateProfileDocument("user_1", "John Doe", "johndoe"));
        await _index.IndexAsync(CreateProfileDocument("user_2", "John Smith", "johnsmith"));
        await _index.IndexAsync(CreateProfileDocument("user_3", "Jane Doe", "janedoe"));

        var result = await _index.AutocompleteAsync(new AutocompleteRequest
        {
            TenantId = "tenant1",
            Prefix = "john",
            Fields = new() { "displayName" }
        });

        Assert.Equal(2, result.Suggestions.Count);
        Assert.All(result.Suggestions, s => Assert.Contains("John", s.Text));
    }

    [Fact]
    public async Task AutocompleteAsync_prioritizes_prefix_matches()
    {
        await _index.IndexAsync(CreateProfileDocument("user_1", "Some John", "somejohn"));
        await _index.IndexAsync(CreateProfileDocument("user_2", "John Doe", "johndoe"));

        var result = await _index.AutocompleteAsync(new AutocompleteRequest
        {
            TenantId = "tenant1",
            Prefix = "john",
            Fields = new() { "displayName" }
        });

        // "John Doe" should come first as it starts with "john"
        Assert.Equal("John Doe", result.Suggestions[0].Text);
    }

    [Fact]
    public async Task AutocompleteAsync_highlights_matches()
    {
        await _index.IndexAsync(CreateProfileDocument("user_1", "John Doe", "johndoe"));

        var result = await _index.AutocompleteAsync(new AutocompleteRequest
        {
            TenantId = "tenant1",
            Prefix = "john",
            Fields = new() { "displayName" }
        });

        Assert.Contains("<mark>John</mark>", result.Suggestions[0].Highlighted);
    }

    [Fact]
    public async Task AutocompleteAsync_respects_limit()
    {
        for (int i = 0; i < 10; i++)
        {
            await _index.IndexAsync(CreateProfileDocument($"user_{i}", $"John {i}", $"john{i}"));
        }

        var result = await _index.AutocompleteAsync(new AutocompleteRequest
        {
            TenantId = "tenant1",
            Prefix = "john",
            Limit = 3
        });

        Assert.Equal(3, result.Suggestions.Count);
    }

    [Fact]
    public async Task AutocompleteAsync_filters_by_document_type()
    {
        await _index.IndexAsync(CreateProfileDocument("user_1", "John Doe", "john"));
        await _index.IndexAsync(CreatePostDocument("post_1", "Hello John", "author"));

        var result = await _index.AutocompleteAsync(new AutocompleteRequest
        {
            TenantId = "tenant1",
            Prefix = "john",
            DocumentTypes = new() { "Profile" },
            Fields = new() { "displayName", "body" }
        });

        Assert.Single(result.Suggestions);
        Assert.Equal("Profile", result.Suggestions[0].DocumentType);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task SearchAsync_throws_for_missing_tenant()
    {
        await Assert.ThrowsAsync<SearchValidationException>(() =>
            _index.SearchAsync(new SearchRequest { TenantId = "" }));
    }

    [Fact]
    public async Task AutocompleteAsync_throws_for_missing_prefix()
    {
        await Assert.ThrowsAsync<SearchValidationException>(() =>
            _index.AutocompleteAsync(new AutocompleteRequest { TenantId = "tenant1", Prefix = "" }));
    }

    [Fact]
    public async Task IndexAsync_throws_for_missing_id()
    {
        var doc = new SearchDocument
        {
            Id = "",
            TenantId = "tenant1",
            DocumentType = "Profile"
        };

        await Assert.ThrowsAsync<SearchValidationException>(() =>
            _index.IndexAsync(doc));
    }

    #endregion

    #region Multi-tenancy Tests

    [Fact]
    public async Task SearchAsync_isolates_by_tenant()
    {
        await _index.IndexAsync(CreateProfileDocument("user_1", "John", "john", tenantId: "tenant1"));
        await _index.IndexAsync(CreateProfileDocument("user_2", "John", "john2", tenantId: "tenant2"));

        var result = await _index.SearchAsync(new SearchRequest
        {
            TenantId = "tenant1",
            Query = "john"
        });

        Assert.Single(result.Hits);
        Assert.Equal("user_1", result.Hits[0].Id);
    }

    [Fact]
    public async Task AutocompleteAsync_isolates_by_tenant()
    {
        await _index.IndexAsync(CreateProfileDocument("user_1", "John", "john", tenantId: "tenant1"));
        await _index.IndexAsync(CreateProfileDocument("user_2", "John", "john2", tenantId: "tenant2"));

        var result = await _index.AutocompleteAsync(new AutocompleteRequest
        {
            TenantId = "tenant1",
            Prefix = "john",
            Fields = new() { "displayName" }
        });

        Assert.Single(result.Suggestions);
    }

    #endregion
}
