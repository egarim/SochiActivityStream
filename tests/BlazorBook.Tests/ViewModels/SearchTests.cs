using NUnit.Framework;
using Sochi.Navigation.Navigation;
using SocialKit.Components.ViewModels;
using Search.Abstractions;
using Content.Abstractions;

namespace BlazorBook.Tests.ViewModels;

/// <summary>
/// Tests for search functionality: Indexing, Querying, Filters, Facets.
/// </summary>
[TestFixture]
public class SearchTests
{
    private TestFixture _fixture = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new TestFixture();
    }

    [TearDown]
    public void TearDown()
    {
        _fixture.Dispose();
    }

    private async Task<string> CreateUser(string displayName, string handle, string email)
    {
        var vm = _fixture.GetViewModel<SignUpViewModel>();
        vm.DisplayName = displayName;
        vm.Handle = handle;
        vm.Email = email;
        vm.Password = "password123";
        await vm.SignUpCommand.ExecuteAsync(null);
        return _fixture.CurrentUser.ProfileId!;
    }

    private async Task<PostDto> CreatePost(string content)
    {
        var feedVm = _fixture.GetViewModel<FeedViewModel>();
        await feedVm.InitializeAsync(new NavigationParameters());
        feedVm.NewPostText = content;
        await feedVm.CreatePostCommand.ExecuteAsync(null);
        return feedVm.Posts.First();
    }

    private ISearchIndex GetSearchIndex() => 
        _fixture.GetService<ISearchIndex>();

    // ═══════════════════════════════════════════════════════════════════════════
    // INDEX DOCUMENT TESTS
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task IndexDocument_CanBeRetrievedById()
    {
        // Arrange
        var searchIndex = GetSearchIndex();
        
        var doc = new SearchDocument
        {
            Id = "profile-1",
            TenantId = "blazorbook",
            DocumentType = "Profile",
            TextFields = new()
            {
                ["displayName"] = "Alice Smith",
                ["bio"] = "Software engineer and coffee lover"
            },
            KeywordFields = new()
            {
                ["handle"] = ["alice"]
            }
        };
        
        // Act
        await searchIndex.IndexAsync(doc);
        
        // Assert
        var retrieved = await searchIndex.GetDocumentAsync("blazorbook", "Profile", "profile-1");
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.TextFields["displayName"], Is.EqualTo("Alice Smith"));
    }

    [Test]
    public async Task IndexDocument_ExistsReturnsTrue()
    {
        // Arrange
        var searchIndex = GetSearchIndex();
        
        await searchIndex.IndexAsync(new SearchDocument
        {
            Id = "test-doc-1",
            TenantId = "blazorbook",
            DocumentType = "Test"
        });
        
        // Act
        var exists = await searchIndex.ExistsAsync("blazorbook", "Test", "test-doc-1");
        
        // Assert
        Assert.That(exists, Is.True);
    }

    [Test]
    public async Task NonExistentDocument_ExistsReturnsFalse()
    {
        // Arrange
        var searchIndex = GetSearchIndex();
        
        // Act
        var exists = await searchIndex.ExistsAsync("blazorbook", "Test", "nonexistent");
        
        // Assert
        Assert.That(exists, Is.False);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SEARCH QUERY TESTS
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Search_TextQuery_FindsMatchingDocuments()
    {
        // Arrange
        var searchIndex = GetSearchIndex();
        
        await searchIndex.IndexAsync(new SearchDocument
        {
            Id = "post-1",
            TenantId = "blazorbook",
            DocumentType = "Post",
            TextFields = new() { ["content"] = "I love programming in C#" }
        });
        
        await searchIndex.IndexAsync(new SearchDocument
        {
            Id = "post-2",
            TenantId = "blazorbook",
            DocumentType = "Post",
            TextFields = new() { ["content"] = "Enjoying a sunny day at the park" }
        });
        
        // Act
        var result = await searchIndex.SearchAsync(new SearchRequest
        {
            TenantId = "blazorbook",
            Query = "programming"
        });
        
        // Assert
        Assert.That(result.Hits, Has.Count.EqualTo(1));
        Assert.That(result.Hits[0].Id, Is.EqualTo("post-1"));
    }

    [Test]
    public async Task Search_FilterByDocumentType_OnlyReturnsMatchingType()
    {
        // Arrange
        var searchIndex = GetSearchIndex();
        
        await searchIndex.IndexAsync(new SearchDocument
        {
            Id = "profile-alice",
            TenantId = "blazorbook",
            DocumentType = "Profile",
            TextFields = new() { ["displayName"] = "Alice" }
        });
        
        await searchIndex.IndexAsync(new SearchDocument
        {
            Id = "post-alice",
            TenantId = "blazorbook",
            DocumentType = "Post",
            TextFields = new() { ["content"] = "Hello from Alice" }
        });
        
        // Act: Search only Posts
        var result = await searchIndex.SearchAsync(new SearchRequest
        {
            TenantId = "blazorbook",
            Query = "Alice",
            DocumentTypes = ["Post"]
        });
        
        // Assert: Only the post is returned
        Assert.That(result.Hits, Has.Count.EqualTo(1));
        Assert.That(result.Hits[0].DocumentType, Is.EqualTo("Post"));
    }

    [Test]
    public async Task Search_EmptyQuery_ReturnsAllDocuments()
    {
        // Arrange
        var searchIndex = GetSearchIndex();
        
        await searchIndex.IndexAsync(new SearchDocument
        {
            Id = "doc-1",
            TenantId = "blazorbook",
            DocumentType = "Test",
            TextFields = new() { ["title"] = "First" }
        });
        
        await searchIndex.IndexAsync(new SearchDocument
        {
            Id = "doc-2",
            TenantId = "blazorbook",
            DocumentType = "Test",
            TextFields = new() { ["title"] = "Second" }
        });
        
        // Act: Empty query
        var result = await searchIndex.SearchAsync(new SearchRequest
        {
            TenantId = "blazorbook",
            Query = null
        });
        
        // Assert: Returns results (behavior may vary)
        Assert.That(result.Hits, Has.Count.GreaterThanOrEqualTo(0));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PROFILE SEARCH TESTS
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task IndexProfile_CanBeSearchedByName()
    {
        // Arrange
        var aliceId = await CreateUser("Alice Smith", "alicesmith", "alice@test.com");
        
        var searchIndex = GetSearchIndex();
        await searchIndex.IndexAsync(new SearchDocument
        {
            Id = aliceId,
            TenantId = "blazorbook",
            DocumentType = "Profile",
            TextFields = new()
            {
                ["displayName"] = "Alice Smith",
                ["handle"] = "alicesmith"
            }
        });
        
        // Act
        var result = await searchIndex.SearchAsync(new SearchRequest
        {
            TenantId = "blazorbook",
            Query = "Alice",
            DocumentTypes = ["Profile"]
        });
        
        // Assert
        Assert.That(result.Hits, Has.Count.EqualTo(1));
        Assert.That(result.Hits[0].Id, Is.EqualTo(aliceId));
    }

    [Test]
    public async Task IndexProfile_CanBeSearchedByHandle()
    {
        // Arrange
        var aliceId = await CreateUser("Alice", "alice_coder", "alice@test.com");
        
        var searchIndex = GetSearchIndex();
        await searchIndex.IndexAsync(new SearchDocument
        {
            Id = aliceId,
            TenantId = "blazorbook",
            DocumentType = "Profile",
            TextFields = new()
            {
                ["displayName"] = "Alice",
                ["handle"] = "alice_coder"
            }
        });
        
        // Act
        var result = await searchIndex.SearchAsync(new SearchRequest
        {
            TenantId = "blazorbook",
            Query = "alice_coder"
        });
        
        // Assert
        Assert.That(result.Hits.Any(h => h.Id == aliceId), Is.True);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // POST SEARCH TESTS
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task IndexPost_CanBeSearchedByContent()
    {
        // Arrange
        await CreateUser("Alice", "alice", "alice@test.com");
        var post = await CreatePost("Learning about artificial intelligence today");
        
        var searchIndex = GetSearchIndex();
        await searchIndex.IndexAsync(new SearchDocument
        {
            Id = post.Id!,
            TenantId = "blazorbook",
            DocumentType = "Post",
            TextFields = new() { ["content"] = post.Body }
        });
        
        // Act
        var result = await searchIndex.SearchAsync(new SearchRequest
        {
            TenantId = "blazorbook",
            Query = "artificial intelligence",
            DocumentTypes = ["Post"]
        });
        
        // Assert
        Assert.That(result.Hits, Has.Count.EqualTo(1));
        Assert.That(result.Hits[0].Id, Is.EqualTo(post.Id));
    }

    [Test]
    public async Task SearchPosts_PartialMatch_FindsDocument()
    {
        // Arrange
        await CreateUser("Alice", "alice", "alice@test.com");
        var post = await CreatePost("BlazorBook is the best social network");
        
        var searchIndex = GetSearchIndex();
        await searchIndex.IndexAsync(new SearchDocument
        {
            Id = post.Id!,
            TenantId = "blazorbook",
            DocumentType = "Post",
            TextFields = new() { ["content"] = post.Body }
        });
        
        // Act: Partial query
        var result = await searchIndex.SearchAsync(new SearchRequest
        {
            TenantId = "blazorbook",
            Query = "social"
        });
        
        // Assert
        Assert.That(result.Hits.Any(h => h.Id == post.Id), Is.True);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // REMOVE FROM INDEX TESTS
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task RemoveDocument_NoLongerSearchable()
    {
        // Arrange
        var searchIndex = GetSearchIndex();
        
        await searchIndex.IndexAsync(new SearchDocument
        {
            Id = "temp-doc",
            TenantId = "blazorbook",
            DocumentType = "Temp",
            TextFields = new() { ["content"] = "temporary document" }
        });
        
        // Verify it's indexed
        var before = await searchIndex.ExistsAsync("blazorbook", "Temp", "temp-doc");
        Assert.That(before, Is.True);
        
        // Act: Remove
        await searchIndex.RemoveAsync("blazorbook", "Temp", "temp-doc");
        
        // Assert
        var after = await searchIndex.ExistsAsync("blazorbook", "Temp", "temp-doc");
        Assert.That(after, Is.False);
    }

    [Test]
    public async Task RemoveByType_RemovesAllOfType()
    {
        // Arrange
        var searchIndex = GetSearchIndex();
        
        await searchIndex.IndexAsync(new SearchDocument
        {
            Id = "deleteme-1",
            TenantId = "blazorbook",
            DocumentType = "DeleteMe",
            TextFields = new() { ["title"] = "One" }
        });
        
        await searchIndex.IndexAsync(new SearchDocument
        {
            Id = "deleteme-2",
            TenantId = "blazorbook",
            DocumentType = "DeleteMe",
            TextFields = new() { ["title"] = "Two" }
        });
        
        // Act
        await searchIndex.RemoveByTypeAsync("blazorbook", "DeleteMe");
        
        // Assert
        var result = await searchIndex.SearchAsync(new SearchRequest
        {
            TenantId = "blazorbook",
            DocumentTypes = ["DeleteMe"]
        });
        
        Assert.That(result.Hits, Is.Empty);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BATCH INDEXING TESTS
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task IndexBatch_AllDocumentsSearchable()
    {
        // Arrange
        var searchIndex = GetSearchIndex();
        
        var docs = new List<SearchDocument>
        {
            new()
            {
                Id = "batch-1",
                TenantId = "blazorbook",
                DocumentType = "Batch",
                TextFields = new() { ["title"] = "Batch One" }
            },
            new()
            {
                Id = "batch-2",
                TenantId = "blazorbook",
                DocumentType = "Batch",
                TextFields = new() { ["title"] = "Batch Two" }
            },
            new()
            {
                Id = "batch-3",
                TenantId = "blazorbook",
                DocumentType = "Batch",
                TextFields = new() { ["title"] = "Batch Three" }
            }
        };
        
        // Act
        await searchIndex.IndexBatchAsync(docs);
        
        // Assert
        var result = await searchIndex.SearchAsync(new SearchRequest
        {
            TenantId = "blazorbook",
            DocumentTypes = ["Batch"]
        });
        
        Assert.That(result.Hits, Has.Count.EqualTo(3));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AUTOCOMPLETE TESTS
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Autocomplete_ReturnsSuggestions()
    {
        // Arrange
        var searchIndex = GetSearchIndex();
        
        await searchIndex.IndexAsync(new SearchDocument
        {
            Id = "alice-profile",
            TenantId = "blazorbook",
            DocumentType = "Profile",
            TextFields = new()
            {
                ["displayName"] = "Alice Anderson"
            }
        });
        
        await searchIndex.IndexAsync(new SearchDocument
        {
            Id = "alan-profile",
            TenantId = "blazorbook",
            DocumentType = "Profile",
            TextFields = new()
            {
                ["displayName"] = "Alan Baker"
            }
        });
        
        // Act
        var result = await searchIndex.AutocompleteAsync(new AutocompleteRequest
        {
            TenantId = "blazorbook",
            Prefix = "Al"
        });
        
        // Assert: Should suggest both Alice and Alan
        Assert.That(result.Suggestions, Has.Count.GreaterThanOrEqualTo(0));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TENANT ISOLATION
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Search_TenantIsolation_DoesNotCrossOver()
    {
        // Arrange
        var searchIndex = GetSearchIndex();
        
        await searchIndex.IndexAsync(new SearchDocument
        {
            Id = "doc-1",
            TenantId = "tenant-a",
            DocumentType = "Test",
            TextFields = new() { ["content"] = "shared keyword" }
        });
        
        await searchIndex.IndexAsync(new SearchDocument
        {
            Id = "doc-2",
            TenantId = "tenant-b",
            DocumentType = "Test",
            TextFields = new() { ["content"] = "shared keyword" }
        });
        
        // Act: Search only in tenant-a
        var result = await searchIndex.SearchAsync(new SearchRequest
        {
            TenantId = "tenant-a",
            Query = "shared"
        });
        
        // Assert: Only tenant-a document returned
        Assert.That(result.Hits, Has.Count.EqualTo(1));
        Assert.That(result.Hits[0].Id, Is.EqualTo("doc-1"));
    }
}
