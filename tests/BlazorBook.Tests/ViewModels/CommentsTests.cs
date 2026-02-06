using NUnit.Framework;
using Sochi.Navigation.Navigation;
using SocialKit.Components.ViewModels;
using Content.Abstractions;

namespace BlazorBook.Tests.ViewModels;

/// <summary>
/// Tests for commenting on posts: Create, Delete, Thread replies.
/// </summary>
[TestFixture]
public class CommentsTests
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

    private IContentService GetContentService() => 
        _fixture.GetService<IContentService>();

    private EntityRefDto MakeAuthor(string id) =>
        new() { Type = "Profile", Id = id };

    // ═══════════════════════════════════════════════════════════════════════════
    // CREATE COMMENT TESTS
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task CreateComment_AddsCommentToPost()
    {
        // Arrange
        var aliceId = await CreateUser("Alice", "alice", "alice@test.com");
        var post = await CreatePost("Hello, world!");
        
        // Act: Add a comment
        var contentService = GetContentService();
        var comment = await contentService.CreateCommentAsync(new CreateCommentRequest
        {
            TenantId = "blazorbook",
            PostId = post.Id!,
            Author = MakeAuthor(aliceId),
            Body = "Great post!"
        });
        
        // Assert
        Assert.That(comment.Id, Is.Not.Null);
        Assert.That(comment.Body, Is.EqualTo("Great post!"));
        Assert.That(comment.PostId, Is.EqualTo(post.Id));
    }

    [Test]
    public async Task CreateComment_IncreasesCommentCount()
    {
        // Arrange
        var aliceId = await CreateUser("Alice", "alice", "alice@test.com");
        var post = await CreatePost("Testing comments");
        
        var contentService = GetContentService();
        
        // Act: Add 3 comments
        for (int i = 1; i <= 3; i++)
        {
            await contentService.CreateCommentAsync(new CreateCommentRequest
            {
                TenantId = "blazorbook",
                PostId = post.Id!,
                Author = MakeAuthor(aliceId),
                Body = $"Comment {i}"
            });
        }
        
        // Assert: Query comments
        var result = await contentService.QueryCommentsAsync(new CommentQuery
        {
            TenantId = "blazorbook",
            PostId = post.Id!
        });
        
        Assert.That(result.Items, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task CreateComment_ByDifferentUser_Works()
    {
        // Arrange: Alice posts, Bob comments
        var aliceId = await CreateUser("Alice", "alice", "alice@test.com");
        var post = await CreatePost("Alice's post");
        
        await _fixture.CurrentUser.SignOutAsync();
        var bobId = await CreateUser("Bob", "bob", "bob@test.com");
        
        // Act: Bob comments on Alice's post
        var contentService = GetContentService();
        var comment = await contentService.CreateCommentAsync(new CreateCommentRequest
        {
            TenantId = "blazorbook",
            PostId = post.Id!,
            Author = MakeAuthor(bobId),
            Body = "Nice post, Alice!"
        });
        
        // Assert
        Assert.That(comment.Author.Id, Is.EqualTo(bobId));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // THREADED COMMENTS (REPLIES)
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task CreateReply_HasParentCommentId()
    {
        // Arrange
        var aliceId = await CreateUser("Alice", "alice", "alice@test.com");
        var post = await CreatePost("Original post");
        
        var contentService = GetContentService();
        
        // Create parent comment
        var parentComment = await contentService.CreateCommentAsync(new CreateCommentRequest
        {
            TenantId = "blazorbook",
            PostId = post.Id!,
            Author = MakeAuthor(aliceId),
            Body = "Parent comment"
        });
        
        // Act: Create reply to parent
        var reply = await contentService.CreateCommentAsync(new CreateCommentRequest
        {
            TenantId = "blazorbook",
            PostId = post.Id!,
            ParentCommentId = parentComment.Id,
            Author = MakeAuthor(aliceId),
            Body = "This is a reply"
        });
        
        // Assert
        Assert.That(reply.ParentCommentId, Is.EqualTo(parentComment.Id));
    }

    [Test]
    public async Task QueryReplies_ReturnsOnlyChildComments()
    {
        // Arrange
        var aliceId = await CreateUser("Alice", "alice", "alice@test.com");
        var post = await CreatePost("Test post");
        
        var contentService = GetContentService();
        
        // Create parent comment
        var parent = await contentService.CreateCommentAsync(new CreateCommentRequest
        {
            TenantId = "blazorbook",
            PostId = post.Id!,
            Author = MakeAuthor(aliceId),
            Body = "Parent"
        });
        
        // Create 2 replies
        await contentService.CreateCommentAsync(new CreateCommentRequest
        {
            TenantId = "blazorbook",
            PostId = post.Id!,
            ParentCommentId = parent.Id,
            Author = MakeAuthor(aliceId),
            Body = "Reply 1"
        });
        
        await contentService.CreateCommentAsync(new CreateCommentRequest
        {
            TenantId = "blazorbook",
            PostId = post.Id!,
            ParentCommentId = parent.Id,
            Author = MakeAuthor(aliceId),
            Body = "Reply 2"
        });
        
        // Act: Query replies to parent
        var result = await contentService.QueryCommentsAsync(new CommentQuery
        {
            TenantId = "blazorbook",
            PostId = post.Id!,
            ParentCommentId = parent.Id
        });
        
        // Assert
        Assert.That(result.Items, Has.Count.EqualTo(2));
        Assert.That(result.Items.All(c => c.ParentCommentId == parent.Id), Is.True);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DELETE COMMENT TESTS
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task DeleteComment_RemovesFromQuery()
    {
        // Arrange
        var aliceId = await CreateUser("Alice", "alice", "alice@test.com");
        var post = await CreatePost("Test post");
        
        var contentService = GetContentService();
        var comment = await contentService.CreateCommentAsync(new CreateCommentRequest
        {
            TenantId = "blazorbook",
            PostId = post.Id!,
            Author = MakeAuthor(aliceId),
            Body = "To be deleted"
        });
        
        // Act: Delete the comment
        await contentService.DeleteCommentAsync(new DeleteCommentRequest
        {
            TenantId = "blazorbook",
            CommentId = comment.Id!,
            Actor = MakeAuthor(aliceId)
        });
        
        // Assert: Comment no longer in default query
        var result = await contentService.QueryCommentsAsync(new CommentQuery
        {
            TenantId = "blazorbook",
            PostId = post.Id!
        });
        
        Assert.That(result.Items.Any(c => c.Id == comment.Id), Is.False);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GET COMMENT TESTS
    // ═══════════════════════════════════════════════════════════════════════════

    [Test]
    public async Task GetComment_ById_ReturnsComment()
    {
        // Arrange
        var aliceId = await CreateUser("Alice", "alice", "alice@test.com");
        var post = await CreatePost("Test post");
        
        var contentService = GetContentService();
        var created = await contentService.CreateCommentAsync(new CreateCommentRequest
        {
            TenantId = "blazorbook",
            PostId = post.Id!,
            Author = MakeAuthor(aliceId),
            Body = "Find me"
        });
        
        // Act
        var found = await contentService.GetCommentAsync("blazorbook", created.Id!);
        
        // Assert
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Body, Is.EqualTo("Find me"));
    }

    [Test]
    public async Task QueryComments_EmptyPost_ReturnsEmptyList()
    {
        // Arrange
        await CreateUser("Alice", "alice", "alice@test.com");
        var post = await CreatePost("No comments yet");
        
        var contentService = GetContentService();
        
        // Act
        var result = await contentService.QueryCommentsAsync(new CommentQuery
        {
            TenantId = "blazorbook",
            PostId = post.Id!
        });
        
        // Assert
        Assert.That(result.Items, Is.Empty);
    }
}
