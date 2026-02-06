using BlazorBook.E2E.Infrastructure;
using Microsoft.Playwright;

namespace BlazorBook.E2E.Tests;

/// <summary>
/// E2E tests for the profile page functionality.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class ProfileTests : BlazorBookPageTest
{
    private string _displayName = "";
    private string _username = "";

    [SetUp]
    public async Task SetUp()
    {
        // Create a unique user for each test
        var uniqueId = GenerateUniqueId();
        _displayName = $"Profile Test {uniqueId}";
        _username = $"profiletest{uniqueId}";
        await SignUpAsync(_displayName, _username, $"profile{uniqueId}@example.com", "password123");
    }

    [Test]
    public async Task Profile_NavigateFromPost_ShowsProfilePage()
    {
        // Arrange - create a post first
        var postContent = $"Profile nav test {GenerateUniqueId()}";
        await CreatePostAsync(postContent);
        
        // Act - click on the author name
        await Page.GetByRole(AriaRole.Link, new() { Name = _displayName }).First.ClickAsync();
        await WaitForBlazorAsync();
        
        // Assert - should be on profile page
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(@"/profile/"));
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = _displayName })).ToBeVisibleAsync();
    }

    [Test]
    public async Task Profile_ShowsUserHandle()
    {
        // Arrange
        var postContent = $"Handle test {GenerateUniqueId()}";
        await CreatePostAsync(postContent);
        
        // Act
        await Page.GetByRole(AriaRole.Link, new() { Name = _displayName }).First.ClickAsync();
        await WaitForBlazorAsync();
        
        // Assert - should show @handle
        await Expect(Page.GetByText(new System.Text.RegularExpressions.Regex(@"@\w+"))).ToBeVisibleAsync();
    }

    [Test]
    public async Task Profile_ShowsFollowerCount()
    {
        // Arrange
        var postContent = $"Follower count test {GenerateUniqueId()}";
        await CreatePostAsync(postContent);
        
        // Act
        await Page.GetByRole(AriaRole.Link, new() { Name = _displayName }).First.ClickAsync();
        await WaitForBlazorAsync();
        
        // Assert
        await Expect(Page.GetByText("Followers")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Profile_ShowsFollowingCount()
    {
        // Arrange
        var postContent = $"My post content {GenerateUniqueId()}";
        await CreatePostAsync(postContent);
        
        // Act
        await Page.GetByRole(AriaRole.Link, new() { Name = _displayName }).First.ClickAsync();
        await WaitForBlazorAsync();
        
        // Assert - use exact match to avoid matching post content
        await Expect(Page.GetByText("Following", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Test]
    public async Task Profile_ShowsPostCount()
    {
        // Arrange
        var postContent = $"Post count test {GenerateUniqueId()}";
        await CreatePostAsync(postContent);
        
        // Act
        await Page.GetByRole(AriaRole.Link, new() { Name = _displayName }).First.ClickAsync();
        await WaitForBlazorAsync();
        
        // Assert
        await Expect(Page.GetByText("Posts")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Profile_OwnProfile_ShowsEditButton()
    {
        // Arrange
        var postContent = $"Edit button test {GenerateUniqueId()}";
        await CreatePostAsync(postContent);
        
        // Act
        await Page.GetByRole(AriaRole.Link, new() { Name = _displayName }).First.ClickAsync();
        await WaitForBlazorAsync();
        
        // Assert
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Edit Profile" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task Profile_ShowsUserPosts()
    {
        // Arrange
        var postContent = $"User posts test {GenerateUniqueId()}";
        await CreatePostAsync(postContent);
        
        // Act
        await Page.GetByRole(AriaRole.Link, new() { Name = _displayName }).First.ClickAsync();
        await WaitForBlazorAsync();
        
        // Assert - the post should be visible on the profile
        await Expect(Page.GetByText(postContent)).ToBeVisibleAsync();
    }

    [Test]
    public async Task Profile_UpdatesPostCount()
    {
        // Arrange - create a post
        var postContent = $"Post count update test {GenerateUniqueId()}";
        await CreatePostAsync(postContent);
        
        // Act
        await Page.GetByRole(AriaRole.Link, new() { Name = _displayName }).First.ClickAsync();
        await WaitForBlazorAsync();
        
        // Assert - post count should be 1
        await Expect(Page.GetByText("1").First).ToBeVisibleAsync();
    }
}
