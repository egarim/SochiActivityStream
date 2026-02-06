using BlazorBook.E2E.Infrastructure;
using Microsoft.Playwright;

namespace BlazorBook.E2E.Tests;

/// <summary>
/// E2E tests for the feed page and post creation.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class FeedTests : BlazorBookPageTest
{
    [SetUp]
    public async Task SetUp()
    {
        // Create a unique user for each test
        var uniqueId = GenerateUniqueId();
        await SignUpAsync($"Feed Test {uniqueId}", $"feedtest{uniqueId}", $"feed{uniqueId}@example.com", "password123");
    }

    /// <summary>
    /// Gets a locator scoped to a specific post card containing the given text.
    /// </summary>
    private ILocator GetPostCard(string postContent) =>
        Page.Locator("dxcard").Filter(new() { HasText = postContent }).First;

    [Test]
    public async Task Feed_ShowsPostComposer()
    {
        // Assert
        var postBox = Page.GetByRole(AriaRole.Textbox, new() { NameRegex = new System.Text.RegularExpressions.Regex("What's on your mind") });
        await Expect(postBox).ToBeVisibleAsync();
        
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Post" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task CreatePost_WithValidContent_AppearsInFeed()
    {
        // Arrange
        var postContent = $"Hello from Playwright test! {GenerateUniqueId()}";
        
        // Act
        await CreatePostAsync(postContent);
        
        // Assert
        await Expect(Page.GetByText(postContent)).ToBeVisibleAsync();
    }

    [Test]
    public async Task CreatePost_ShowsAuthorName()
    {
        // Arrange
        var postContent = $"Author name test {GenerateUniqueId()}";
        
        // Act
        await CreatePostAsync(postContent);
        
        // Assert - the post should show the author's name (as a link)
        var postCard = GetPostCard(postContent);
        await Expect(postCard.GetByRole(AriaRole.Link).First).ToBeVisibleAsync();
    }

    [Test]
    public async Task CreatePost_ShowsTimestamp()
    {
        // Arrange
        var postContent = $"Timestamp test {GenerateUniqueId()}";
        
        // Act
        await CreatePostAsync(postContent);
        
        // Assert - should show "Just now" within the post
        var postCard = GetPostCard(postContent);
        await Expect(postCard.GetByText("Just now")).ToBeVisibleAsync();
    }

    [Test]
    public async Task CreatePost_ShowsLikeButton()
    {
        // Arrange
        var postContent = $"Like button test {GenerateUniqueId()}";
        
        // Act
        await CreatePostAsync(postContent);
        
        // Assert - check within the specific post
        var postCard = GetPostCard(postContent);
        await Expect(postCard.GetByRole(AriaRole.Button, new() { Name = "Like" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task CreatePost_ShowsCommentButton()
    {
        // Arrange
        var postContent = $"Comment button test {GenerateUniqueId()}";
        
        // Act
        await CreatePostAsync(postContent);
        
        // Assert - check within the specific post
        var postCard = GetPostCard(postContent);
        await Expect(postCard.GetByRole(AriaRole.Button, new() { Name = "Comment" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task CreatePost_ShowsShareButton()
    {
        // Arrange
        var postContent = $"Share button test {GenerateUniqueId()}";
        
        // Act
        await CreatePostAsync(postContent);
        
        // Assert - check within the specific post
        var postCard = GetPostCard(postContent);
        await Expect(postCard.GetByRole(AriaRole.Button, new() { Name = "Share" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task CreatePost_ClearsComposerAfterPosting()
    {
        // Arrange
        var postContent = $"Clear composer test {GenerateUniqueId()}";
        
        // Act
        await CreatePostAsync(postContent);
        
        // Assert - composer should be empty
        var postBox = Page.GetByRole(AriaRole.Textbox, new() { NameRegex = new System.Text.RegularExpressions.Regex("What's on your mind") });
        await Expect(postBox).ToHaveValueAsync("");
    }

    [Test]
    public async Task CreateMultiplePosts_AppearsInReverseChronologicalOrder()
    {
        // Arrange
        var firstPost = $"First post {GenerateUniqueId()}";
        var secondPost = $"Second post {GenerateUniqueId()}";
        
        // Act
        await CreatePostAsync(firstPost);
        await Task.Delay(100); // Ensure different timestamps
        await CreatePostAsync(secondPost);
        
        // Assert - both posts should be visible
        await Expect(Page.GetByText(firstPost)).ToBeVisibleAsync();
        await Expect(Page.GetByText(secondPost)).ToBeVisibleAsync();
    }

    [Test]
    public async Task PostButton_DisabledWhenEmpty()
    {
        // Assert - Post button should be disabled when composer is empty
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Post" })).ToBeDisabledAsync();
    }
}
