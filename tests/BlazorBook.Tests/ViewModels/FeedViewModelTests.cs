using NUnit.Framework;
using Content.Abstractions;
using Sochi.Navigation.Navigation;
using SocialKit.Components.ViewModels;

namespace BlazorBook.Tests.ViewModels;

[TestFixture]
public class FeedViewModelTests
{
    private TestFixture _fixture = null!;
    private FeedViewModel _viewModel = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new TestFixture();
        _viewModel = _fixture.GetViewModel<FeedViewModel>();
    }

    [TearDown]
    public void TearDown()
    {
        _fixture.Dispose();
    }

    [Test]
    public void Title_ShouldBeNewsFeed()
    {
        Assert.That(_viewModel.Title, Is.EqualTo("News Feed"));
    }

    [Test]
    public void WhenNotAuthenticated_Posts_ShouldBeEmpty()
    {
        // Not signed in
        Assert.That(_fixture.CurrentUser.IsAuthenticated, Is.False);
        Assert.That(_viewModel.Posts, Is.Empty);
    }

    [Test]
    public async Task WhenAuthenticated_InitializeAsync_ShouldLoadEmptyFeed()
    {
        // Arrange: Sign in as test user
        _fixture.CurrentUser.SignIn("user-1", "Test User", "https://example.com/avatar.jpg");
        
        // Act: Initialize the ViewModel
        await _viewModel.InitializeAsync(new NavigationParameters());
        
        // Assert: Feed is empty (no posts created yet)
        Assert.That(_viewModel.IsLoading, Is.False);
        Assert.That(_viewModel.Posts, Is.Empty);
    }

    [Test]
    public async Task CreatePostCommand_ShouldAddPostToFeed()
    {
        // Arrange: Sign in and initialize
        _fixture.CurrentUser.SignIn("user-1", "Test User");
        await _viewModel.InitializeAsync(new NavigationParameters());

        // Act: Create a post
        _viewModel.NewPostText = "Hello, this is my first post!";
        Assert.That(_viewModel.CreatePostCommand.CanExecute(null), Is.True);
        
        await _viewModel.CreatePostCommand.ExecuteAsync(null);
        
        // Assert: Post appears in feed
        Assert.That(_viewModel.Posts, Has.Count.EqualTo(1));
        Assert.That(_viewModel.Posts[0].Body, Is.EqualTo("Hello, this is my first post!"));
        Assert.That(_viewModel.Posts[0].Author.DisplayName, Is.EqualTo("Test User"));
        Assert.That(_viewModel.NewPostText, Is.Empty); // Input cleared after post
    }

    [Test]
    public async Task CreatePostCommand_WhenNotAuthenticated_CannotExecute()
    {
        // Not signed in
        _viewModel.NewPostText = "This should not be allowed";
        
        Assert.That(_viewModel.CreatePostCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task CreatePostCommand_WhenEmptyText_CannotExecute()
    {
        // Arrange: Sign in
        _fixture.CurrentUser.SignIn("user-1", "Test User");
        
        // Empty text
        _viewModel.NewPostText = "";
        Assert.That(_viewModel.CreatePostCommand.CanExecute(null), Is.False);
        
        // Whitespace only
        _viewModel.NewPostText = "   ";
        Assert.That(_viewModel.CreatePostCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task CreateMultiplePosts_ShouldAppearInReverseOrder()
    {
        // Arrange
        _fixture.CurrentUser.SignIn("user-1", "Test User");
        await _viewModel.InitializeAsync(new NavigationParameters());

        // Act: Create multiple posts
        _viewModel.NewPostText = "First post";
        await _viewModel.CreatePostCommand.ExecuteAsync(null);
        
        _viewModel.NewPostText = "Second post";
        await _viewModel.CreatePostCommand.ExecuteAsync(null);
        
        _viewModel.NewPostText = "Third post";
        await _viewModel.CreatePostCommand.ExecuteAsync(null);

        // Assert: Most recent first (inserted at index 0)
        Assert.That(_viewModel.Posts, Has.Count.EqualTo(3));
        Assert.That(_viewModel.Posts[0].Body, Is.EqualTo("Third post"));
        Assert.That(_viewModel.Posts[1].Body, Is.EqualTo("Second post"));
        Assert.That(_viewModel.Posts[2].Body, Is.EqualTo("First post"));
    }

    [Test]
    public async Task LikePostCommand_ShouldToggleLikeState()
    {
        // Arrange: Sign in and create a post
        _fixture.CurrentUser.SignIn("user-1", "Test User");
        await _viewModel.InitializeAsync(new NavigationParameters());
        
        _viewModel.NewPostText = "Like this post!";
        await _viewModel.CreatePostCommand.ExecuteAsync(null);
        
        var post = _viewModel.Posts[0];
        Assert.That(post.ViewerReaction, Is.Null);

        // Act: Like the post
        await _viewModel.LikePostCommand.ExecuteAsync(post);
        
        // Assert: Post is now liked
        Assert.That(post.ViewerReaction, Is.EqualTo(ReactionType.Like));

        // Act: Unlike the post
        await _viewModel.LikePostCommand.ExecuteAsync(post);
        
        // Assert: Post is no longer liked
        Assert.That(post.ViewerReaction, Is.Null);
    }

    [Test]
    public async Task RefreshCommand_ShouldReloadPosts()
    {
        // Arrange: Sign in and create a post
        _fixture.CurrentUser.SignIn("user-1", "Test User");
        await _viewModel.InitializeAsync(new NavigationParameters());
        
        _viewModel.NewPostText = "Test post";
        await _viewModel.CreatePostCommand.ExecuteAsync(null);
        
        Assert.That(_viewModel.Posts, Has.Count.EqualTo(1));

        // Act: Refresh feed
        await _viewModel.RefreshCommand.ExecuteAsync(null);
        
        // Assert: Posts are reloaded (still 1 post persisted in store)
        Assert.That(_viewModel.Posts, Has.Count.EqualTo(1));
    }

    [Test]
    public void CurrentUserName_ShouldReflectAuthenticatedUser()
    {
        Assert.That(_viewModel.CurrentUserName, Is.Null);
        
        _fixture.CurrentUser.SignIn("user-1", "Alice");
        Assert.That(_viewModel.CurrentUserName, Is.EqualTo("Alice"));
    }

    [Test]
    public void CurrentUserAvatar_ShouldReflectAuthenticatedUser()
    {
        Assert.That(_viewModel.CurrentUserAvatar, Is.Null);
        
        _fixture.CurrentUser.SignIn("user-1", "Alice", "https://example.com/alice.jpg");
        Assert.That(_viewModel.CurrentUserAvatar, Is.EqualTo("https://example.com/alice.jpg"));
    }
}
