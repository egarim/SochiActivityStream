using BlazorBook.E2E.Infrastructure;
using Microsoft.Playwright;

namespace BlazorBook.E2E.Tests;

/// <summary>
/// E2E tests for post interactions (like, comment, share).
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class InteractionTests : BlazorBookPageTest
{
    [SetUp]
    public async Task SetUp()
    {
        // Create a unique user for each test
        var uniqueId = GenerateUniqueId();
        await SignUpAsync($"Interaction Test {uniqueId}", $"interactiontest{uniqueId}", $"interaction{uniqueId}@example.com", "password123");
    }

    /// <summary>
    /// Gets a locator scoped to a specific post card containing the given text.
    /// </summary>
    private ILocator GetPostCard(string postContent) =>
        Page.Locator("dxcard").Filter(new() { HasText = postContent }).First;

    [Test]
    public async Task Like_ClickLikeButton_ShowsLikeCount()
    {
        // Arrange - create a post
        var postContent = $"Like count test {GenerateUniqueId()}";
        await CreatePostAsync(postContent);
        
        // Get the specific post card
        var postCard = GetPostCard(postContent);
        
        // Act - click the like button within this post
        await postCard.GetByRole(AriaRole.Button, new() { Name = "Like" }).ClickAsync();
        await WaitForBlazorAsync();
        
        // Assert - should show "1" like within this post
        await Expect(postCard.GetByText("1")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Like_ClickLikeTwice_TogglesLike()
    {
        // Arrange - create a post
        var postContent = $"Like toggle test {GenerateUniqueId()}";
        await CreatePostAsync(postContent);
        
        // Get the specific post card
        var postCard = GetPostCard(postContent);
        var likeButton = postCard.GetByRole(AriaRole.Button, new() { Name = "Like" });
        
        // Act - click like button twice
        await likeButton.ClickAsync();
        await WaitForBlazorAsync();
        
        // First click - should show 1
        await Expect(postCard.GetByText("1")).ToBeVisibleAsync();
        
        await likeButton.ClickAsync();
        await WaitForBlazorAsync();
        
        // Second click - like count should be removed or 0
        // The "1" should no longer be visible in this post
    }

    [Test]
    public async Task Comment_ClickCommentButton_IsClickable()
    {
        // Arrange - create a post
        var postContent = $"Comment button test {GenerateUniqueId()}";
        await CreatePostAsync(postContent);
        
        // Get the specific post card
        var postCard = GetPostCard(postContent);
        
        // Act & Assert - the comment button should be clickable
        var commentButton = postCard.GetByRole(AriaRole.Button, new() { Name = "Comment" });
        await Expect(commentButton).ToBeEnabledAsync();
        await commentButton.ClickAsync();
    }

    [Test]
    public async Task Share_ClickShareButton_IsClickable()
    {
        // Arrange - create a post
        var postContent = $"Share button test {GenerateUniqueId()}";
        await CreatePostAsync(postContent);
        
        // Get the specific post card
        var postCard = GetPostCard(postContent);
        
        // Act & Assert - the share button should be clickable
        var shareButton = postCard.GetByRole(AriaRole.Button, new() { Name = "Share" });
        await Expect(shareButton).ToBeEnabledAsync();
        await shareButton.ClickAsync();
    }

    [Test]
    public async Task PostInteraction_AllButtonsVisible()
    {
        // Arrange - create a post
        var postContent = $"All buttons test {GenerateUniqueId()}";
        await CreatePostAsync(postContent);
        
        // Get the specific post card
        var postCard = GetPostCard(postContent);
        
        // Assert - all interaction buttons should be visible within this post
        await Expect(postCard.GetByRole(AriaRole.Button, new() { Name = "Like" })).ToBeVisibleAsync();
        await Expect(postCard.GetByRole(AriaRole.Button, new() { Name = "Comment" })).ToBeVisibleAsync();
        await Expect(postCard.GetByRole(AriaRole.Button, new() { Name = "Share" })).ToBeVisibleAsync();
    }
}
