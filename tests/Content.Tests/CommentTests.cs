using Content.Abstractions;
using Content.Core;
using Content.Store.InMemory;
using Xunit;

namespace Content.Tests;

/// <summary>
/// Tests for ContentService comment operations.
/// </summary>
public class CommentTests
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

    private static async Task<PostDto> CreateTestPost(ContentService service, string tenantId = "tenant1")
    {
        return await service.CreatePostAsync(new CreatePostRequest
        {
            TenantId = tenantId,
            Author = CreateAuthor(),
            Body = "Test post"
        });
    }

    #region CreateComment Tests

    [Fact]
    public async Task CreateComment_ValidRequest_ReturnsCommentWithId()
    {
        var service = CreateService();
        var post = await CreateTestPost(service);

        var comment = await service.CreateCommentAsync(new CreateCommentRequest
        {
            TenantId = "tenant1",
            Author = CreateAuthor("user2"),
            PostId = post.Id!,
            Body = "Nice post!"
        });

        Assert.NotNull(comment.Id);
        Assert.Equal(post.Id, comment.PostId);
        Assert.Equal("Nice post!", comment.Body);
        Assert.Null(comment.ParentCommentId);
    }

    [Fact]
    public async Task CreateComment_IncrementsPostCommentCount()
    {
        var service = CreateService();
        var post = await CreateTestPost(service);

        await service.CreateCommentAsync(new CreateCommentRequest
        {
            TenantId = "tenant1",
            Author = CreateAuthor("user2"),
            PostId = post.Id!,
            Body = "Comment 1"
        });

        await service.CreateCommentAsync(new CreateCommentRequest
        {
            TenantId = "tenant1",
            Author = CreateAuthor("user3"),
            PostId = post.Id!,
            Body = "Comment 2"
        });

        var updated = await service.GetPostAsync("tenant1", post.Id!);
        Assert.Equal(2, updated!.CommentCount);
    }

    [Fact]
    public async Task CreateComment_OnNonExistentPost_ThrowsNotFound()
    {
        var service = CreateService();

        var ex = await Assert.ThrowsAsync<ContentValidationException>(() =>
            service.CreateCommentAsync(new CreateCommentRequest
            {
                TenantId = "tenant1",
                Author = CreateAuthor(),
                PostId = "nonexistent",
                Body = "Comment"
            }));

        Assert.Equal(ContentValidationError.PostNotFound, ex.Error);
    }

    [Fact]
    public async Task CreateComment_WithParent_CreatesNestedComment()
    {
        var service = CreateService();
        var post = await CreateTestPost(service);
        var parent = await service.CreateCommentAsync(new CreateCommentRequest
        {
            TenantId = "tenant1",
            Author = CreateAuthor(),
            PostId = post.Id!,
            Body = "Parent comment"
        });

        var reply = await service.CreateCommentAsync(new CreateCommentRequest
        {
            TenantId = "tenant1",
            Author = CreateAuthor("user2"),
            PostId = post.Id!,
            ParentCommentId = parent.Id,
            Body = "Reply to parent"
        });

        Assert.Equal(parent.Id, reply.ParentCommentId);
    }

    [Fact]
    public async Task CreateComment_WithParent_IncrementsReplyCount()
    {
        var service = CreateService();
        var post = await CreateTestPost(service);
        var parent = await service.CreateCommentAsync(new CreateCommentRequest
        {
            TenantId = "tenant1",
            Author = CreateAuthor(),
            PostId = post.Id!,
            Body = "Parent comment"
        });

        await service.CreateCommentAsync(new CreateCommentRequest
        {
            TenantId = "tenant1",
            Author = CreateAuthor("user2"),
            PostId = post.Id!,
            ParentCommentId = parent.Id,
            Body = "Reply"
        });

        var updatedParent = await service.GetCommentAsync("tenant1", parent.Id!);
        Assert.Equal(1, updatedParent!.ReplyCount);
    }

    [Fact]
    public async Task CreateComment_ParentOnDifferentPost_ThrowsError()
    {
        var service = CreateService();
        var post1 = await CreateTestPost(service);
        var post2 = await CreateTestPost(service);
        var parent = await service.CreateCommentAsync(new CreateCommentRequest
        {
            TenantId = "tenant1",
            Author = CreateAuthor(),
            PostId = post1.Id!,
            Body = "Parent on post1"
        });

        var ex = await Assert.ThrowsAsync<ContentValidationException>(() =>
            service.CreateCommentAsync(new CreateCommentRequest
            {
                TenantId = "tenant1",
                Author = CreateAuthor(),
                PostId = post2.Id!,
                ParentCommentId = parent.Id,
                Body = "Wrong post"
            }));

        Assert.Equal(ContentValidationError.ParentCommentWrongPost, ex.Error);
    }

    #endregion

    #region QueryComments Tests

    [Fact]
    public async Task QueryComments_ReturnsTopLevelOnly()
    {
        var service = CreateService();
        var post = await CreateTestPost(service);
        var parent = await service.CreateCommentAsync(new CreateCommentRequest
        {
            TenantId = "tenant1",
            Author = CreateAuthor(),
            PostId = post.Id!,
            Body = "Parent"
        });
        await service.CreateCommentAsync(new CreateCommentRequest
        {
            TenantId = "tenant1",
            Author = CreateAuthor("user2"),
            PostId = post.Id!,
            ParentCommentId = parent.Id,
            Body = "Reply"
        });

        var result = await service.QueryCommentsAsync(new CommentQuery
        {
            TenantId = "tenant1",
            PostId = post.Id!
        });

        Assert.Single(result.Items);
        Assert.Equal("Parent", result.Items[0].Body);
    }

    [Fact]
    public async Task QueryComments_WithParentId_ReturnsReplies()
    {
        var service = CreateService();
        var post = await CreateTestPost(service);
        var parent = await service.CreateCommentAsync(new CreateCommentRequest
        {
            TenantId = "tenant1",
            Author = CreateAuthor(),
            PostId = post.Id!,
            Body = "Parent"
        });
        await service.CreateCommentAsync(new CreateCommentRequest
        {
            TenantId = "tenant1",
            Author = CreateAuthor("user2"),
            PostId = post.Id!,
            ParentCommentId = parent.Id,
            Body = "Reply 1"
        });
        await service.CreateCommentAsync(new CreateCommentRequest
        {
            TenantId = "tenant1",
            Author = CreateAuthor("user3"),
            PostId = post.Id!,
            ParentCommentId = parent.Id,
            Body = "Reply 2"
        });

        var result = await service.QueryCommentsAsync(new CommentQuery
        {
            TenantId = "tenant1",
            PostId = post.Id!,
            ParentCommentId = parent.Id
        });

        Assert.Equal(2, result.Items.Count);
    }

    #endregion

    #region UpdateComment Tests

    [Fact]
    public async Task UpdateComment_ByAuthor_UpdatesBody()
    {
        var service = CreateService();
        var post = await CreateTestPost(service);
        var author = CreateAuthor();
        var comment = await service.CreateCommentAsync(new CreateCommentRequest
        {
            TenantId = "tenant1",
            Author = author,
            PostId = post.Id!,
            Body = "Original"
        });

        var updated = await service.UpdateCommentAsync(new UpdateCommentRequest
        {
            TenantId = "tenant1",
            CommentId = comment.Id!,
            Actor = author,
            Body = "Updated"
        });

        Assert.Equal("Updated", updated.Body);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task UpdateComment_ByNonAuthor_ThrowsUnauthorized()
    {
        var service = CreateService();
        var post = await CreateTestPost(service);
        var comment = await service.CreateCommentAsync(new CreateCommentRequest
        {
            TenantId = "tenant1",
            Author = CreateAuthor("user1"),
            PostId = post.Id!,
            Body = "Original"
        });

        var ex = await Assert.ThrowsAsync<ContentValidationException>(() =>
            service.UpdateCommentAsync(new UpdateCommentRequest
            {
                TenantId = "tenant1",
                CommentId = comment.Id!,
                Actor = CreateAuthor("user2"),
                Body = "Hacked!"
            }));

        Assert.Equal(ContentValidationError.CommentUnauthorized, ex.Error);
    }

    #endregion

    #region DeleteComment Tests

    [Fact]
    public async Task DeleteComment_DecrementsPostCommentCount()
    {
        var service = CreateService();
        var post = await CreateTestPost(service);
        var author = CreateAuthor();
        var comment = await service.CreateCommentAsync(new CreateCommentRequest
        {
            TenantId = "tenant1",
            Author = author,
            PostId = post.Id!,
            Body = "To delete"
        });

        var postBefore = await service.GetPostAsync("tenant1", post.Id!);
        Assert.Equal(1, postBefore!.CommentCount);

        await service.DeleteCommentAsync(new DeleteCommentRequest
        {
            TenantId = "tenant1",
            CommentId = comment.Id!,
            Actor = author
        });

        var postAfter = await service.GetPostAsync("tenant1", post.Id!);
        Assert.Equal(0, postAfter!.CommentCount);
    }

    [Fact]
    public async Task DeleteComment_WithReplies_DecrementsParentReplyCount()
    {
        var service = CreateService();
        var post = await CreateTestPost(service);
        var parent = await service.CreateCommentAsync(new CreateCommentRequest
        {
            TenantId = "tenant1",
            Author = CreateAuthor(),
            PostId = post.Id!,
            Body = "Parent"
        });
        var replyAuthor = CreateAuthor("user2");
        var reply = await service.CreateCommentAsync(new CreateCommentRequest
        {
            TenantId = "tenant1",
            Author = replyAuthor,
            PostId = post.Id!,
            ParentCommentId = parent.Id,
            Body = "Reply"
        });

        await service.DeleteCommentAsync(new DeleteCommentRequest
        {
            TenantId = "tenant1",
            CommentId = reply.Id!,
            Actor = replyAuthor
        });

        var updatedParent = await service.GetCommentAsync("tenant1", parent.Id!);
        Assert.Equal(0, updatedParent!.ReplyCount);
    }

    #endregion
}
