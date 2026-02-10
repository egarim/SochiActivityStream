using ActivityStream.Abstractions;
using Content.Core;
using Content.Store.InMemory;
using Xunit;

namespace ActivityStream.Tests.Content;

/// <summary>
/// Tests for ContentService post operations.
/// </summary>
public class PostTests
{
    private static ContentService CreateService()
    {
        return new ContentService(
            new InMemoryPostStore(),
            new InMemoryCommentStore(),
            new InMemoryReactionStore(),
            new UlidIdGenerator());
    }

    private static EntityRefDto CreateAuthor(string id = "user1") => new()
    {
        Type = "Profile",
        Id = id,
        DisplayName = $"User {id}"
    };

    #region CreatePost Tests

    [Fact]
    public async Task CreatePost_ValidRequest_ReturnsPostWithId()
    {
        var service = CreateService();

        var post = await service.CreatePostAsync(new CreatePostRequest
        {
            TenantId = "tenant1",
            Author = CreateAuthor(),
            Body = "Hello, world!"
        });

        Assert.NotNull(post.Id);
        Assert.Equal("tenant1", post.TenantId);
        Assert.Equal("Hello, world!", post.Body);
        Assert.Equal(0, post.CommentCount);
        Assert.Empty(post.ReactionCounts);
    }

    [Fact]
    public async Task CreatePost_WithMediaIds_StoresMediaIds()
    {
        var service = CreateService();

        var post = await service.CreatePostAsync(new CreatePostRequest
        {
            TenantId = "tenant1",
            Author = CreateAuthor(),
            Body = "Check out this photo!",
            MediaIds = new List<string> { "media1", "media2" }
        });

        Assert.NotNull(post.MediaIds);
        Assert.Equal(2, post.MediaIds.Count);
        Assert.Contains("media1", post.MediaIds);
    }

    [Fact]
    public async Task CreatePost_WithVisibility_SetsVisibility()
    {
        var service = CreateService();

        var post = await service.CreatePostAsync(new CreatePostRequest
        {
            TenantId = "tenant1",
            Author = CreateAuthor(),
            Body = "Friends only",
            Visibility = ContentVisibility.Friends
        });

        Assert.Equal(ContentVisibility.Friends, post.Visibility);
    }

    [Fact]
    public async Task CreatePost_MissingTenantId_ThrowsValidation()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ContentValidationException>(() =>
            service.CreatePostAsync(new CreatePostRequest
            {
                TenantId = "",
                Author = CreateAuthor(),
                Body = "Test"
            }));
    }

    [Fact]
    public async Task CreatePost_MissingBody_ThrowsValidation()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ContentValidationException>(() =>
            service.CreatePostAsync(new CreatePostRequest
            {
                TenantId = "tenant1",
                Author = CreateAuthor(),
                Body = ""
            }));
    }

    [Fact]
    public async Task CreatePost_BodyTooLong_ThrowsValidation()
    {
        var service = new ContentService(
            new InMemoryPostStore(),
            new InMemoryCommentStore(),
            new InMemoryReactionStore(),
            new UlidIdGenerator(),
            new ContentServiceOptions { MaxPostBodyLength = 100 });

        await Assert.ThrowsAsync<ContentValidationException>(() =>
            service.CreatePostAsync(new CreatePostRequest
            {
                TenantId = "tenant1",
                Author = CreateAuthor(),
                Body = new string('x', 101)
            }));
    }

    #endregion

    #region GetPost Tests

    [Fact]
    public async Task GetPost_ExistingPost_ReturnsPost()
    {
        var service = CreateService();
        var created = await service.CreatePostAsync(new CreatePostRequest
        {
            TenantId = "tenant1",
            Author = CreateAuthor(),
            Body = "Test post"
        });

        var retrieved = await service.GetPostAsync("tenant1", created.Id!);

        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal("Test post", retrieved.Body);
    }

    [Fact]
    public async Task GetPost_NonExistent_ReturnsNull()
    {
        var service = CreateService();

        var result = await service.GetPostAsync("tenant1", "nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPost_WrongTenant_ReturnsNull()
    {
        var service = CreateService();
        var created = await service.CreatePostAsync(new CreatePostRequest
        {
            TenantId = "tenant1",
            Author = CreateAuthor(),
            Body = "Test"
        });

        var result = await service.GetPostAsync("tenant2", created.Id!);

        Assert.Null(result);
    }

    #endregion

    #region UpdatePost Tests

    [Fact]
    public async Task UpdatePost_ValidUpdate_UpdatesBody()
    {
        var service = CreateService();
        var author = CreateAuthor();
        var post = await service.CreatePostAsync(new CreatePostRequest
        {
            TenantId = "tenant1",
            Author = author,
            Body = "Original"
        });

        var updated = await service.UpdatePostAsync(new UpdatePostRequest
        {
            TenantId = "tenant1",
            PostId = post.Id!,
            Actor = author,
            Body = "Updated"
        });

        Assert.Equal("Updated", updated.Body);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task UpdatePost_ByNonAuthor_ThrowsUnauthorized()
    {
        var service = CreateService();
        var post = await service.CreatePostAsync(new CreatePostRequest
        {
            TenantId = "tenant1",
            Author = CreateAuthor("user1"),
            Body = "Original"
        });

        var ex = await Assert.ThrowsAsync<ContentValidationException>(() =>
            service.UpdatePostAsync(new UpdatePostRequest
            {
                TenantId = "tenant1",
                PostId = post.Id!,
                Actor = CreateAuthor("user2"),
                Body = "Hacked!"
            }));

        Assert.Equal(ContentValidationError.PostUnauthorized, ex.Error);
    }

    [Fact]
    public async Task UpdatePost_NonExistent_ThrowsNotFound()
    {
        var service = CreateService();

        var ex = await Assert.ThrowsAsync<ContentValidationException>(() =>
            service.UpdatePostAsync(new UpdatePostRequest
            {
                TenantId = "tenant1",
                PostId = "nonexistent",
                Actor = CreateAuthor(),
                Body = "Test"
            }));

        Assert.Equal(ContentValidationError.PostNotFound, ex.Error);
    }

    #endregion

    #region DeletePost Tests

    [Fact]
    public async Task DeletePost_ByAuthor_SoftDeletes()
    {
        var service = CreateService();
        var author = CreateAuthor();
        var post = await service.CreatePostAsync(new CreatePostRequest
        {
            TenantId = "tenant1",
            Author = author,
            Body = "To delete"
        });

        await service.DeletePostAsync(new DeletePostRequest
        {
            TenantId = "tenant1",
            PostId = post.Id!,
            Actor = author
        });

        // Should still exist but be marked deleted
        var result = await service.QueryPostsAsync(new PostQuery
        {
            TenantId = "tenant1",
            IncludeDeleted = true
        });
        var deleted = result.Items.FirstOrDefault(p => p.Id == post.Id);
        Assert.NotNull(deleted);
        Assert.True(deleted.IsDeleted);
    }

    [Fact]
    public async Task DeletePost_ExcludedByDefault()
    {
        var service = CreateService();
        var author = CreateAuthor();
        var post = await service.CreatePostAsync(new CreatePostRequest
        {
            TenantId = "tenant1",
            Author = author,
            Body = "To delete"
        });

        await service.DeletePostAsync(new DeletePostRequest
        {
            TenantId = "tenant1",
            PostId = post.Id!,
            Actor = author
        });

        var result = await service.QueryPostsAsync(new PostQuery { TenantId = "tenant1" });
        Assert.DoesNotContain(result.Items, p => p.Id == post.Id);
    }

    #endregion

    #region QueryPosts Tests

    [Fact]
    public async Task QueryPosts_ReturnsAllForTenant()
    {
        var service = CreateService();
        for (int i = 0; i < 5; i++)
        {
            await service.CreatePostAsync(new CreatePostRequest
            {
                TenantId = "tenant1",
                Author = CreateAuthor(),
                Body = $"Post {i}"
            });
        }

        var result = await service.QueryPostsAsync(new PostQuery { TenantId = "tenant1" });

        Assert.Equal(5, result.Items.Count);
    }

    [Fact]
    public async Task QueryPosts_FiltersByAuthor()
    {
        var service = CreateService();
        await service.CreatePostAsync(new CreatePostRequest
        {
            TenantId = "tenant1",
            Author = CreateAuthor("user1"),
            Body = "Post 1"
        });
        await service.CreatePostAsync(new CreatePostRequest
        {
            TenantId = "tenant1",
            Author = CreateAuthor("user2"),
            Body = "Post 2"
        });

        var result = await service.QueryPostsAsync(new PostQuery
        {
            TenantId = "tenant1",
            Author = CreateAuthor("user1")
        });

        Assert.Single(result.Items);
        Assert.Equal("user1", result.Items[0].Author.Id);
    }

    [Fact]
    public async Task QueryPosts_Pagination()
    {
        var service = CreateService();
        for (int i = 0; i < 10; i++)
        {
            await service.CreatePostAsync(new CreatePostRequest
            {
                TenantId = "tenant1",
                Author = CreateAuthor(),
                Body = $"Post {i}"
            });
        }

        var page1 = await service.QueryPostsAsync(new PostQuery { TenantId = "tenant1", Limit = 5 });
        var page2 = await service.QueryPostsAsync(new PostQuery { TenantId = "tenant1", Limit = 5, Cursor = page1.NextCursor });

        Assert.Equal(5, page1.Items.Count);
        Assert.True(page1.HasMore);
        Assert.Equal(5, page2.Items.Count);

        var allIds = page1.Items.Select(p => p.Id).Concat(page2.Items.Select(p => p.Id)).ToList();
        Assert.Equal(10, allIds.Distinct().Count());
    }

    #endregion
}
