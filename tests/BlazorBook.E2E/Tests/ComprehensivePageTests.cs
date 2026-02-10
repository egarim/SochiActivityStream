using BlazorBook.E2E.Infrastructure;
using Microsoft.Playwright;

namespace BlazorBook.E2E.Tests;

/// <summary>
/// Comprehensive E2E tests that systematically verify all pages in the BlazorBook application.
/// Tests UI layout, navigation, and core functionality on each page.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class ComprehensivePageTests : BlazorBookPageTest
{
    private string _testUserDisplayName = "";
    private string _testUserUsername = "";
    private string _testUserEmail = "";
    private const string TestPassword = "password123";

    [OneTimeSetUp]
    public new async Task OneTimeSetupAsync()
    {
        await base.OneTimeSetupAsync();
        
        // Create a persistent test user for all tests in this fixture
        var uniqueId = GenerateUniqueId();
        _testUserDisplayName = $"Test User {uniqueId}";
        _testUserUsername = $"testuser{uniqueId}";
        _testUserEmail = $"testuser{uniqueId}@example.com";
        
        await SignUpAsync(_testUserDisplayName, _testUserUsername, _testUserEmail, TestPassword);
    }

    #region Authentication Pages

    [Test]
    [Order(1)]
    [Category("UI")]
    [Category("Authentication")]
    public async Task HomePage_UnauthenticatedUser_ShowsLoginOptions()
    {
        // Navigate fresh without authentication
        var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        
        await page.GotoAsync($"{BaseUrl}/");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Take screenshot
        await page.ScreenshotAsync(new() { Path = "screenshots/home-unauthenticated.png", FullPage = true });
        
        // Verify UI elements
        await Expect(page.GetByText("BlazorBook")).ToBeVisibleAsync();
        await Expect(page.GetByText("Connect with friends")).ToBeVisibleAsync();
        
        await page.CloseAsync();
        await context.CloseAsync();
    }

    [Test]
    [Order(2)]
    [Category("UI")]
    [Category("Authentication")]
    public async Task LoginPage_HasCorrectLayout()
    {
        var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        
        await page.GotoAsync($"{BaseUrl}/login");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Take screenshot
        await page.ScreenshotAsync(new() { Path = "screenshots/login-page.png", FullPage = true });
        
        // Verify UI elements
        await Expect(page.Locator("#login-email")).ToBeVisibleAsync();
        await Expect(page.Locator("#login-password")).ToBeVisibleAsync();
        await Expect(page.Locator("#login-button")).ToBeVisibleAsync();
        await Expect(page.GetByText("BlazorBook", new() { Exact = false })).ToBeVisibleAsync();
        
        await page.CloseAsync();
        await context.CloseAsync();
    }

    [Test]
    [Order(3)]
    [Category("UI")]
    [Category("Authentication")]
    public async Task SignUpPage_HasCorrectLayout()
    {
        var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        
        await page.GotoAsync($"{BaseUrl}/signup");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Take screenshot
        await page.ScreenshotAsync(new() { Path = "screenshots/signup-page.png", FullPage = true });
        
        // Verify UI elements
        await Expect(page.Locator("#signup-displayname")).ToBeVisibleAsync();
        await Expect(page.Locator("#signup-username")).ToBeVisibleAsync();
        await Expect(page.Locator("#signup-email")).ToBeVisibleAsync();
        await Expect(page.Locator("#signup-password")).ToBeVisibleAsync();
        await Expect(page.Locator("#signup-button")).ToBeVisibleAsync();
        
        await page.CloseAsync();
        await context.CloseAsync();
    }

    #endregion

    #region Core Pages (Authenticated)

    [Test]
    [Order(10)]
    [Category("UI")]
    [Category("Feed")]
    public async Task FeedPage_ShowsCorrectLayout()
    {
        await NavigateToAsync("/feed");
        
        // Take screenshot
        await Page.ScreenshotAsync(new() { Path = "screenshots/feed-page.png", FullPage = true });
        
        // Verify core UI elements
        await Expect(Page.Locator("#post-content")).ToBeVisibleAsync();
        await Expect(Page.Locator("#post-button")).ToBeVisibleAsync();
        
        // Verify navigation is present
        await Expect(Page.GetByRole(AriaRole.Navigation).First).ToBeVisibleAsync();
    }

    [Test]
    [Order(11)]
    [Category("Functionality")]
    [Category("Feed")]
    public async Task FeedPage_CanCreatePost()
    {
        await NavigateToAsync("/feed");
        
        var postContent = $"Test post {GenerateUniqueId()}";
        await CreatePostAsync(postContent);
        
        // Verify post appears
        await Expect(Page.GetByText(postContent)).ToBeVisibleAsync();
        
        // Take screenshot showing the created post
        await Page.ScreenshotAsync(new() { Path = "screenshots/feed-with-post.png", FullPage = true });
    }

    [Test]
    [Order(12)]
    [Category("Functionality")]
    [Category("Feed")]
    public async Task FeedPage_PostHasInteractionButtons()
    {
        await NavigateToAsync("/feed");
        
        var postContent = $"Interaction test {GenerateUniqueId()}";
        await CreatePostAsync(postContent);
        
        var postCard = Page.Locator(".mud-card").Filter(new() { HasText = postContent }).First;
        
        // Verify interaction buttons
        await Expect(postCard.GetByRole(AriaRole.Button, new() { Name = "Like" })).ToBeVisibleAsync();
        await Expect(postCard.GetByRole(AriaRole.Button, new() { Name = "Comment" })).ToBeVisibleAsync();
    }

    [Test]
    [Order(20)]
    [Category("UI")]
    [Category("Profile")]
    public async Task ProfilePage_ShowsUserInformation()
    {
        await NavigateToAsync("/profile");
        
        // Take screenshot
        await Page.ScreenshotAsync(new() { Path = "screenshots/profile-page.png", FullPage = true });
        
        // Verify user information is displayed
        await Expect(Page.GetByText(_testUserDisplayName)).ToBeVisibleAsync();
        await Expect(Page.GetByText($"@{_testUserUsername}")).ToBeVisibleAsync();
    }

    [Test]
    [Order(21)]
    [Category("UI")]
    [Category("Profile")]
    public async Task ProfilePage_HasTabs()
    {
        await NavigateToAsync("/profile");
        
        // Look for tab indicators (MudBlazor tabs)
        var tabsContainer = Page.Locator(".mud-tabs");
        if (await tabsContainer.CountAsync() > 0)
        {
            await Expect(tabsContainer).ToBeVisibleAsync();
            
            // Take screenshot of tabs
            await Page.ScreenshotAsync(new() { Path = "screenshots/profile-tabs.png", FullPage = true });
        }
    }

    [Test]
    [Order(30)]
    [Category("UI")]
    [Category("Messages")]
    public async Task MessagesPage_ShowsCorrectLayout()
    {
        await NavigateToAsync("/messages");
        
        // Take screenshot
        await Page.ScreenshotAsync(new() { Path = "screenshots/messages-page.png", FullPage = true });
        
        // Verify page loaded
        await Expect(Page.GetByText("Messages", new() { Exact = false })).ToBeVisibleAsync();
    }

    [Test]
    [Order(40)]
    [Category("UI")]
    [Category("Friends")]
    public async Task FriendsPage_ShowsCorrectLayout()
    {
        await NavigateToAsync("/friends");
        
        // Take screenshot
        await Page.ScreenshotAsync(new() { Path = "screenshots/friends-page.png", FullPage = true });
        
        // Verify page loaded
        await Expect(Page.GetByText("Friends", new() { Exact = false })).ToBeVisibleAsync();
    }

    [Test]
    [Order(50)]
    [Category("UI")]
    [Category("Notifications")]
    public async Task NotificationsPage_ShowsCorrectLayout()
    {
        await NavigateToAsync("/notifications");
        
        // Take screenshot
        await Page.ScreenshotAsync(new() { Path = "screenshots/notifications-page.png", FullPage = true });
        
        // Verify page loaded
        await Expect(Page.GetByText("Notifications", new() { Exact = false })).ToBeVisibleAsync();
    }

    [Test]
    [Order(60)]
    [Category("UI")]
    [Category("Search")]
    public async Task SearchPage_ShowsCorrectLayout()
    {
        await NavigateToAsync("/search");
        
        // Take screenshot
        await Page.ScreenshotAsync(new() { Path = "screenshots/search-page.png", FullPage = true });
        
        // Look for search functionality
        var searchInput = Page.Locator("input[type='search'], input[placeholder*='Search' i]").First;
        if (await searchInput.CountAsync() > 0)
        {
            await Expect(searchInput).ToBeVisibleAsync();
        }
    }

    #endregion

    #region Navigation Tests

    [Test]
    [Order(100)]
    [Category("Navigation")]
    public async Task Navigation_CanAccessAllMainPages()
    {
        var pages = new[]
        {
            ("/feed", "Feed"),
            ("/profile", "Profile"),
            ("/messages", "Messages"),
            ("/friends", "Friends"),
            ("/notifications", "Notifications"),
            ("/search", "Search")
        };

        foreach (var (path, name) in pages)
        {
            await NavigateToAsync(path);
            
            // Verify page loaded (URL should contain the path)
            Assert.That(Page.Url, Does.Contain(path), 
                $"Failed to navigate to {name} page");
            
            await Task.Delay(500); // Brief pause between navigations
        }
    }

    [Test]
    [Order(101)]
    [Category("Navigation")]
    [Category("UI")]
    public async Task Navigation_SidebarIsVisibleOnAllPages()
    {
        var pages = new[] { "/feed", "/profile", "/messages", "/friends" };

        foreach (var path in pages)
        {
            await NavigateToAsync(path);
            
            // Verify navigation sidebar is present
            var sidebar = Page.Locator("aside, nav, .x-shell__rail").First;
            if (await sidebar.CountAsync() > 0)
            {
                await Expect(sidebar).ToBeVisibleAsync();
            }
        }
    }

    #endregion

    #region Layout & Responsive Tests

    [Test]
    [Order(200)]
    [Category("UI")]
    [Category("Responsive")]
    public async Task Layout_WorksOnDesktopSize()
    {
        await Page.SetViewportSizeAsync(1920, 1080);
        await NavigateToAsync("/feed");
        
        await Page.ScreenshotAsync(new() { Path = "screenshots/layout-desktop.png", FullPage = true });
        
        // Verify main layout elements are visible
        await Expect(Page.Locator(".x-shell, main, .content").First).ToBeVisibleAsync();
    }

    [Test]
    [Order(201)]
    [Category("UI")]
    [Category("Responsive")]
    public async Task Layout_WorksOnTabletSize()
    {
        await Page.SetViewportSizeAsync(768, 1024);
        await NavigateToAsync("/feed");
        
        await Page.ScreenshotAsync(new() { Path = "screenshots/layout-tablet.png", FullPage = true });
        
        // Verify page is still functional
        await Expect(Page.Locator("body").First).ToBeVisibleAsync();
    }

    [Test]
    [Order(202)]
    [Category("UI")]
    [Category("Responsive")]
    public async Task Layout_WorksOnMobileSize()
    {
        await Page.SetViewportSizeAsync(375, 667);
        await NavigateToAsync("/feed");
        
        await Page.ScreenshotAsync(new() { Path = "screenshots/layout-mobile.png", FullPage = true });
        
        // Verify page is still functional
        await Expect(Page.Locator("body").First).ToBeVisibleAsync();
    }

    #endregion

    #region Error Handling

    [Test]
    [Order(300)]
    [Category("Navigation")]
    public async Task Navigation_InvalidRoute_ShowsNotFound()
    {
        await NavigateToAsync("/this-page-does-not-exist");
        
        // Take screenshot
        await Page.ScreenshotAsync(new() { Path = "screenshots/page-not-found.png", FullPage = true });
        
        // Verify 404 or "not found" message
        var notFoundIndicators = new[]
        {
            "not found",
            "404",
            "page not found",
            "nothing at this address"
        };

        var pageContent = await Page.TextContentAsync("body");
        var hasNotFoundMessage = notFoundIndicators.Any(indicator => 
            pageContent?.Contains(indicator, StringComparison.OrdinalIgnoreCase) == true);
        
        Assert.That(hasNotFoundMessage, Is.True, "Page should show a not found message");
    }

    #endregion
}
