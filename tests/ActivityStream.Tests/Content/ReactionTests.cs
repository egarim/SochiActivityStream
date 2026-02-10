using ActivityStream.Abstractions;
using Content.Core;
using Content.Store.InMemory;
using Xunit;

namespace ActivityStream.Tests.Content;

/// <summary>
/// Tests for ContentService reaction operations.
/// </summary>
public class ReactionTests
{
    private static ContentService CreateService()
    {
        return new ContentService(
            new InMemoryPostStore(),
            new InMemoryCommentStore(),
            new InMemoryReactionStore(),
            new UlidIdGenerator());
    }

    private static EntityRefDto CreateActor(string id = "user1") => new()
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
            Author = CreateActor(),
            Body = "Test post"
        });
    }

    #region React Tests

    [Fact]
    public async Task React_NewReaction_ReturnsReaction()
    {
        var service = CreateService();
        var post = await CreateTestPost(service);

        var reaction = await service.ReactAsync(new ReactRequest
        {
            TenantId = "tenant1",
            Actor = CreateActor("user2"),
            TargetId = post.Id!,
            TargetKind = ReactionTargetKind.Post,
            Type = ReactionType.Like
        });

        Assert.NotNull(reaction.Id);
        Assert.Equal(ReactionType.Like, reaction.Type);
    }

    [Fact]
    public async Task React_IncrementsReactionCount()
    {
        var service = CreateService();
        var post = await CreateTestPost(service);

        await service.ReactAsync(new ReactRequest
        {
            TenantId = "tenant1",
            Actor = CreateActor("user2"),
            TargetId = post.Id!,
            TargetKind = ReactionTargetKind.Post,
            Type = ReactionType.Like
        });

        await service.ReactAsync(new ReactRequest
        {
            TenantId = "tenant1",
            Actor = CreateActor("user3"),
            TargetId = post.Id!,
            TargetKind = ReactionTargetKind.Post,
            Type = ReactionType.Like
        });

        var updated = await service.GetPostAsync("tenant1", post.Id!);
        Assert.Equal(2, updated!.ReactionCounts[ReactionType.Like]);
    }

    [Fact]
    public async Task React_ChangeReaction_UpdatesCounts()
    {
        var service = CreateService();
        var post = await CreateTestPost(service);
        var actor = CreateActor("user2");

        await service.ReactAsync(new ReactRequest
        {
            TenantId = "tenant1",
            Actor = actor,
            TargetId = post.Id!,
            TargetKind = ReactionTargetKind.Post,
            Type = ReactionType.Like
        });

        await service.ReactAsync(new ReactRequest
        {
            TenantId = "tenant1",
            Actor = actor,
            TargetId = post.Id!,
            TargetKind = ReactionTargetKind.Post,
            Type = ReactionType.Love
        });

        var updated = await service.GetPostAsync("tenant1", post.Id!);
        Assert.False(updated!.ReactionCounts.ContainsKey(ReactionType.Like));
        Assert.Equal(1, updated.ReactionCounts[ReactionType.Love]);
    }

    [Fact]
    public async Task React_SameReactionTwice_NoChange()
    {
        var service = CreateService();
        var post = await CreateTestPost(service);
        var actor = CreateActor("user2");

        await service.ReactAsync(new ReactRequest
        {
            TenantId = "tenant1",
            Actor = actor,
            TargetId = post.Id!,
            TargetKind = ReactionTargetKind.Post,
            Type = ReactionType.Like
        });

        await service.ReactAsync(new ReactRequest
        {
            TenantId = "tenant1",
            Actor = actor,
            TargetId = post.Id!,
            TargetKind = ReactionTargetKind.Post,
            Type = ReactionType.Like
        });

        var updated = await service.GetPostAsync("tenant1", post.Id!);
        Assert.Equal(1, updated!.ReactionCounts[ReactionType.Like]);
    }

    [Fact]
    public async Task React_OnNonExistentTarget_ThrowsNotFound()
    {
        var service = CreateService();

        var ex = await Assert.ThrowsAsync<ContentValidationException>(() =>
            service.ReactAsync(new ReactRequest
            {
                TenantId = "tenant1",
                Actor = CreateActor(),
                TargetId = "nonexistent",
                TargetKind = ReactionTargetKind.Post,
                Type = ReactionType.Like
            }));

        Assert.Equal(ContentValidationError.TargetNotFound, ex.Error);
    }

    [Fact]
    public async Task React_OnComment_Works()
    {
        var service = CreateService();
        var post = await CreateTestPost(service);
        var comment = await service.CreateCommentAsync(new CreateCommentRequest
        {
            TenantId = "tenant1",
            Author = CreateActor(),
            PostId = post.Id!,
            Body = "Comment"
        });

        await service.ReactAsync(new ReactRequest
        {
            TenantId = "tenant1",
            Actor = CreateActor("user2"),
            TargetId = comment.Id!,
            TargetKind = ReactionTargetKind.Comment,
            Type = ReactionType.Haha
        });

        var updated = await service.GetCommentAsync("tenant1", comment.Id!);
        Assert.Equal(1, updated!.ReactionCounts[ReactionType.Haha]);
    }

    #endregion

    #region RemoveReaction Tests

    [Fact]
    public async Task RemoveReaction_DecrementsCount()
    {
        var service = CreateService();
        var post = await CreateTestPost(service);
        var actor = CreateActor("user2");

        await service.ReactAsync(new ReactRequest
        {
            TenantId = "tenant1",
            Actor = actor,
            TargetId = post.Id!,
            TargetKind = ReactionTargetKind.Post,
            Type = ReactionType.Like
        });

        await service.RemoveReactionAsync(new RemoveReactionRequest
        {
            TenantId = "tenant1",
            Actor = actor,
            TargetId = post.Id!,
            TargetKind = ReactionTargetKind.Post
        });

        var updated = await service.GetPostAsync("tenant1", post.Id!);
        Assert.False(updated!.ReactionCounts.ContainsKey(ReactionType.Like));
    }

    [Fact]
    public async Task RemoveReaction_NonExistent_NoOp()
    {
        var service = CreateService();
        var post = await CreateTestPost(service);

        // Should not throw
        await service.RemoveReactionAsync(new RemoveReactionRequest
        {
            TenantId = "tenant1",
            Actor = CreateActor("user2"),
            TargetId = post.Id!,
            TargetKind = ReactionTargetKind.Post
        });
    }

    #endregion

    #region ViewerReaction Tests

    [Fact]
    public async Task GetPost_WithViewer_PopulatesViewerReaction()
    {
        var service = CreateService();
        var post = await CreateTestPost(service);
        var viewer = CreateActor("user2");

        await service.ReactAsync(new ReactRequest
        {
            TenantId = "tenant1",
            Actor = viewer,
            TargetId = post.Id!,
            TargetKind = ReactionTargetKind.Post,
            Type = ReactionType.Love
        });

        var retrieved = await service.GetPostAsync("tenant1", post.Id!, viewer);

        Assert.Equal(ReactionType.Love, retrieved!.ViewerReaction);
    }

    [Fact]
    public async Task GetPost_WithoutReaction_ViewerReactionIsNull()
    {
        var service = CreateService();
        var post = await CreateTestPost(service);

        var retrieved = await service.GetPostAsync("tenant1", post.Id!, CreateActor("user2"));

        Assert.Null(retrieved!.ViewerReaction);
    }

    [Fact]
    public async Task QueryPosts_WithViewer_PopulatesViewerReactions()
    {
        var service = CreateService();
        var post = await CreateTestPost(service);
        var viewer = CreateActor("user2");

        await service.ReactAsync(new ReactRequest
        {
            TenantId = "tenant1",
            Actor = viewer,
            TargetId = post.Id!,
            TargetKind = ReactionTargetKind.Post,
            Type = ReactionType.Wow
        });

        var result = await service.QueryPostsAsync(new PostQuery
        {
            TenantId = "tenant1",
            Viewer = viewer
        });

        Assert.Equal(ReactionType.Wow, result.Items[0].ViewerReaction);
    }

    #endregion

    #region QueryReactions Tests

    [Fact]
    public async Task QueryReactions_ReturnsAllReactions()
    {
        var service = CreateService();
        var post = await CreateTestPost(service);

        await service.ReactAsync(new ReactRequest
        {
            TenantId = "tenant1",
            Actor = CreateActor("user1"),
            TargetId = post.Id!,
            TargetKind = ReactionTargetKind.Post,
            Type = ReactionType.Like
        });

        await service.ReactAsync(new ReactRequest
        {
            TenantId = "tenant1",
            Actor = CreateActor("user2"),
            TargetId = post.Id!,
            TargetKind = ReactionTargetKind.Post,
            Type = ReactionType.Love
        });

        var result = await service.QueryReactionsAsync(new ReactionQuery
        {
            TenantId = "tenant1",
            TargetId = post.Id!,
            TargetKind = ReactionTargetKind.Post
        });

        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task QueryReactions_FiltersByType()
    {
        var service = CreateService();
        var post = await CreateTestPost(service);

        await service.ReactAsync(new ReactRequest
        {
            TenantId = "tenant1",
            Actor = CreateActor("user1"),
            TargetId = post.Id!,
            TargetKind = ReactionTargetKind.Post,
            Type = ReactionType.Like
        });

        await service.ReactAsync(new ReactRequest
        {
            TenantId = "tenant1",
            Actor = CreateActor("user2"),
            TargetId = post.Id!,
            TargetKind = ReactionTargetKind.Post,
            Type = ReactionType.Love
        });

        var result = await service.QueryReactionsAsync(new ReactionQuery
        {
            TenantId = "tenant1",
            TargetId = post.Id!,
            TargetKind = ReactionTargetKind.Post,
            Type = ReactionType.Like
        });

        Assert.Single(result.Items);
        Assert.Equal(ReactionType.Like, result.Items[0].Type);
    }

    #endregion
}
