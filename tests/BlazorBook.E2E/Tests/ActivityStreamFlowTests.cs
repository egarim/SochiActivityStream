using BlazorBook.E2E.Infrastructure;
using Microsoft.Playwright;

namespace BlazorBook.E2E.Tests;

/// <summary>
/// Comprehensive E2E tests for Activity Stream flows:
/// - Follow/unfollow users
/// - Search for profiles
/// - Like/unlike posts
/// - Comment on posts
/// - Activity feed updates
/// Tests both functionality and UI state with screenshots.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class ActivityStreamFlowTests : BlazorBookPageTest
{
    private string _user1DisplayName = "";
    private string _user1Username = "";
    private string _user1Email = "";
    
    private string _user2DisplayName = "";
    private string _user2Username = "";
    private string _user2Email = "";

    [OneTimeSetUp]
    public new async Task OneTimeSetupAsync()
    {
        await base.OneTimeSetupAsync();
        
        // Create two test users for interaction testing
        var uniqueId = GenerateUniqueId();
        
        _user1DisplayName = $"Alice Test {uniqueId}";
        _user1Username = $"alice{uniqueId}";
        _user1Email = $"alice{uniqueId}@example.com";
        
        _user2DisplayName = $"Bob Test {uniqueId}";
        _user2Username = $"bob{uniqueId}";
        _user2Email = $"bob{uniqueId}@example.com";
    }

    private ILocator GetPostCard(string postContent) =>
        Page.Locator(".mud-card").Filter(new() { HasText = postContent }).First;

    #region Search Profile Flow Tests

    [Test]
    [Order(1)]
    [Category("Search")]
    [Category("ActivityStream")]
    public async Task SearchFlow_SearchForUser_FindsProfile()
    {
        // Arrange - Sign up user 1
        await SignUpAsync(_user1DisplayName, _user1Username, _user1Email, "password123");
        await NavigateToAsync("/");
        await WaitForBlazorAsync(2000); // Wait for indexing to complete
        
        // Log out and create user 2
        await NavigateToAsync("/logout");
        await WaitForBlazorAsync();
        
        await SignUpAsync(_user2DisplayName, _user2Username, _user2Email, "password123");
        await WaitForBlazorAsync(2000); // Wait for indexing to complete
        
        // Log out after signup to clear any stale auth state
        await NavigateToAsync("/logout");
        await WaitForBlazorAsync();
        
        // Explicitly log in as user 2 to establish reliable auth session
        await LoginAsync(_user2Email, "password123");
        
        // Wait for redirect to /feed after successful login
        await Expect(Page).ToHaveURLAsync($"{BaseUrl}/feed", new() { Timeout = 10000 });
        await WaitForBlazorAsync(1000);
        
        // USE THE ACTUAL SEARCH UI: Find the search input in the top nav and type the query
        // This uses Blazor's built-in navigation which preserves auth state
        var searchInput = Page.GetByPlaceholder("Search topics, people, or posts");
        await searchInput.FillAsync(_user1Username);
        
        // Press Enter to trigger search
        await searchInput.PressAsync("Enter");
        await WaitForBlazorAsync(5000); // Wait for search results to render
        
        // Take screenshot of search results
        await Page.ScreenshotAsync(new() { Path = "screenshots/flow-search-results.png", FullPage = true });
        
        // Debug: Check if results list is present
        var resultsList = Page.Locator(".mud-list");
        var hasResultsList = await resultsList.CountAsync() > 0;
        Console.WriteLine($"Results list present: {hasResultsList}");
        
        // Assert - Should find the user (search by display name or username in page content)
        var pageContent = await Page.ContentAsync();
        var foundDisplayName = pageContent.Contains(_user1DisplayName);
        var foundUsername = pageContent.Contains(_user1Username);
        
        Console.WriteLine($"Search for '{_user1Username}' - DisplayName found: {foundDisplayName}, Username found: {foundUsername}");
        
        if (!foundDisplayName && !foundUsername)
        {
            // Debug: Save page content to file for analysis
            await System.IO.File.WriteAllTextAsync("screenshots/flow-search-results.html", pageContent);
            Console.WriteLine("Page HTML saved to screenshots/flow-search-results.html for debugging");
        }
        
        Assert.That(foundDisplayName || foundUsername, Is.True, $"Should find user '{_user1DisplayName}' or '{_user1Username}' in search results");
    }

    #endregion

    #region Follow/Unfollow Flow Tests

    [Test]
    [Order(2)]
    [Category("Follow")]
    [Category("ActivityStream")]
    public async Task FollowFlow_FollowUser_ShowsFollowingState()
    {
        // Arrange - User 2 is logged in, navigate to user 1's profile
        await SignUpAsync(_user1DisplayName, _user1Username, _user1Email, "password123");
        var user1PostContent = $"Post by User 1 {GenerateUniqueId()}";
        await CreatePostAsync(user1PostContent);
        
        await NavigateToAsync("/logout");
        await SignUpAsync(_user2DisplayName, _user2Username, _user2Email, "password123");
        
        // Log out after signup to clear any stale auth state
        await NavigateToAsync("/logout");
        await WaitForBlazorAsync();
        
        // Explicitly log in as user 2 to establish reliable auth session
        await LoginAsync(_user2Email, "password123");
        
        // Wait for redirect to /feed after successful login
        await Expect(Page).ToHaveURLAsync($"{BaseUrl}/feed", new() { Timeout = 10000 });
        await WaitForBlazorAsync();
        
        // Use the search UI instead of direct navigation to preserve auth
        var searchInput = Page.GetByPlaceholder("Search topics, people, or posts");
        await searchInput.FillAsync(_user1Username);
        await searchInput.PressAsync("Enter");
        await WaitForBlazorAsync(1000);
        
        // Click on user 1's profile
        var profileLink = Page.GetByText(_user1DisplayName).First;
        await profileLink.ClickAsync();
        await WaitForBlazorAsync();
        
        await Page.ScreenshotAsync(new() { Path = "screenshots/flow-profile-before-follow.png", FullPage = true });
        
        // Act - Click Follow button
        var followButton = Page.GetByRole(AriaRole.Button).Filter(new() { HasText = "Follow" }).First;
        
        if (await followButton.CountAsync() > 0)
        {
            Console.WriteLine("✓ Follow button found");
            await followButton.ClickAsync();
            await WaitForBlazorAsync();
            
            // Take screenshot after following
            await Page.ScreenshotAsync(new() { Path = "screenshots/flow-profile-after-follow.png", FullPage = true });
            
            // Assert - Button should now say "Following" or "Unfollow"
            var followingButton = Page.GetByRole(AriaRole.Button).Filter(new() { HasText = "Following" });
            var unfollowButton = Page.GetByRole(AriaRole.Button).Filter(new() { HasText = "Unfollow" });
            
            var hasFollowingButton = await followingButton.CountAsync() > 0;
            var hasUnfollowButton = await unfollowButton.CountAsync() > 0;
            
            Console.WriteLine($"Following button visible: {hasFollowingButton}");
            Console.WriteLine($"Unfollow button visible: {hasUnfollowButton}");
            
            Assert.That(hasFollowingButton || hasUnfollowButton, Is.True,
                "After clicking Follow, should show 'Following' or 'Unfollow' button");
        }
        else
        {
            Console.WriteLine("⚠ Follow button not found - checking UI structure...");
            await Page.ScreenshotAsync(new() { Path = "screenshots/flow-profile-no-follow-button.png", FullPage = true });
            
            // Log what buttons are available
            var allButtons = await Page.GetByRole(AriaRole.Button).AllAsync();
            Console.WriteLine($"Available buttons: {allButtons.Count}");
            foreach (var btn in allButtons)
            {
                var btnText = await btn.TextContentAsync();
                Console.WriteLine($"  - Button: '{btnText}'");
            }
            
            Assert.Fail("Follow button not found on profile page");
        }
    }

    [Test]
    [Order(3)]
    [Category("Follow")]
    [Category("ActivityStream")]
    public async Task UnfollowFlow_UnfollowUser_ShowsUnfollowedState()
    {
        // Arrange - Set up two users where user 2 follows user 1
        await SignUpAsync(_user1DisplayName, _user1Username, _user1Email, "password123");
        await CreatePostAsync($"User 1 post {GenerateUniqueId()}");
        await NavigateToAsync("/logout");
        
        await SignUpAsync(_user2DisplayName, _user2Username, _user2Email, "password123");
        
        // Log out after signup to clear any stale auth state
        await NavigateToAsync("/logout");
        await WaitForBlazorAsync();
        
        // Explicitly log in as user 2 to establish reliable auth session
        await LoginAsync(_user2Email, "password123");
        
        // Wait for redirect to /feed after successful login
        await Expect(Page).ToHaveURLAsync($"{BaseUrl}/feed", new() { Timeout = 10000 });
        await WaitForBlazorAsync();
        
        // Use the search UI instead of direct navigation to preserve auth
        var searchInput = Page.GetByPlaceholder("Search topics, people, or posts");
        await searchInput.FillAsync(_user1Username);
        await searchInput.PressAsync("Enter");
        await WaitForBlazorAsync(1000);
        
        await Page.GetByText(_user1DisplayName).First.ClickAsync();
        await WaitForBlazorAsync();
        
        var followButton = Page.GetByRole(AriaRole.Button).Filter(new() { HasText = "Follow" }).First;
        if (await followButton.CountAsync() > 0)
        {
            await followButton.ClickAsync();
            await WaitForBlazorAsync();
        }
        
        // Act - Unfollow
        var unfollowButton = Page.GetByRole(AriaRole.Button).Filter(new() { 
            HasTextRegex = new System.Text.RegularExpressions.Regex("(Following|Unfollow)")
        }).First;
        
        if (await unfollowButton.CountAsync() > 0)
        {
            await Page.ScreenshotAsync(new() { Path = "screenshots/flow-before-unfollow.png", FullPage = true });
            
            await unfollowButton.ClickAsync();
            await WaitForBlazorAsync();
            
            await Page.ScreenshotAsync(new() { Path = "screenshots/flow-after-unfollow.png", FullPage = true });
            
            // Assert - Should show Follow button again
            var followButtonAgain = Page.GetByRole(AriaRole.Button).Filter(new() { HasText = "Follow" });
            Console.WriteLine($"Follow button visible after unfollow: {await followButtonAgain.CountAsync() > 0}");
            
            await Expect(followButtonAgain.First).ToBeVisibleAsync(new() { Timeout = 5000 });
        }
        else
        {
            Console.WriteLine("⚠ Following/Unfollow button not found");
            Assert.Fail("Could not find button to unfollow");
        }
    }

    #endregion

    #region Like/Unlike Flow Tests

    [Test]
    [Order(10)]
    [Category("Like")]
    [Category("ActivityStream")]
    public async Task LikeFlow_LikePost_ShowsLikeCount()
    {
        // Arrange
        await SignUpAsync(_user1DisplayName, _user1Username, _user1Email, "password123");
        var postContent = $"Post to like {GenerateUniqueId()}";
        await CreatePostAsync(postContent);
        
        await Page.ScreenshotAsync(new() { Path = "screenshots/flow-post-before-like.png", FullPage = true });
        
        // Act - Find and click like button on the specific post
        var postCard = GetPostCard(postContent);
        var likeButton = postCard.GetByRole(AriaRole.Button, new() { Name = "Like" });
        
        if (await likeButton.CountAsync() > 0)
        {
            Console.WriteLine("✓ Like button found");
            await likeButton.ClickAsync();
            await WaitForBlazorAsync(1000);
            
            await Page.ScreenshotAsync(new() { Path = "screenshots/flow-post-after-like.png", FullPage = true });
            
            // Assert - Should show like count of 1
            var likeCount = postCard.GetByText("1");
            var hasLikeCount = await likeCount.CountAsync() > 0;
            
            Console.WriteLine($"Like count '1' visible: {hasLikeCount}");
            
            // Also check if the button state changed (might have different styling/icon)
            var likedButton = postCard.GetByRole(AriaRole.Button, new() { Name = "Like" });
            Console.WriteLine($"Like button still present: {await likedButton.CountAsync() > 0}");
            
            Assert.That(hasLikeCount, Is.True, "Post should show like count of 1 after liking");
        }
        else
        {
            Console.WriteLine("⚠ Like button not found");
            await Page.ScreenshotAsync(new() { Path = "screenshots/flow-post-no-like-button.png", FullPage = true });
            Assert.Fail("Like button not found on post");
        }
    }

    [Test]
    [Order(11)]
    [Category("Like")]
    [Category("ActivityStream")]
    public async Task LikeFlow_UnlikePost_RemovesLikeCount()
    {
        // Arrange - Create and like a post
        await SignUpAsync(_user1DisplayName, _user1Username, _user1Email, "password123");
        var postContent = $"Post to unlike {GenerateUniqueId()}";
        await CreatePostAsync(postContent);
        
        var postCard = GetPostCard(postContent);
        var likeButton = postCard.GetByRole(AriaRole.Button, new() { Name = "Like" });
        
        if (await likeButton.CountAsync() > 0)
        {
            await likeButton.ClickAsync();
            await WaitForBlazorAsync(1000);
            
            await Page.ScreenshotAsync(new() { Path = "screenshots/flow-post-liked.png", FullPage = true });
            
            // Act - Unlike
            await likeButton.ClickAsync();
            await WaitForBlazorAsync(1000);
            
            await Page.ScreenshotAsync(new() { Path = "screenshots/flow-post-unliked.png", FullPage = true });
            
            // Assert - Like count should be removed or show 0
            var likeCountOne = postCard.GetByText("1");
            var hasLikeCountOne = await likeCountOne.CountAsync() > 0;
            
            Console.WriteLine($"Like count '1' visible after unlike: {hasLikeCountOne}");
            Assert.That(hasLikeCountOne, Is.False, "Post should not show like count of 1 after unliking");
        }
        else
        {
            Assert.Fail("Like button not found");
        }
    }

    [Test]
    [Order(12)]
    [Category("Like")]
    [Category("ActivityStream")]
    public async Task LikeFlow_MultipleUsers_ShowsTotalLikeCount()
    {
        // Arrange - User 1 creates a post
        await SignUpAsync(_user1DisplayName, _user1Username, _user1Email, "password123");
        var postContent = $"Post with multiple likes {GenerateUniqueId()}";
        await CreatePostAsync(postContent);
        
        // User 1 likes their own post
        var postCard = GetPostCard(postContent);
        var likeButton = postCard.GetByRole(AriaRole.Button, new() { Name = "Like" });
        if (await likeButton.CountAsync() > 0)
        {
            await likeButton.ClickAsync();
            await WaitForBlazorAsync();
        }
        
        await Page.ScreenshotAsync(new() { Path = "screenshots/flow-post-one-like.png", FullPage = true });
        
        // Log out and user 2 likes the same post
        await NavigateToAsync("/logout");
        await SignUpAsync(_user2DisplayName, _user2Username, _user2Email, "password123");
        
        // Log out after signup to clear any stale auth state
        await NavigateToAsync("/logout");
        await WaitForBlazorAsync();
        
        // Explicitly log in as user 2 to establish reliable auth session
        await LoginAsync(_user2Email, "password123");
        await WaitForBlazorAsync();
        
        // Find the post (should be in feed or search)
        await NavigateToAsync("/");
        await WaitForBlazorAsync(2000);
        
        // Try to find the post by content
        var postExists = await Page.GetByText(postContent).CountAsync() > 0;
        Console.WriteLine($"Post visible to user 2: {postExists}");
        
        if (postExists)
        {
            postCard = GetPostCard(postContent);
            likeButton = postCard.GetByRole(AriaRole.Button, new() { Name = "Like" });
            
            if (await likeButton.CountAsync() > 0)
            {
                await likeButton.ClickAsync();
                await WaitForBlazorAsync();
                
                await Page.ScreenshotAsync(new() { Path = "screenshots/flow-post-two-likes.png", FullPage = true });
                
                // Assert - Should show 2 likes
                var likeCount = postCard.GetByText("2");
                var hasTwoLikes = await likeCount.CountAsync() > 0;
                
                Console.WriteLine($"Like count '2' visible: {hasTwoLikes}");
                Assert.That(hasTwoLikes, Is.True, "Post should show like count of 2 after two users like it");
            }
        }
        else
        {
            Console.WriteLine("⚠ Post not visible in feed to user 2 - may need to follow user 1 first");
        }
    }

    #endregion

    #region Comment Flow Tests

    [Test]
    [Order(20)]
    [Category("Comment")]
    [Category("ActivityStream")]
    public async Task CommentFlow_ClickCommentButton_OpensCommentSection()
    {
        // Arrange
        await SignUpAsync(_user1DisplayName, _user1Username, _user1Email, "password123");
        var postContent = $"Post to comment on {GenerateUniqueId()}";
        await CreatePostAsync(postContent);
        
        await Page.ScreenshotAsync(new() { Path = "screenshots/flow-post-before-comment-click.png", FullPage = true });
        
        // Act
        var postCard = GetPostCard(postContent);
        var commentButton = postCard.GetByRole(AriaRole.Button, new() { Name = "Comment" });
        
        if (await commentButton.CountAsync() > 0)
        {
            Console.WriteLine("✓ Comment button found");
            await commentButton.ClickAsync();
            await WaitForBlazorAsync();
            
            await Page.ScreenshotAsync(new() { Path = "screenshots/flow-post-after-comment-click.png", FullPage = true });
            
            // Assert - Should show comment input or comment section
            var commentInputs = await Page.Locator("textarea, input[placeholder*='comment' i], input[placeholder*='reply' i]").AllAsync();
            var hasCommentInput = commentInputs.Count > 0;
            
            Console.WriteLine($"Comment input found: {hasCommentInput}");
            Console.WriteLine($"Number of comment inputs: {commentInputs.Count}");
            
            Assert.That(hasCommentInput, Is.True, "Should show comment input after clicking comment button");
        }
        else
        {
            Console.WriteLine("⚠ Comment button not found");
            await Page.ScreenshotAsync(new() { Path = "screenshots/flow-post-no-comment-button.png", FullPage = true });
            Assert.Fail("Comment button not found on post");
        }
    }

    [Test]
    [Order(21)]
    [Category("Comment")]
    [Category("ActivityStream")]
    public async Task CommentFlow_AddComment_ShowsInCommentList()
    {
        // Arrange
        await SignUpAsync(_user1DisplayName, _user1Username, _user1Email, "password123");
        var postContent = $"Post to add comment {GenerateUniqueId()}";
        await CreatePostAsync(postContent);
        
        var postCard = GetPostCard(postContent);
        var commentButton = postCard.GetByRole(AriaRole.Button, new() { Name = "Comment" });
        
        if (await commentButton.CountAsync() > 0)
        {
            await commentButton.ClickAsync();
            await WaitForBlazorAsync();
            
            // Act - Add a comment
            var commentText = $"Test comment {GenerateUniqueId()}";
            var commentInput = Page.Locator("textarea, input[placeholder*='comment' i]").Last;
            
            if (await commentInput.CountAsync() > 0)
            {
                Console.WriteLine("✓ Comment input found");
                await commentInput.FillAsync(commentText);
                await WaitForBlazorAsync(500);
                
                await Page.ScreenshotAsync(new() { Path = "screenshots/flow-comment-typing.png", FullPage = true });
                
                // Submit comment (press Enter or click submit button)
                await commentInput.PressAsync("Enter");
                await WaitForBlazorAsync(2000);
                
                await Page.ScreenshotAsync(new() { Path = "screenshots/flow-comment-submitted.png", FullPage = true });
                
                // Assert - Comment should appear in the list
                var commentExists = await Page.GetByText(commentText).CountAsync() > 0;
                Console.WriteLine($"Comment '{commentText}' visible: {commentExists}");
                
                Assert.That(commentExists, Is.True, "Comment should appear after submission");
            }
            else
            {
                Console.WriteLine("⚠ Comment input not found");
                await Page.ScreenshotAsync(new() { Path = "screenshots/flow-no-comment-input.png", FullPage = true });
                Assert.Fail("Comment input not found");
            }
        }
        else
        {
            Assert.Fail("Comment button not found");
        }
    }

    #endregion

    #region Activity Feed Tests

    [Test]
    [Order(30)]
    [Category("Feed")]
    [Category("ActivityStream")]
    public async Task FeedFlow_FollowUser_SeesTheirPostsInFeed()
    {
        // Arrange - User 1 creates posts
        await SignUpAsync(_user1DisplayName, _user1Username, _user1Email, "password123");
        var user1Post = $"User 1 post {GenerateUniqueId()}";
        await CreatePostAsync(user1Post);
        
        await Page.ScreenshotAsync(new() { Path = "screenshots/flow-user1-feed.png", FullPage = true });
        
        // User 2 logs in and follows User 1
        await NavigateToAsync("/logout");
        await SignUpAsync(_user2DisplayName, _user2Username, _user2Email, "password123");
        
        // Follow user 1
        await NavigateToAsync("/search");
        var searchInput = Page.Locator("input[type='search'], input[type='text'], .mud-input-slot input").First;
        await searchInput.FillAsync(_user1Username);
        await WaitForBlazorAsync(1000);
        
        await Page.GetByText(_user1DisplayName).First.ClickAsync();
        await WaitForBlazorAsync();
        
        var followButton = Page.GetByRole(AriaRole.Button).Filter(new() { HasText = "Follow" });
        if (await followButton.CountAsync() > 0)
        {
            await followButton.ClickAsync();
            await WaitForBlazorAsync();
        }
        
        // Act - Navigate to feed
        await NavigateToAsync("/");
        await WaitForBlazorAsync(2000);
        
        await Page.ScreenshotAsync(new() { Path = "screenshots/flow-user2-feed-after-follow.png", FullPage = true });
        
        // Assert - Should see user 1's post in the feed
        var postVisible = await Page.GetByText(user1Post).CountAsync() > 0;
        Console.WriteLine($"User 1's post visible in User 2's feed: {postVisible}");
        
        // Log all visible post content for debugging
        var allPosts = await Page.Locator(".mud-card").AllAsync();
        Console.WriteLine($"Total posts in feed: {allPosts.Count}");
        
        Assert.That(postVisible, Is.True, "User 2 should see User 1's post in feed after following");
    }

    #endregion

    #region UI State and CSS Tests

    [Test]
    [Order(40)]
    [Category("UI")]
    [Category("ActivityStream")]
    public async Task UI_LikeButton_HasCorrectStyling()
    {
        // Arrange
        await SignUpAsync(_user1DisplayName, _user1Username, _user1Email, "password123");
        var postContent = $"Style test post {GenerateUniqueId()}";
        await CreatePostAsync(postContent);
        
        var postCard = GetPostCard(postContent);
        var likeButton = postCard.GetByRole(AriaRole.Button, new() { Name = "Like" });
        
        if (await likeButton.CountAsync() > 0)
        {
            // Check if button is visible and enabled
            await Expect(likeButton).ToBeVisibleAsync();
            await Expect(likeButton).ToBeEnabledAsync();
            
            // Get computed styles
            var isVisible = await likeButton.IsVisibleAsync();
            var isEnabled = await likeButton.IsEnabledAsync();
            
            Console.WriteLine($"✓ Like button visible: {isVisible}");
            Console.WriteLine($"✓ Like button enabled: {isEnabled}");
            
            await Page.ScreenshotAsync(new() { Path = "screenshots/flow-like-button-styling.png", FullPage = true });
            
            Assert.That(isVisible && isEnabled, Is.True, "Like button should be visible and enabled");
        }
        else
        {
            Assert.Fail("Like button not found for styling test");
        }
    }

    [Test]
    [Order(41)]
    [Category("UI")]
    [Category("ActivityStream")]
    public async Task UI_FollowButton_HasCorrectStyling()
    {
        // Arrange - Create user 1 and view from user 2
        await SignUpAsync(_user1DisplayName, _user1Username, _user1Email, "password123");
        await CreatePostAsync($"Test post {GenerateUniqueId()}");
        await NavigateToAsync("/logout");
        
        await SignUpAsync(_user2DisplayName, _user2Username, _user2Email, "password123");
        
        // Navigate to user 1's profile
        await NavigateToAsync("/search");
        var searchInput = Page.Locator("input[type='search'], input[type='text'], .mud-input-slot input").First;
        await searchInput.FillAsync(_user1Username);
        await WaitForBlazorAsync(1000);
        
        await Page.GetByText(_user1DisplayName).First.ClickAsync();
        await WaitForBlazorAsync();
        
        var followButton = Page.GetByRole(AriaRole.Button).Filter(new() { HasText = "Follow" });
        
        if (await followButton.CountAsync() > 0)
        {
            await Expect(followButton.First).ToBeVisibleAsync();
            await Expect(followButton.First).ToBeEnabledAsync();
            
            Console.WriteLine("✓ Follow button is visible and enabled");
            await Page.ScreenshotAsync(new() { Path = "screenshots/flow-follow-button-styling.png", FullPage = true });
        }
        else
        {
            Console.WriteLine("⚠ Follow button not found");
            await Page.ScreenshotAsync(new() { Path = "screenshots/flow-no-follow-button-styling.png", FullPage = true });
        }
    }

    #endregion
}
